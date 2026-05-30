using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class DebtTemplateInjector : DataInjector
{
    public DebtTemplateInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/DebtTemplate.csv' ")
        .Append("INTO TABLE DebtTemplate ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES");
        _injectionCommand = sb.ToString();
    }
}
