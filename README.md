# FinanceWizard

FinanceWizard is a personal finance tracker built with ASP.NET Core MVC (.NET 8 or later) and Entity Framework Core on SQL Server. Users manage wallets, transactions, categories, merchants, and tags, see a dashboard of their finances, and can ask an AI financial advisor questions about their spending.

Team project: ST2-2026-Team-6 (Software Technologies, 2026).

---

## Features

- **Accounts:** register, log in, log out, and manage a profile photo.
- **Dashboard:** summary of balances and recent activity.
- **Wallets:** create and manage several wallets, each with its own currency.
- **Transactions:** record income and expenses with category, merchant, tags, and date.
- **Categories, merchants, and tags:** organize transactions. Merchants can have logos.
- **AI Financial Advisor:** chat with an advisor about your spending and savings. Optionally include your recent transactions as anonymized context. Names, emails, and passwords are never sent.
- **Light and dark themes:** switch in the navigation bar; the choice is remembered.

---

## Tech stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC (.NET 8 or later) |
| Data access | Entity Framework Core, SQL Server |
| UI | Razor views, Bootstrap 5, Bootstrap Icons |
| Client scripts | jQuery, jQuery Validation, marked and DOMPurify (AI view, loaded from jsDelivr) |
| AI | Google Gemini REST API, with an offline mock provider as fallback |
| Auth | Cookie authentication |

---

## Project structure

```
ST2-2026-Team-6-FinanceWizard/
├── Controllers/          MVC controllers (Account, Assistant, Category, Home,
│                         Merchant, Tag, Transactions, Wallet)
├── Migrations/           EF Core migrations
├── Models/               View models (for example AssistantViewModel)
├── Repositories/
│   ├── Entities/         Database entities (Transaction, Wallet, Category, Merchant, Tag, ...)
│   ├── ApplicationDbContext.cs
│   ├── DatabaseConnection.cs
│   └── EfRepository / LoggingRepository
├── Services/
│   ├── AI/               AiClientProvider (singleton settings), GeminiProvider, MockProvider
│   ├── Builders/         AiPromptBuilder, TransactionQueryBuilder
│   ├── Contracts/        Interfaces (IAiProvider, ITransactionService, ...)
│   ├── Models/           Service-level DTOs
│   └── *.cs              Service implementations (TransactionService, WalletService, ...)
├── Views/                Razor views grouped by controller, plus Shared/_Layout.cshtml
├── wwwroot/
│   ├── css/              site.css, fw-dashboard.css
│   ├── js/               Client scripts (password-toggle.js)
│   ├── lib/              Bootstrap, Bootstrap Icons, jQuery, jQuery Validation
│   └── uploads/merchants Uploaded merchant logos and profile photos
├── appsettings.json      Non-secret configuration
├── Program.cs            Application startup and dependency injection
└── ST2-2026-Team-6-FinanceWizard.csproj
```

---

## Architecture & Design Patterns

### DatabaseConnection Singleton Rationale

We explicitly decoupled database connection option configuration from Entity Framework Core's request context lifecycle:

* **The Problem:** `DbContext` is **not thread-safe** and maintains entity state tracking in memory. Registering `DbContext` as a Singleton causes captive dependencies, thread-access crashes under concurrent requests, and stale cached entity data.

* **The Solution:** We implemented `DatabaseConnection` as a thread-safe Singleton (`Lazy<T>`) that loads and retains connection string details once on application startup.

* **Lifecycle Boundary:** `ApplicationDbContext` remains registered in ASP.NET Core DI as **Scoped** (one instance per request) while dynamically retrieving runtime configuration options via `DatabaseConnection.Instance`.

### AI Client Singleton (`AiClientProvider`)

* **Purpose:** stores global AI settings only: the API key, model name, and request timeout. It is initialized once at startup from configuration.
* **No user state:** it holds no user data, no chat history, and no database references. It does not depend on any Scoped service (`DbContext`, repositories).
* **Implementation:** classic Singleton with a private constructor, `Lazy<T>`, and a static `Instance` property.
* **Configuration registration:** `IConfiguration` is provided by `WebApplication.CreateBuilder`. The project does not register it as a Singleton manually.

