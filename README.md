# 💳 ClientePagamentoSystem

Sistema de **Clientes e Controle de Pagamentos** em **C# Windows Forms (.NET 8)** com **SQL Server**, login real com **BCrypt**, dashboard e interface moderna (azul como cor principal).

## ✨ Funcionalidades

- Login real (BCrypt + SQL Server) — usuário `Larissa` / senha `Larissa123`
- Dashboard com cards (clientes, pagamentos pagos/pendentes/atrasados, valores) e gráfico de barras
- CRUD de **Clientes** (validação de CPF, e-mail, CPF não duplicado, pesquisa, desativação)
- CRUD de **Pagamentos** (filtros por cliente/status/forma, exclusão lógica, status automático de atraso)
- Gestão de **Formas de Pagamento** (ativar/desativar sem apagar histórico)
- Inicialização automática do banco `ClientePagamentoDB` (tabelas, seeds)
- Tratamento de erros com detalhes técnicos + log em `logs/erros.log`

## ⚙ Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server ou **SQL Server Express** (instância padrão `.\SQLEXPRESS`)
- VS Code (extensão C# Dev Kit) ou Visual Studio 2022

## ▶ Como executar

```bash
cd ClientePagamentoSystem
dotnet restore
dotnet build
dotnet run
```

### Configurar a conexão

Edite `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=ClientePagamentoDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

- Instância local padrão: `Server=.;` ou `Server=localhost;`
- SQL Server Express: `Server=.\\SQLEXPRESS;`
- Autenticação SQL (em vez de Windows):
  `Server=.\\SQLEXPRESS;Database=ClientePagamentoDB;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True;`

### Verificar se o SQL Server está funcionando

```powershell
Get-Service | Where-Object { $_.Name -like 'MSSQL*' }
```

Se o serviço `MSSQL$SQLEXPRESS` estiver parado:

```powershell
Start-Service MSSQL`$SQLEXPRESS
```

Habilite também o protocolo **TCP/IP** no *SQL Server Configuration Manager* se usar conexão remota.

### Login de teste

| Campo  | Valor        |
|--------|--------------|
| Usuário| `Larissa`    |
| Senha  | `Larissa123` |

A senha é armazenada **somente como hash BCrypt** gerado em tempo de execução no primeiro start (`DatabaseInitializer`) e verificada com `BCrypt.Net.BCrypt.Verify()`.

## 🗂 Estrutura

```
ClientePagamentoSystem/
├── Data/            IDatabaseConnection, DatabaseConnection, DatabaseInitializer
├── Models/          Usuario, Cliente, Pagamento, FormaPagamento
├── Repositories/    UsuarioRepository, ClienteRepository, PagamentoRepository, FormaPagamentoRepository
├── Services/        AuthService, ClienteService, PagamentoService
├── Forms/           LoginForm, MainForm, DashboardView, ClientesView, ClienteForm,
│                    PagamentosView, PagamentoForm, FormasPagamentoView
├── Helpers/         UITheme (tema azul/cards/bordas), ValidationHelper (CPF/e-mail), ErrorHelper (log/erros)
├── Database/        DatabaseScript.sql
├── appsettings.json
└── Program.cs
```

## 🩺 Solução de problemas

**SQL Server não conecta** — verifique o serviço (`Get-Service MSSQL*`), a instância e a `DefaultConnection` no `appsettings.json`. A mensagem de abertura mostra o erro técnico completo.

**Banco não existe** — ele é criado automaticamente (`DatabaseInitializer`). Alternativa: execute `Database/DatabaseScript.sql` no SSMS.

**Tabela/coluna não existe** — compare com o `DatabaseScript.sql` e recrie o objeto faltante.

**Login não funciona** — confirme que o usuário `Larissa` existe, está `Ativo = 1` e que o hash BCrypt é válido. Para recriar: delete a linha em `Usuarios` e reinicie o app (o seed roda de novo).

**BCrypt apresenta erro** — confirme o pacote `BCrypt.Net-Next` com `dotnet restore`; o hash deve começar com `$2`.

**Pacote faltando** — `dotnet restore`.

**Erro de compilação** — `dotnet build` mostra arquivo, linha e mensagem (ex.: `CS0246`).

**Namespace/referência quebrada** — verifique `using`, nomes de classes e classes duplicadas.

**Erro no cadastro/pagamento** — verifique campos obrigatórios, FKs (cliente, forma de pagamento, usuário), datas, valores e a conexão. Detalhes em `logs/erros.log`.
