using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors.Scheduleds;

public sealed class ScheduledClassInjector : DataInjector
{
    public ScheduledClassInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/ScheduledClass.csv' ")
        .Append("INTO TABLE ScheduledClass ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES ")
        .Append("(Id, DayOfWeekIndex, StartTime, EndTime, CourseId, GroupId, TenantId)");
        _injectionCommand = sb.ToString();
    }
}
