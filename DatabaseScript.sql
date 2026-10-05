-- =============================================================
-- ClientePagamentoDB - Script de criação do banco de dados
-- SQL Server
-- =============================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ClientePagamentoDB')
BEGIN
    CREATE DATABASE ClientePagamentoDB;
END
GO

USE ClientePagamentoDB;
GO

-- ===================== USUARIOS =====================
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
END
GO

-- ===================== CLIENTES =====================
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
END
GO

-- ===================== FORMAS PAGAMENTO =====================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FormasPagamento')
BEGIN
    CREATE TABLE FormasPagamento (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(80) NOT NULL UNIQUE,
        Descricao NVARCHAR(250) NULL,
        Ativo BIT NOT NULL DEFAULT 1
    );
END
GO

-- ===================== PAGAMENTOS =====================
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
END
GO

-- ===================== SEED: FORMAS DE PAGAMENTO =====================
IF NOT EXISTS (SELECT 1 FROM FormasPagamento WHERE Nome = 'Dinheiro')
    INSERT INTO FormasPagamento (Nome, Descricao) VALUES ('Dinheiro', 'Pagamento em espécie');
IF NOT EXISTS (SELECT 1 FROM FormasPagamento WHERE Nome = 'PIX')
    INSERT INTO FormasPagamento (Nome, Descricao) VALUES ('PIX', 'Transferência instantânea');
IF NOT EXISTS (SELECT 1 FROM FormasPagamento WHERE Nome = 'Cartão de Crédito')
    INSERT INTO FormasPagamento (Nome, Descricao) VALUES ('Cartão de Crédito', 'Pagamento com cartão de crédito');
IF NOT EXISTS (SELECT 1 FROM FormasPagamento WHERE Nome = 'Cartão de Débito')
    INSERT INTO FormasPagamento (Nome, Descricao) VALUES ('Cartão de Débito', 'Pagamento com cartão de débito');
IF NOT EXISTS (SELECT 1 FROM FormasPagamento WHERE Nome = 'Boleto')
    INSERT INTO FormasPagamento (Nome, Descricao) VALUES ('Boleto', 'Boleto bancário');
IF NOT EXISTS (SELECT 1 FROM FormasPagamento WHERE Nome = 'Transferência Bancária')
    INSERT INTO FormasPagamento (Nome, Descricao) VALUES ('Transferência Bancária', 'TED/DOC');
GO

-- ===================== SEED: USUÁRIO PADRÃO =====================
-- A senha NUNCA é gravada em texto puro. O hash BCrypt real de 'Larissa123'
-- é gerado pela aplicação (DatabaseInitializer) na primeira execução,
-- usando BCrypt.Net.BCrypt.HashPassword("Larissa123").
-- Este script SQL apenas garante a estrutura. Caso queira inserir manualmente,
-- gere um hash BCrypt válido e use:
-- INSERT INTO Usuarios (Username, PasswordHash, NomeCompleto, Email)
-- VALUES ('Larissa', '<HASH_BCRYPT>', 'Larissa Admin', 'larissa@sistema.com');
GO
