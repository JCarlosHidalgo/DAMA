using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors.Uniques;

public sealed class UniqueClassTeacherInjector : DataInjector
{
    public UniqueClassTeacherInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/UniqueClassTeacher.csv' ")
        .Append("INTO TABLE UniqueClassTeacher ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES");
        _injectionCommand = sb.ToString();
    }
}
