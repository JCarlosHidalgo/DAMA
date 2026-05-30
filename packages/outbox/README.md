# DAMA.Software.MySqlOutbox

Helpers for the transactional **Outbox** pattern on MySQL. Extracted from duplicated code in the DAMA Auth and Payment services and published as a NuGet package so every backend that needs an outbox table can consume the same lease / failure recording logic.

## What it provides

- `IOutboxEvent` — interface implemented by every outbox-row entity. Requires `Id` (`Guid`), `OccurredAt` (`DateTime`) and a settable `Attempts` (`int`).
- `OutboxLeaseDescriptor<TEvent>` — record that tells the helper how to read a particular outbox table: table name, SELECT columns, pending predicate, and a mapper from `MySqlDataReader` to `TEvent`.
- `MySqlOutboxLeaseHelper.LeaseAsync<TEvent>(...)` — runs the `SELECT ... FOR UPDATE SKIP LOCKED` + `UPDATE LeasedUntil/Attempts` cycle in a single transaction and returns the leased events.
- `MySqlOutboxLeaseHelper.RecordFailureAsync(...)` — clears the lease and stores the last error on a row.

## Why it exists

Every DAMA outbox table shares the same lease semantics: pick up to N pending rows that nobody is currently leasing, claim them for a few seconds, and let a worker publish them. The schemas around them (column names, additional state columns, terminal transitions) differ by service, but the lease cycle does not. This package owns that lease cycle so it does not have to be copy-pasted across services.

## Requirements on the consumer table

Every outbox table must have, at minimum, columns named:

- `Id` (Guid / CHAR(36))
- `OccurredAt` (DATETIME / TIMESTAMP)
- `Attempts` (INT)
- `LeasedUntil` (DATETIME, nullable)
- `LastError` (VARCHAR / TEXT, nullable)

Other columns are free; the descriptor lets the consumer choose which to project.

## Example

See `OutboxEventDao` in the DAMA Auth service and `PaymentOutboxDao` in the DAMA Payment service for production usage.

## License

MIT.
