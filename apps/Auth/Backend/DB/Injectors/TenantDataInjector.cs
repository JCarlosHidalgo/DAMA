using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class TenantDataInjector : DataInjector
{
    public TenantDataInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/Tenant.csv' ")
        .Append("INTO TABLE Tenant ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("LINES TERMINATED BY '\n' ")
        .Append("IGNORE 1 LINES ")
        .Append("(Id, Name, Timezone)");
        _injectionCommand = sb.ToString();
    }
}
