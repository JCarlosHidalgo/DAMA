using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class UserDataInjector : DataInjector
{
    public UserDataInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/Users.csv' ")
        .Append("INTO TABLE User ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("LINES TERMINATED BY '\n' ")
        .Append("IGNORE 1 LINES ")
        .Append("(Id, UserName, PasswordHash, Role)");
        _injectionCommand = sb.ToString();
    }
}
