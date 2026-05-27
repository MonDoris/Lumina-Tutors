# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Lumina Tutors** is a multi-tenant education management system for Vietnamese schools, built on ASP.NET Core 8.0 MVC with Clean Architecture. It supports 6 roles (Admin, Teacher, Student, Parent, Supervisor, Accountant) and covers academic management, attendance (QR/manual/face recognition), grading, finance, HR, and communication.

## Build & Run Commands

```powershell
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the web app (development)
dotnet run --project src/LuminaTutors.Web

# Run all tests
dotnet test

# Run unit tests only
dotnet test tests/LuminaTutors.UnitTests

# Run integration tests only
dotnet test tests/LuminaTutors.IntegrationTests

# Run a single test by name
dotnet test tests/LuminaTutors.UnitTests --filter "FullyQualifiedName~ClassName.MethodName"

# Add a new EF Core migration
dotnet ef migrations add <MigrationName> --project src/LuminaTutors.Infrastructure --startup-project src/LuminaTutors.Web

# Apply migrations
dotnet ef database update --project src/LuminaTutors.Infrastructure --startup-project src/LuminaTutors.Web
```

Development server runs on `https://localhost:60480` (HTTPS) or `http://localhost:60481` (HTTP).

## Architecture

Clean Architecture with 4 layers — each layer depends only on layers inward:

```
Domain → Application → Infrastructure → Web
```

### Domain (`src/LuminaTutors.Domain/`)
No external dependencies. Contains:
- **Entities**: Organized by feature subdirectory. All entities inherit from one of three base classes:
  - `BaseEntity`: just `Id`
  - `AuditableEntity`: adds `CreatedAt`, `UpdatedAt`
  - `TenantEntity`: adds `SchoolId` for multi-tenancy isolation
- **Interfaces**: `IRepository<T>` and `IUnitOfWork` contracts
- **Result pattern**: `Result<T>` returned by all services for consistent error handling
- **Enums**: `RoleCode` (6 fixed roles), `EducationLevel`, `AttendanceStatus`, `CheckMethod`, etc.

### Application (`src/LuminaTutors.Application/`)
Depends on Domain only. Contains:
- **Services**: 11+ feature services (Auth, Student, Class, Attendance, Grading, Finance, HR, Discipline, Notification, Message, NewsBoard)
- **DTOs**: Input/output data transfer objects per feature
- **AutoMapper profiles**: `Mappings/MappingProfile.cs`
- **FluentValidation**: DTO validators
- All service methods return `Result<T>` with Vietnamese error messages

### Infrastructure (`src/LuminaTutors.Infrastructure/`)
Depends on Domain + Application. Contains:
- **`LuminaTutorsDbContext`**: 40+ DbSet properties, assembly-based Fluent API configuration, `AuditInterceptor` for auto-stamping timestamps, all enums stored as strings
- **Entity configurations**: `Data/Configurations/` using `IEntityTypeConfiguration<T>`
- **`GenericRepository<T>`** and **`UnitOfWork`**: implement repository pattern
- **`DatabaseSeeder`**: development seed data (runs only in Development environment)
- **Migrations**: in `Migrations/` directory

### Web (`src/LuminaTutors.Web/`)
ASP.NET Core MVC. Contains:
- **Controllers**: Area-based routing; 8 controllers covering Auth, Dashboard, Class, Student, Attendance, Grading, Finance, Supervisor
- **Authentication**: Cookie-based (not JWT-based despite JwtBearer package), 8-hour expiry, sliding expiration, `HttpOnly`/`Secure`/`SameSite=Strict`
- **Authorization policies**: `AdminOnly`, `TeacherOrAdmin`, `FinanceAccess`, `SupervisorAccess`, `AnyAuthenticated`
- **Claims**: `Role`, `UserId`, `SchoolId` stored in `ClaimsPrincipal`
- **Logging**: Serilog to console + daily rolling files in `logs/` (30-day retention)

## Key Patterns

**Repository + Unit of Work**: Access data only through `IUnitOfWork`, never `DbContext` directly in Application/Web layers.

**Result pattern**: Services return `Result` or `Result<T>`. Check `.IsSuccess` before using `.Value`; `.Error` contains a Vietnamese-language message for display.

**Multi-tenancy**: Every `TenantEntity` carries `SchoolId`. When querying, always filter by `SchoolId` from the authenticated user's claims.

**Seeding**: `DatabaseSeeder.SeedAsync()` is called at startup in Development only; do not add production data here.

**EF configuration**: Use `IEntityTypeConfiguration<T>` classes in `Infrastructure/Data/Configurations/` rather than `OnModelCreating`.

## Configuration

- Connection string name: `LuminaTutorsDb` (production) / LocalDB in `appsettings.Development.json`
- JWT secret is a placeholder — must be replaced before production deployment
- File uploads land in `wwwroot/uploads`, max 50 MB
- QR attendance tokens expire in 10 minutes; invite links expire in 3 days

## Testing

- **Unit tests**: xUnit + Moq + FluentAssertions; target Application and Domain layers
- **Integration tests**: xUnit + SpecFlow (BDD) + WebApplicationFactory with In-Memory EF Core + Selenium for browser tests
- Integration tests use an in-memory database, not SQL Server — be aware of SQL Server-specific behavior differences
