using Microsoft.Data.SqlClient;

namespace ClientePagamentoSystem.Data;

/// <summary>
/// Verifica o SQL Server, cria o banco/tabelas se necessário e faz o seed
/// inicial (formas de pagamento + usuário Larissa com hash BCrypt real).
/// </summary>
public class DatabaseInitializer
{
    private readonly DatabaseConnection _connection;

    public DatabaseInitializer(DatabaseConnection connection)
    {
        _connection = connection;
    }

    public void Initialize()
    {
        EnsureDatabase();
        EnsureSchemaCompatibility();
        EnsureTables();
        EnsureUsuariosPerfil();
        SeedFormasPagamento();
        SeedUsuarioPadrao();
        SeedUsuariosComPerfil();
    }

    private void EnsureDatabase()
    {
        using var cn = _connection.CreateMasterConnection();
        cn.Open(); // lança exceção se o SQL Server estiver indisponível
        const string sql = @"
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ClientePagamentoDB')
                CREATE DATABASE ClientePagamentoDB;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Corrige bancos legados com schema incompatível (restos de versões antigas):
    /// recria a tabela Pagamentos e remove colunas legadas de Clientes quando necessário.
    /// </summary>
    private void EnsureSchemaCompatibility()
    {
        using var cn = _connection.CreateConnection();
        cn.Open();

        const string sql = @"
-- Se a tabela Pagamentos existe mas não tem a coluna Valor, é um schema legado: recria.
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Pagamentos')
   AND NOT EXISTS (SELECT * FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
                   WHERE t.name = 'Pagamentos' AND c.name = 'Valor')
BEGIN
    -- Remove FKs que apontam para Pagamentos (se houver) e a própria tabela
    DECLARE @fk NVARCHAR(MAX) = '';
    SELECT @fk += 'ALTER TABLE ' + QUOTENAME(OBJECT_NAME(parent_object_id))
                + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';'
    FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID('Pagamentos');
    EXEC(@fk);
    DROP TABLE Pagamentos;
END;

-- Remove colunas legadas de Clientes que não pertencem ao modelo atual
if exists (select * from sys.tables where name = 'Clientes')
begin
    -- antes: remove constraints DEFAULT vinculadas a essas colunas
    declare @cons nvarchar(max) = '';
    select @cons += 'alter table Clientes drop constraint ' + quotename(dc.name) + ';'
    from sys.default_constraints dc
    join sys.columns c on c.object_id = dc.parent_object_id and c.column_id = dc.parent_column_id
    join sys.tables t on t.object_id = c.object_id
    where t.name = 'Clientes'
      and c.name not in ('Id','NomeCompleto','CPF','Telefone','Email','DataNascimento','CEP','Endereco','Complemento','Ativo');
    exec(@cons);

    declare @cols nvarchar(max) = '';
    select @cols += 'alter table Clientes drop column ' + quotename(c.name) + ';'
    from sys.columns c join sys.tables t on t.object_id = c.object_id
    where t.name = 'Clientes'
      and c.name not in ('Id','NomeCompleto','CPF','Telefone','Email','DataNascimento','CEP','Endereco','Complemento','Ativo')
      and c.is_identity = 0;
    exec(@cols);

    -- garante colunas novas em bancos já existentes
    if not exists (select * from sys.columns c join sys.tables t on t.object_id = c.object_id
                   where t.name = 'Clientes' and c.name = 'CEP')
        alter table Clientes add CEP varchar(9) null;
    if not exists (select * from sys.columns c join sys.tables t on t.object_id = c.object_id
                   where t.name = 'Clientes' and c.name = 'Complemento')
        alter table Clientes add Complemento nvarchar(150) null;
end;";

        using var cmd = new SqlCommand(sql, cn);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Garante que a tabela Usuarios tenha a coluna Perfil (adm/func).</summary>
    private void EnsureUsuariosPerfil()
    {
        using var cn = _connection.CreateConnection();
        cn.Open();
        const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
               WHERE t.name = 'Usuarios' AND c.name = 'Perfil')
    ALTER TABLE Usuarios ADD Perfil NVARCHAR(20) NOT NULL DEFAULT 'adm';";
        using var cmd = new SqlCommand(sql, cn);
        cmd.ExecuteNonQuery();
    }

    private void EnsureTables()
    {
        using var cn = _connection.CreateConnection();
        cn.Open();

        const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios')
BEGIN
    CREATE TABLE Usuarios (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        NomeCompleto NVARCHAR(150) NOT NULL,
        Email NVARCHAR(150) NOT NULL,
        Ativo BIT NOT NULL DEFAULT 1,
        DataCriacao DATETIME NOT NULL DEFAULT GETDATE(),
        UltimoLogin DATETIME NULL
    );
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Clientes')
BEGIN
    CREATE TABLE Clientes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        NomeCompleto NVARCHAR(150) NOT NULL,
        CPF VARCHAR(14) NOT NULL,
        Telefone VARCHAR(20) NULL,
        Email NVARCHAR(150) NULL,
        DataNascimento DATE NULL,
        CEP VARCHAR(9) NULL,
        Endereco NVARCHAR(250) NULL,
        Complemento NVARCHAR(150) NULL,
        Ativo BIT NOT NULL DEFAULT 1,
        CONSTRAINT UQ_Clientes_CPF UNIQUE (CPF)
    );
    CREATE INDEX IX_Clientes_Nome ON Clientes (NomeCompleto);
    CREATE INDEX IX_Clientes_Ativo ON Clientes (Ativo);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FormasPagamento')
BEGIN
    CREATE TABLE FormasPagamento (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(80) NOT NULL UNIQUE,
        Descricao NVARCHAR(250) NULL,
        Ativo BIT NOT NULL DEFAULT 1
    );
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Pagamentos')
BEGIN
    CREATE TABLE Pagamentos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ClienteId INT NOT NULL,
        Valor DECIMAL(18,2) NOT NULL CHECK (Valor >= 0),
        DataVencimento DATE NOT NULL,
        DataPagamento DATETIME NULL,
        FormaPagamentoId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pendente'
            CHECK (Status IN ('Pendente', 'Pago', 'Atrasado')),
        Observacao NVARCHAR(500) NULL,
        UsuarioId INT NOT NULL,
        Ativo BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_Pagamentos_Clientes
            FOREIGN KEY (ClienteId) REFERENCES Clientes (Id),
        CONSTRAINT FK_Pagamentos_FormasPagamento
            FOREIGN KEY (FormaPagamentoId) REFERENCES FormasPagamento (Id),
        CONSTRAINT FK_Pagamentos_Usuarios
            FOREIGN KEY (UsuarioId) REFERENCES Usuarios (Id)
    );
    CREATE INDEX IX_Pagamentos_Cliente ON Pagamentos (ClienteId);
    CREATE INDEX IX_Pagamentos_Status ON Pagamentos (Status);
    CREATE INDEX IX_Pagamentos_FormaPagamento ON Pagamentos (FormaPagamentoId);
    CREATE INDEX IX_Pagamentos_Vencimento ON Pagamentos (DataVencimento);
END;";

