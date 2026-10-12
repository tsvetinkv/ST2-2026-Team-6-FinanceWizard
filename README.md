# FinanceWizard

## Architecture & Design Patterns

### DatabaseConnection Singleton Rationale
We explicitly decoupled database connection option configuration from Entity Framework Core's request context lifecycle:

* **The Problem:** `DbContext` is **not thread-safe** and maintains entity state tracking in memory. Registering `DbContext` as a Singleton causes captive dependencies, thread-access crashes under concurrent requests, and stale cached entity data.

* **The Solution:** We implemented `DatabaseConnection` as a thread-safe Singleton (`Lazy<T>`) that loads and retains connection string details once on application startup.

* **Lifecycle Boundary:** `ApplicationDbContext` remains registered in ASP.NET Core DI as **Scoped** (one instance per request) while dynamically retrieving runtime configuration options via `DatabaseConnection.Instance`.