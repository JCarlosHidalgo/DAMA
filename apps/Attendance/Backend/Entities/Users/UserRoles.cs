namespace Backend.Entities.Users;

public static class UserRoles
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
    public const string Client = "Client";
    public const string Admin = "Admin";

    public const string ClientOrTeacher = Client + "," + Teacher;

    public const string ClientOrStudent = Client + "," + Student;
}