        using var cmd = new SqlCommand(sql, cn);
        cmd.ExecuteNonQuery();
    }

    private void SeedFormasPagamento()
    {
        var formas = new (string Nome, string Descricao)[]
        {
            ("Dinheiro", "Pagamento em espécie"),
            ("PIX", "Transferência instantânea"),
            ("Cartão de Crédito", "Pagamento com cartão de crédito"),
            ("Cartão de Débito", "Pagamento com cartão de débito"),
            ("Boleto", "Boleto bancário"),
            ("Transferência Bancária", "TED/DOC")
        };

        using var cn = _connection.CreateConnection();
        cn.Open();
        foreach (var (nome, descricao) in formas)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM FormasPagamento WHERE Nome = @Nome)
                    INSERT INTO FormasPagamento (Nome, Descricao) VALUES (@Nome, @Descricao);";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Nome", nome);
            cmd.Parameters.AddWithValue("@Descricao", descricao);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Cria/atualiza os usuários com perfil: Tatiane12 = adm (acessa tudo),
    /// Larissa562 = funcionário (sem acesso ao Mapa Mensal).
    /// Usa upsert para garantir senha e perfil mesmo em bancos antigos.
    /// </summary>
    private void SeedUsuariosComPerfil()
    {
        using var cn = _connection.CreateConnection();
        cn.Open();

        var usuarios = new (string User, string Senha, string Nome, string Perfil)[]
        {
            ("Tatiane12", "Tati120809", "Tatiane", "adm"),
            ("Larissa562", "Lala3650", "Larissa", "func")
        };

        foreach (var (user, senha, nome, perfil) in usuarios)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(senha);
            const string sql = @"
                IF EXISTS (SELECT 1 FROM Usuarios WHERE Username = @U)
                    UPDATE Usuarios SET PasswordHash = @H, Perfil = @P, Ativo = 1
                    WHERE Username = @U;
                ELSE
                    INSERT INTO Usuarios (Username, PasswordHash, NomeCompleto, Email, Ativo, DataCriacao, Perfil)
                    VALUES (@U, @H, @N, '', 1, GETDATE(), @P);";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@U", user);
            cmd.Parameters.AddWithValue("@H", hash);
            cmd.Parameters.AddWithValue("@N", nome);
            cmd.Parameters.AddWithValue("@P", perfil);
            cmd.ExecuteNonQuery();
        }

        // Usuário antigo "Larissa" (criado na primeira versão): vira funcionário,
        // sem acesso ao Mapa Mensal.
        const string sqlLarissaAntiga =
            "UPDATE Usuarios SET Perfil = 'func' WHERE Username = 'Larissa';";
        using (var cmd = new SqlCommand(sqlLarissaAntiga, cn))
            cmd.ExecuteNonQuery();
    }

    private void SeedUsuarioPadrao()
    {
        using var cn = _connection.CreateConnection();
        cn.Open();

        const string existeSql = "SELECT COUNT(1) FROM Usuarios WHERE Username = @Username;";
        using (var cmd = new SqlCommand(existeSql, cn))
        {
            cmd.Parameters.AddWithValue("@Username", "Larissa");
            var count = (int)cmd.ExecuteScalar()!;
            if (count > 0) return;
        }

        // Hash BCrypt REAL gerado em tempo de execução para 'Larissa123'.
        // Nunca armazenamos a senha em texto puro.
        string hash = BCrypt.Net.BCrypt.HashPassword("Larissa123");

        const string insertSql = @"
            INSERT INTO Usuarios (Username, PasswordHash, NomeCompleto, Email, Ativo, DataCriacao)
            VALUES (@Username, @PasswordHash, @NomeCompleto, @Email, 1, GETDATE());";
        using (var cmd = new SqlCommand(insertSql, cn))
        {
            cmd.Parameters.AddWithValue("@Username", "Larissa");
            cmd.Parameters.AddWithValue("@PasswordHash", hash);
            cmd.Parameters.AddWithValue("@NomeCompleto", "Larissa Admin");
            cmd.Parameters.AddWithValue("@Email", "larissa@sistema.com");
            cmd.ExecuteNonQuery();
        }

        // Validação imediata de que o hash gerado verifica corretamente.
        if (!BCrypt.Net.BCrypt.Verify("Larissa123", hash))
            throw new InvalidOperationException("Falha ao validar o hash BCrypt do usuário padrão.");
    }
}
