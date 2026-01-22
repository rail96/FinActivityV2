# FindActivity

FindActivity is an ASP.NET Core MVC (.NET 8) MVP for creating and discovering local activities. Users can register/login, create activities, search/filter, join/leave, and leave reviews after completion.

## Tech stack
- ASP.NET Core MVC (.NET 8)
- EF Core (SQL Server LocalDB)
- ASP.NET Core Identity
- Razor views (Bootstrap)

## First-time setup
### Prerequisites
- .NET SDK 8.x
- SQL Server LocalDB (included with Visual Studio) or SQL Server

### Configure database connection
The default connection string is in `FindActivity.Web/appsettings.json`:
```
Server=(localdb)\mssqllocaldb;Database=FindActivityDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```
If you use a different SQL Server instance, update this value.

### Install EF Core CLI (once)
```
dotnet tool install --global dotnet-ef
```

### Create database and schema
From the solution root:
```
dotnet ef migrations add InitialCreate --project FindActivity.Infrastructure --startup-project FindActivity.Web --output-dir Data/Migrations
dotnet ef database update --project FindActivity.Infrastructure --startup-project FindActivity.Web
```

## Run the app
From the solution root:
```
dotnet run --project FindActivity.Web
```
Open the URL shown in the console (typically `https://localhost:7xxx`).

## Notes
- Categories are seeded automatically on first migration.
- Reviews are only allowed after the activity is completed and the user joined.
