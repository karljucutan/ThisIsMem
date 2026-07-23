# EF Core Migration Commands

Run these commands from the backend API project folder:

`c:\Development\ThisIsMem\src\backend\API`

## Add a migration

```powershell
dotnet ef migrations add <MigrationName> --output-dir Infrastructure/Persistence/Migrations
```

Example:

```powershell
dotnet ef migrations add InitialMemSchema --output-dir Infrastructure/Persistence/Migrations
```

## Update the database

```powershell
dotnet ef database update
```

## Optional: specify environment (if needed)

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet ef database update
```
