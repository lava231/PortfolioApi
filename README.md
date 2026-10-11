# PortfolioApi

PortfolioApi is an ASP.NET Core Web API that provides a backend for a simple portfolio site. It is a migration of an original Node.js/Express portfolio project and uses EF Core for data persistence and JWT for authentication.

This repository contains a single Web API project located in the PortfolioApi directory.

Key points
- Target framework: .NET 10
- Database: SQLite in-memory (for tests) and SQL Server (LocalDB by default via DefaultConnection for development)
- Authentication: JWT bearer tokens
- Validation: FluentValidation
- OpenAPI/Swagger UI: Swashbuckle (available at /swagger/index.html in Development)

Main features
- Public portfolio retrieval endpoint
- Contact submission endpoint (public)
- Admin endpoints for listing/exporting/importing contacts and backups (protected by JWT + role policy)
- Authentication endpoints (login + get current admin)
- EF Core migrations and database seeding (optional default admin via environment variables)

Repository structure (important files)
- PortfolioApi/PortfolioApi.csproj — main Web API project
- PortfolioApi/Program.cs — app startup, services and middleware
- PortfolioApi/Controllers — API controllers (AuthController, ContactController, PortfolioController, AdminController, HealthController)
- PortfolioApi/Data — ApplicationDbContext, DbInitializer, EF Core migrations
- PortfolioApi/Models — Entities and DTOs
- PortfolioApi/Repositories — repository interfaces and implementations
- docs/ — migration notes and short developer guidance

Prerequisites
- .NET 10 SDK
- SQL Server or LocalDB (LocalDB used by default connection string if none configured)
- Optional: Visual Studio 2026, VS Code, or Rider

Installation and local setup
1. Clone the repository to your machine.
2. From the repo root, restore and build:

   dotnet restore
   dotnet build

3. Run the API locally:

   dotnet run --project PortfolioApi/PortfolioApi.csproj

   The app listens on the configured URLs (Kestrel). In Development the project will expose Swagger UI at /swagger/index.html and is configured to automatically open the browser.

Configuration and environment variables
- appsettings.json — tracked and used for common settings. Do NOT store secrets here in production.
- appsettings.Development.json — ignored by .gitignore (local overrides); keep local secrets out of source control.

Important configuration keys (placeholders)
- Jwt:Key or JWT_SECRET — symmetric key for signing JWT tokens. Example: set via environment variable JWT_SECRET.
- Jwt:Issuer — token issuer (defaults to "PortfolioApi" if not set).
- DefaultConnection — connection string name in appsettings for EF Core (LocalDB default provided in code if missing).
- ADMIN_EMAIL / ADMIN_PASSWORD or Admin:Email / Admin:Password — used by DbInitializer to seed a default super-admin (optional).
- Backups:Directory — optional folder path where admin backups are written (defaults to ./backups).

Database setup and EF Core migrations
This project uses EF Core migrations stored in PortfolioApi/Data/Migrations. Typical workflow:

- Add a migration:
  dotnet ef migrations add <Name> --project PortfolioApi/PortfolioApi.csproj --startup-project PortfolioApi/PortfolioApi.csproj

- Apply migrations and update database:
  dotnet ef database update --project PortfolioApi/PortfolioApi.csproj --startup-project PortfolioApi/PortfolioApi.csproj

The application will attempt to apply pending migrations on startup (DbInitializer calls MigrateAsync()).

Seeding a default admin
If you want the app to create a default admin on first run, provide either environment variables ADMIN_EMAIL and ADMIN_PASSWORD, or set Admin:Email and Admin:Password in configuration. If these values are missing the seeding step will be skipped (a log warning is emitted).

How to access Swagger UI
- In Development the application enables Swashbuckle and Swagger UI at /swagger/index.html. The app is configured to open the browser automatically on ApplicationStarted while in Development.

Authentication & Authorization
- Login: POST /api/auth/login — returns a JWT token when credentials are valid.
- Current admin: GET /api/auth/me — requires Authorization header Bearer <token>.
- Admin-only endpoints are protected using an authorization policy named "SuperAdminOnly" which requires the role claim value "super-admin".

Main API endpoints (summary)
- POST /api/contact
  - Public endpoint to create a contact submission.
  - Body: { name, email, message }
  - Returns 201 Created with created resource id.

- GET /api/portfolio
  - Public endpoint to retrieve public portfolio data.

- POST /api/auth/login
  - Body: { email, password }
  - Returns: { token, id, name, email, role, lastLogin }

- GET /api/auth/me
  - Requires Authorization: Bearer <token>
  - Returns basic admin profile information.

- GET /api/contact (admin)
  - Protected (SuperAdminOnly). Paged list of contact submissions.

- GET /api/admin/export (admin)
  - Exports contacts and portfolio snapshot as JSON file download.

- POST /api/admin/import (admin)
  - Imports contacts (optionally replacing existing ones) from JSON payload.

- POST /api/admin/backup (admin)
  - Creates a timestamped backup JSON file in Backups:Directory (or ./backups by default).

- GET /api/health
  - Returns a lightweight health response: { status: "OK", message, timestamp }

Example: create contact (curl)

curl -X POST "http://localhost:5075/api/contact" -H "Content-Type: application/json" -d '{"name":"Jane Doe","email":"jane@example.com","message":"Hello"}'

Example: login (curl)

curl -X POST "http://localhost:5075/api/auth/login" -H "Content-Type: application/json" -d '{"email":"admin@example.com","password":"YourPassword"}'

Error responses and validation
- The API uses data annotations and FluentValidation validators. Validation failures return 400 Bad Request with model state details.
- Authentication failures return 401 Unauthorized.
- Not found resources return 404 with a small message payload.

Testing
- There are no automated tests included in the repository. You can manually exercise endpoints via Swagger UI or curl.

Deployment notes
- Ensure Jwt:Key and database connection strings are provided via secure configuration (environment variables or secret manager) in production.
- Do not enable automatic Swagger UI or developer-only features in production.

Security considerations
- Keep Jwt:Key secret and rotate regularly.
- Use HTTPS in production and secure database credentials.
- Do not commit secrets into source control. If sensitive files are already tracked, remove them from Git history carefully.

Sensitive files detection
- I scanned the repository for common secret names and environment files. I did not find explicit plaintext secrets, but the application reads configuration from environment variables (JWT_SECRET, ADMIN_EMAIL, ADMIN_PASSWORD) so ensure these are set securely in production.

License
- No license file was found in the repository. Add a LICENSE file if you wish to license this project.

If you want, I can also:
- Add a CONTRIBUTING.md with local development steps
- Add GitHub Actions workflow for build and optional tests
- Add more detailed Swagger/OpenAPI configuration (XML comments, info, contact)
