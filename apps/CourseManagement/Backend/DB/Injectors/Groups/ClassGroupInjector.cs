using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors.Groups;

public sealed class ClassGroupInjector : DataInjector
{
    public ClassGroupInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/ClassGroup.csv' ")
        .Append("INTO TABLE ClassGroup ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES");
        _injectionCommand = sb.ToString();
    }
}
