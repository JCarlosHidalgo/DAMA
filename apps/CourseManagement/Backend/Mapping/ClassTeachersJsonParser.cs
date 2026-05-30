using System.Text.Json;

using Backend.Entities;

namespace Backend.Mapping;

public static class ClassTeachersJsonParser
{
    public static List<ClassTeacher> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
        {
            return new List<ClassTeacher>();
        }

        using JsonDocument document = JsonDocument.Parse(json);
        List<ClassTeacher> teachers = new List<ClassTeacher>(document.RootElement.GetArrayLength());
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            teachers.Add(new ClassTeacher
            {
                TeacherId = Guid.Parse(element.GetProperty("TeacherId").GetString()!),
                TeacherName = element.GetProperty("TeacherName").GetString() ?? string.Empty
            });
        }
        return teachers;
    }
}
