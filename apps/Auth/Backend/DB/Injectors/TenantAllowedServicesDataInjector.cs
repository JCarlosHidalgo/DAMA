using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class TenantAllowedServicesDataInjector : DataInjector
{
    public TenantAllowedServicesDataInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/TenantAllowedServices.csv' ")
        .Append("INTO TABLE TenantAllowedServices ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("LINES TERMINATED BY '\n' ")
        .Append("IGNORE 1 LINES ")
        .Append("(Id, IndexCoreServicesPyramid, ExpiresAt)");
        _injectionCommand = sb.ToString();
    }
}
