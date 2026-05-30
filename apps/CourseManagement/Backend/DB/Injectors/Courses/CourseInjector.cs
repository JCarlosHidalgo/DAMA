using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors.Courses;

public sealed class CourseInjector : DataInjector
{
    public CourseInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/Course.csv' ")
        .Append("INTO TABLE Course ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES");
        _injectionCommand = sb.ToString();
    }
}