### Strategy Pattern (`IAiProvider`)

* `GeminiProvider` calls the Gemini REST API. It is used when an API key is configured.
* `MockProvider` returns canned responses. It is used when no key is set, so the app runs offline.
* `Program.cs` chooses the implementation at startup.

### Builder Pattern (`AiPromptBuilder`)

Assembles the prompt from a system role, the user's question, and optionally up to 10 recent transactions. Only amount, type, description, date, and currency are included. Names, emails, and passwords are never added.

### Chat history

The advisor's conversation is stored in the user's session (`Advisor_Chat_History`), with a 30-minute idle timeout. It is not written to the database.

---

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) or later
- SQL Server. Any of these works:
  - SQL Server LocalDB (installed with Visual Studio)
  - SQL Server Express
  - A full SQL Server instance
- Visual Studio 2022 (recommended) or VS Code with the C# Dev Kit
- The EF Core command-line tool:
  ```bash
  dotnet tool install --global dotnet-ef
  ```
- A Google Gemini API key (optional; see [AI configuration](#2-ai-configuration-optional))

---

## Getting the code

```bash
git clone <repository-url>
cd ST2-2026-Team-6-FinanceWizard
```

Or download the ZIP from the repository page and extract it.

---

## Configuration

### 1. Connection string

`appsettings.json` contains a default connection string for SQL Server LocalDB:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=FinanceWizard;Trusted_Connection=True;"
}
```

If you use a different SQL Server instance, change this value. Do not commit a connection string that contains a password. Store it in user-secrets instead:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-connection-string>"
```

### 2. AI configuration (optional)

The model name is in `appsettings.json`:

```json
"Gemini": {
  "Model": "gemini-3.5-flash-lite"
}
```

The API key is never stored in the project. Set it with user-secrets from the project folder:

```bash
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "<your-gemini-api-key>"
```

Get a key from [Google AI Studio](https://aistudio.google.com/). If no key is set, the advisor runs in offline mode and returns canned responses.

> Free-tier quotas are limited per model and reset daily. If you get HTTP 429, wait or switch to another model in `appsettings.json`.

---

## Running the project

### With Visual Studio

1. Open `ST2-2026-Team-6-FinanceWizard.sln`.
2. Set the project as the startup project.
3. Press **F5** (or Ctrl+F5 without the debugger).

### With the command line

```bash
dotnet restore
dotnet build
dotnet run
```

The browser opens at the URL shown in the console, typically `https://localhost:7xxx`.

To stop the app: press **Shift+F5** in Visual Studio, or **Ctrl+C** in the terminal.

---

## Database setup

Apply the EF Core migrations before the first run:

```bash
dotnet ef database update
```

On startup the app also seeds initial data through `DbInitializer.SeedAsync`, so the database is populated automatically if it is empty.

To add a migration after changing an entity:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

To reset the database completely (this deletes all data):

```bash
dotnet ef database drop
dotnet ef database update
```

---

## Troubleshooting

| Problem | Fix |
|---|---|
| `Failed to load configuration ... appsettings.json` | The JSON is malformed. Check for missing commas or braces. |
| `Connection string 'DefaultConnection' was not found` | Add the connection string as shown above. |
| HTTP 404 from the AI advisor | The model name in `appsettings.json` is retired or wrong. Check the available models and update it. |
| HTTP 403 from the AI advisor | The API key is invalid, or the Generative Language API is not enabled for its project. |
| HTTP 429 from the AI advisor | The daily or per-minute quota for the model is used up. Wait, or change the model. |
| `No executable found matching command "dotnet-ef"` | Install the tool: `dotnet tool install --global dotnet-ef`. |
| Advisor replies appear as plain text | Check that the browser can reach the jsDelivr CDN. |

---

## Security notes

- Secrets (API keys, passwords) are kept in user-secrets or environment variables, not in source control.
- Database access is scoped per request. No `DbContext` is shared between users.
- The advisor sends only transaction amount, type, description, date, and currency. It never sends names, emails, or passwords.
- Model output is sanitized with DOMPurify before it is rendered.
- All state-changing forms use anti-forgery tokens.
