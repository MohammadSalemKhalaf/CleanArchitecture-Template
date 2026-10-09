# Infrastructure layer guide

This project connects the application to the outside world. It contains the SQL Server database through EF Core, the cache (in-process memory, plus optional Redis) and the health checks. It implements the interfaces that Application declares.

- Rules: [RULES.md](RULES.md)
- The whole picture: [docs/ARCHITECTURE-GUIDE.md](../../docs/ARCHITECTURE-GUIDE.md)

## References

- **Application** (and through it, Domain): this layer implements `IAppDbContext` and `ICacheService`, and maps the entities.
- **Packages:**
  - EF Core SQL Server provider;
  - HybridCache and the StackExchange Redis cache;
  - EF Core health checks;
  - options binding.

It must not reference the Api or Contracts.

## Folders and files

| Path | Contents |
|---|---|
| [DependencyInjection.cs](DependencyInjection.cs) | `AddInfrastructure(configuration)`, split into `AddPersistence` and `AddCaching`. It maps `IAppDbContext` → `AppDbContext` and `ICacheService` → `HybridCacheService`. |
| [Data/AppDbContext.cs](Data/AppDbContext.cs) | The EF Core context. It applies every configuration in this assembly and turns SQL duplicate-key errors (2601, 2627) into `UniqueConstraintViolationException`. |
| [Data/Configurations/](Data/Configurations/TodoItemConfiguration.cs) | One `IEntityTypeConfiguration<T>` per entity: table, keys, lengths, indexes. |
| [Data/Interceptors/AuditableEntityInterceptor.cs](Data/Interceptors/AuditableEntityInterceptor.cs) | Before each save, stamps the audit columns using `IUser` and `TimeProvider`. |
| `Data/Migrations/` | Generated EF Core migrations. Do not edit them by hand. |
| [Data/ApplicationDbContextInitialiser.cs](Data/ApplicationDbContextInitialiser.cs) | `MigrateAsync` and `SeedAsync`. Used at startup (when `Database:*` allows it), by `--migrate` and by tests. |
| [Caching/HybridCacheService.cs](Caching/HybridCacheService.cs) | `ICacheService` over HybridCache. It fails open, meaning cache errors are logged and reads go to the database. |
| [Caching/RedisCacheTagIndex.cs](Caching/RedisCacheTagIndex.cs) | Tracks which keys belong to which tag in Redis, so invalidation reaches every instance. When Redis is not configured, `NullCacheTagIndex` is used instead. |
| [Caching/RedisHealthCheck.cs](Caching/RedisHealthCheck.cs) | Reports `Degraded`, not `Unhealthy`, when Redis is down. |
| [Settings/](Settings/DatabaseOptions.cs) | `DatabaseOptions` (`Database:InitializeOnStartup`, `Database:SeedSampleData`) and `CachingOptions` (`Caching:LocalExpiration`, `Caching:RedisInstanceName`). |
| [ConnectionStringNames.cs](ConnectionStringNames.cs), [HealthCheckTags.cs](HealthCheckTags.cs) | Names shared with the Api: `Database`, `Redis`, `ready`. |

## How the configuration switches work

- `ConnectionStrings:Database` is **required**. Startup fails if it is missing (`ValidateOnStart`).
- `ConnectionStrings:Redis` is **optional**:
  - empty means HybridCache uses process memory only;
  - set means Redis becomes the shared second tier, and a `redis` readiness check is added.
- `Database:InitializeOnStartup` and `Database:SeedSampleData` are read by `InitialiseDatabaseAsync`, which `Program.cs` calls.

## What belongs here and what does not

| Belongs here | Stays outside |
|---|---|
| EF configurations, migrations, interceptors | Business rules (Domain) |
| Implementations of Application interfaces | Use cases and queries (Application handlers) |
| Option classes for settings | Controllers, middleware, authentication (Api) |
| Health checks for external systems | |

## When you edit it

When you add an entity:

1. add `public DbSet<X> Xs => Set<X>();` to `AppDbContext` (and the matching property on `IAppDbContext`);
2. add `Data/Configurations/XConfiguration.cs`, including indexes for filtered, sorted or unique columns;
3. create a migration:

   ```bash
   dotnet ef migrations add <Name> --project src/TemplateApp.Infrastructure --startup-project src/TemplateApp.Api --output-dir Data/Migrations
   ```

4. add tests in `tests/TemplateApp.Infrastructure.IntegrationTests/Data/` for any new constraint or index behaviour. `Migrations_MatchTheCurrentModel` fails if you forget the migration.

Also edit this layer when you add a new external service: put its interface in Application, and its implementation and registration here.
