using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class UniqueClassAttendanceInjector : DataInjector
{
    public UniqueClassAttendanceInjector()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("LOAD DATA INFILE '/var/lib/mysql-files/UniqueClassAttendanceControl.csv' ")
                     .Append("INTO TABLE UniqueClassAttendance ")
                     .Append("FIELDS TERMINATED BY ',' ")
                     .Append("IGNORE 1 LINES ")
                     .Append("(TenantId, ClassId, ClassDate, StartTime, EndTime, CourseName, StudentId, StudentName)");
        _injectionCommand = stringBuilder.ToString();
    }
}
