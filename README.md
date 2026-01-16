# Nuclea Scheduler

Nuclea Scheduler is a modern, flexible background job management system based on Quartz.NET, built with .NET 10.0.

## 🚀 Purpose of the Project
This project is designed to centrally manage, monitor, and report tasks that need to be triggered at specific time intervals or manually (e.g., report generation, data synchronization, sending notifications). Its layered architecture makes adding new tasks straightforward.

## 🛠 Technologies Used
- **.NET 10.0**: The latest generation backend development platform.
- **Quartz.NET**: Enterprise-grade job scheduling and triggering library.
- **Entity Framework Core**: Database management and ORM operations.
- **SilkierQuartz**: A modern web dashboard for monitoring and managing Quartz jobs.
- **Microsoft SQL Server**: Database for business data and (optional) logging.
- **Scrutor**: Automatic assembly scanning and service registration for Dependency Injection (DI).

## 🏗 Project Structure
- **Nuclea.Quartz**: The main web project hosting API endpoints and application startup.
- **Nuclea.Quartz.Business**: The layer containing business logic, `IJob` definitions, and core services.
- **Nuclea.Data**: `DbContext`, entity models, and database configurations.
- **Nuclea.Common**: Shared abstract classes, interfaces, and helpers across the project.

## ⚙️ Setup and Execution

### 1. Prerequisites
- .NET 10 Runtime & SDK.
- SQL Server (LocalDB or Full Instance).

### 2. Configuration
Open `Nuclea.Quartz/appsettings.json` and update the `ConnectionStrings` section with your database info:

```json
"ConnectionStrings": {
  "ConnectionString": "Server=YOUR_SERVER;Database=NucleaSchedulerDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3. Running the Application
Navigate to the project directory via terminal and run:

```bash
cd Nuclea.Scheduler/Nuclea.Quartz
dotnet run
```

### 4. Monitoring (Dashboard)
Once the application starts, you can monitor the status of jobs at:
`https://localhost:7197/quartz` (The port may vary on your machine).

**Dashboard Login Info:**
- **Username:** `quartz`
- **Password:** `sda3fda-9bea` (This value can be updated in `appsettings.json`).

## 📝 Adding a New Job
To add a new background job:

1. Create a class under `Nuclea.Quartz.Business/Jobs` inheriting from `BaseQuartzJob`:
   ```csharp
   public class MyNewJob(ILogger logger, IHttpContextAccessor contextAccessor, NucleaDataContext context) 
       : BaseQuartzJob(logger, contextAccessor, context)
   {
       protected override async Task ExecuteJobAsync(IJobExecutionContext context)
       {
           // Implement your business logic here
       }
   }
   ```
2. Add your job to the `AddQuartz` configuration in `DependencyInjection.cs`.

