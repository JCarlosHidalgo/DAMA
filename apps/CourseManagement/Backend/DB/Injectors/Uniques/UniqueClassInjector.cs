using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors.Uniques;

public sealed class UniqueClassInjector : DataInjector
{
    public UniqueClassInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/UniqueClass.csv' ")
        .Append("INTO TABLE UniqueClass ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES ")
        .Append("(Id, Date, StartTime, EndTime, CourseId, GroupId, TenantId)");
        _injectionCommand = sb.ToString();
    }
}
