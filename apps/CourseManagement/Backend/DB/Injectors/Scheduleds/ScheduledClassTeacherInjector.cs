using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors.Scheduleds;

public sealed class ScheduledClassTeacherInjector : DataInjector
{
    public ScheduledClassTeacherInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/ScheduledClassTeacher.csv' ")
        .Append("INTO TABLE ScheduledClassTeacher ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES");
        _injectionCommand = sb.ToString();
    }
}
