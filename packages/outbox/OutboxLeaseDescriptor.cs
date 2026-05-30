using MySql.Data.MySqlClient;

namespace DAMA.Software.MySqlOutbox;

public sealed record OutboxLeaseDescriptor<TEvent>(
    string TableName,
    string SelectColumns,
    string PendingPredicate,
    Func<MySqlDataReader, TEvent> Mapper)
    where TEvent : IOutboxEvent;
