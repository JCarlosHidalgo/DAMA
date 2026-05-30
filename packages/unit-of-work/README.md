# DAMA.Software.MySqlUnitOfWork

Unit-of-work abstraction over a scoped MySQL connection for the DAMA backend services. Extracted so that service-layer code and DAO **interfaces** never reference `MySql.Data` types: the transaction is handed around as an opaque `ITransactionContext` and only the concrete (MySQL) DAOs unwrap it.

## What it provides

- `ITransactionContext` — opaque handle that DAO methods accept instead of `MySqlTransaction`.
- `ITransactionScope` — an `ITransactionContext` that is also `IAsyncDisposable` and exposes `CommitAsync()`. Disposing without committing rolls the transaction back.
- `IUnitOfWork.BeginAsync()` — opens the scoped connection (via `MySQLRetryPolicy.EnsureOpenAsync`) and starts a transaction, returning an `ITransactionScope`.
- `MySqlUnitOfWork` — the MySQL implementation, constructed from the request-scoped `MySqlConnection`.
- `MySqlTransactionContextAccessor.Unwrap(context)` — used inside concrete MySQL DAOs to recover the underlying `MySqlTransaction`.
- `IUnitOfWork.RunInTransactionAsync(...)` — extension overloads that wrap `BeginAsync`/`CommitAsync`: one returns `(TResult Result, bool ShouldCommit)` so the work can ask for a rollback without throwing, the other always commits.

## Why it exists

Before this package, services injected `MySqlConnection` directly, called `BeginTransactionAsync()`/`CommitAsync()` by hand, and DAO interfaces took a `MySqlTransaction` parameter. That tied the business layer to MySQL and made unit testing awkward. This package owns the transaction lifecycle so the business layer depends only on `IUnitOfWork` / `ITransactionContext`.

## Usage

Register in `Program.cs`:

```csharp
builder.Services.AddScoped<IUnitOfWork, MySqlUnitOfWork>();
```

In a service:

```csharp
await using ITransactionScope scope = await unitOfWork.BeginAsync();
bool created = await userDao.TryCreateAsync(user, scope);
if (!created)
{
    return null;
}
await tenantDomainDao.CreateAsync(tenantDomain, scope);
await scope.CommitAsync();
```

In a concrete MySQL DAO:

```csharp
public async Task CreateAsync(TenantDomain domain, ITransactionContext transaction)
{
    MySqlTransaction tx = MySqlTransactionContextAccessor.Unwrap(transaction);
    MySqlCommand command = new MySqlCommand(sql, _connection, tx);
    ...
}
```

## License

MIT.
