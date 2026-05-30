using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class StudentRemainClassesInjector : DataInjector
{
    public StudentRemainClassesInjector()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("LOAD DATA INFILE '/var/lib/mysql-files/StudentRemainClasses.csv' ")
                     .Append("INTO TABLE StudentRemainClasses ")
                     .Append("FIELDS TERMINATED BY ',' ")
                     .Append("IGNORE 1 LINES ")
                     .Append("(TenantId, Id, NumberOfClasses, StudentName)");
        _injectionCommand = stringBuilder.ToString();
    }
}
