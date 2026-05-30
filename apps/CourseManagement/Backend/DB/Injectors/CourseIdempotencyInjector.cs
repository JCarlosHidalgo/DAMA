using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class CourseIdempotencyInjector : DataInjector
{
    public CourseIdempotencyInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/CourseIdempotency.csv' ")
        .Append("INTO TABLE CourseIdempotency ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES ")
        .Append("(TenantId, ExternalReference, EntityType, EntityId, ProcessedAt)");
        _injectionCommand = sb.ToString();
    }
}
