# PortfolioApi (ASP.NET Core)

This project is a migration of a Node.js/Express Portfolio API to ASP.NET Core Web API using EF Core and JWT authentication.

Development notes:
- Target framework: .NET 10
- Database: SQL Server (LocalDB by default). Connection string: DefaultConnection in appsettings.json
- Run locally: dotnet run --project PortfolioApi/PortfolioApi.csproj
- EF Migrations: dotnet ef migrations add <Name> --project PortfolioApi/PortfolioApi.csproj --startup-project PortfolioApi/PortfolioApi.csproj
- Apply migrations: dotnet ef database update --project PortfolioApi/PortfolioApi.csproj --startup-project PortfolioApi/PortfolioApi.csproj

Security:
- JWT secret configured via Jwt:Key or JWT_SECRET environment variable.
