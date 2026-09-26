# AgentFrameworkPlayground

A hobby playground for building AI agents with **Microsoft Agent Framework** on **.NET 10**, using **Azure AI Foundry** for models and **PostgreSQL + pgvector** for retrieval-augmented generation (RAG).

> **Status:** work in progress. The todo app, chat panel UI and database setup are done; the agent itself is not wired up yet (see [Roadmap](#roadmap)).

## Features

- **Todo list:** add, delete and mark items as done. Data is stored in PostgreSQL via EF Core.
- **Chat panel:** a floating chat window in the bottom-right corner of every page. It currently returns a placeholder reply until the agent backend is connected.
- **AI providers page:** store connection settings for Azure AI Foundry (API key or Entra ID), OpenAI and OpenAI-compatible servers such as Ollama or LM Studio. Several providers can be saved; one is marked active.
- **Vector-ready database:** PostgreSQL 17 with the pgvector extension, running in Docker.

## Tech stack

| Area | Technology |
|---|---|
| Web app | ASP.NET Core 10 Razor Pages, Bootstrap 5 |
| Data access | EF Core 10, Npgsql, snake_case naming convention |
| Database | PostgreSQL 17 + pgvector (Docker) |
| AI (planned) | Microsoft Agent Framework, Azure AI Foundry |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Visual Studio 2026, or VS Code with the C# Dev Kit
- *(later)* An [Azure AI Foundry](https://ai.azure.com) project with a chat model and an embedding model deployed

## Getting started

### 1. Clone the repository

```bash
git clone https://github.com/bahadiremre/AgentFrameworkPlayground.git
cd AgentFrameworkPlayground
```

### 2. Start the database

Copy the example environment file and set your own password:

```bash
cp .env.example .env
```

On Windows PowerShell: `Copy-Item .env.example .env`

Edit `.env` and replace `change-me` with a strong password. Then start PostgreSQL:

```bash
docker compose up -d --wait
```

The database listens on `127.0.0.1:5432` only, so it is not reachable from other machines on your network. On first start, the scripts in `db/init/` enable the `vector` extension.

### 3. Configure the connection string

The connection string is kept in [.NET user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), never in `appsettings.json`. Use the same values as in your `.env` file:

```bash
cd AgentFw/AgentFw
dotnet user-secrets set "ConnectionStrings:AgentRag" "Host=localhost;Port=5432;Database=agentrag;Username=agentrag;Password=<your-password>"
```

In Visual Studio you can also right-click the project and choose **Manage User Secrets**.

### 4. Run the app

```bash
dotnet run
```

Or open `AgentFw/AgentFw.slnx` in Visual Studio and press **F5**. Pending EF Core migrations are applied automatically on startup.

## Project structure

```
.
├── AgentFw/
│   ├── AgentFw.slnx
│   └── AgentFw/                 # ASP.NET Core Razor Pages app
│       ├── Data/                # EF Core DbContext, entities and migrations
│       ├── Pages/               # Razor Pages (todo page, layout, chat panel)
│       └── wwwroot/             # Static files (CSS, JS, libraries)
├── db/init/                     # SQL scripts run on the first database start
├── docker-compose.yml           # PostgreSQL + pgvector
└── .env.example                 # Template for database credentials
```

## Database migrations

After changing an entity, add a migration from the `AgentFw/AgentFw` folder:

```bash
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
```

In Visual Studio's Package Manager Console:

```powershell
Add-Migration <MigrationName> -OutputDir Data/Migrations
```

The migration is applied the next time the app starts. To apply it without running the app, use `dotnet ef database update` or `Update-Database`.

## Security notes

- Secrets live in `.env` (for Docker) and user secrets (for the app). Both stay out of git.
- `.env.example` only contains placeholder values.
- AI provider API keys entered in the app are encrypted with [ASP.NET Core Data Protection](https://learn.microsoft.com/aspnet/core/security/data-protection/introduction) before they reach the database. The encryption keys stay in your user profile, so a database dump alone does not reveal the API keys. The UI only ever shows the last four characters.
- API keys are never sent over plain `http` to a non-local address.
- The app has no login. It is meant to run on your own machine (`localhost`); don't expose it to a network as-is.
- Dependabot checks NuGet packages and the Docker image weekly.

## Roadmap

- [x] Todo list backed by PostgreSQL
- [x] Chat panel UI
- [x] PostgreSQL + pgvector in Docker
- [x] AI providers page with encrypted API key storage
- [ ] Connect the chat panel to an agent built with Microsoft Agent Framework and Azure AI Foundry
- [ ] Document upload page
- [ ] RAG: chunk documents, store embeddings in pgvector, answer questions from them

## License

This project is licensed under the [MIT License](LICENSE).
