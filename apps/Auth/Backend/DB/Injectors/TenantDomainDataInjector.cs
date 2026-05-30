using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class TenantDomainDataInjector : DataInjector
{
    public TenantDomainDataInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/TenantDomain.csv' ")
        .Append("INTO TABLE TenantDomain ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("LINES TERMINATED BY '\n' ")
        .Append("IGNORE 1 LINES ")
        .Append("(UserId, TenantId)");
        _injectionCommand = sb.ToString();
    }
}
