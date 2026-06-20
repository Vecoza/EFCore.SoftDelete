# EFCore.SoftDelete

A lightweight .NET 10 library that adds automatic soft-delete behaviour to any Entity Framework Core application.

Mark an entity as soft-deletable, call `Remove()` like you always do, and the library intercepts the delete, sets `IsDeleted` / `DeletedAt`, and keeps the row hidden from normal queries via an EF Core global query filter. Zero configuration beyond a marker interface.

## Features

- **Transparent interception** — `context.Remove(entity)` and `SaveChanges()`/`SaveChangesAsync()` work as usual; deletes are rewritten to updates under the hood.
- **Automatic query filtering** — soft-deleted rows are excluded from every query by default, for every entity implementing `ISoftDeletable`.
- **Opt-in access to deleted data** — `IncludeDeleted<T>()` and `OnlyDeleted()` let you query past the filter when you need to.
- **Restore support** — a single `Restore()` extension method to undelete an entity.
- **No reflection-heavy magic at query time** — the query filter is built once, per entity type, in `OnModelCreating`.

## Installation

```bash
dotnet add package EFCore.SoftDelete
```

## Requirements

- .NET 10
- Entity Framework Core 10

## Usage

### 1. Implement `ISoftDeletable` on your entity

```csharp
public class Product : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

### 2. Derive your `DbContext` from `SoftDeleteDbContext`

```csharp
public class AppDbContext : SoftDeleteDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
}
```

### 3. Register it with DI

```csharp
services.AddSoftDeleteDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### 4. Delete and query as normal

```csharp
// Soft-deletes the entity: sets IsDeleted = true, DeletedAt = UtcNow,
// instead of issuing a DELETE.
context.Products.Remove(product);
await context.SaveChangesAsync();

// Excludes soft-deleted rows automatically.
var activeProducts = await context.Products.ToListAsync();
```

### Querying deleted rows

```csharp
// All rows, including soft-deleted ones.
var all = context.IncludeDeleted<Product>();

// Only soft-deleted rows.
var deleted = context.Products.OnlyDeleted();
```

### Restoring a soft-deleted entity

```csharp
var product = await context.IncludeDeleted<Product>()
    .FirstAsync(p => p.Id == id);

product.Restore(); // IsDeleted = false, DeletedAt = null
await context.SaveChangesAsync();
```

## How it works

- `SoftDeleteDbContext.OnModelCreating` applies a `HasQueryFilter(e => !e.IsDeleted)` to every entity type implementing `ISoftDeletable`.
- `SaveChanges`/`SaveChangesAsync` are overridden to scan the change tracker for entries in the `Deleted` state that implement `ISoftDeletable`. Each such entry is switched to `Modified` and its `IsDeleted`/`DeletedAt` properties are set, so EF Core issues an `UPDATE` instead of a `DELETE`.

## License

MIT
