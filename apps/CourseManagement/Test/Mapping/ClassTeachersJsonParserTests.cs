using Backend.Entities;
using Backend.Mapping;

namespace Test.Mapping;

[TestFixture]
public class ClassTeachersJsonParserTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("[]")]
    public void Parse_WhenNullWhitespaceOrEmptyArray_ReturnsEmptyList(string? json)
    {
        List<ClassTeacher> teachers = ClassTeachersJsonParser.Parse(json!);

        Assert.That(teachers, Is.Empty);
    }

    [Test]
    public void Parse_WhenSingleTeacher_ReturnsParsedTeacher()
    {
        var teacherId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        string json = $"[{{\"TeacherId\":\"{teacherId}\",\"TeacherName\":\"Ada\"}}]";

        List<ClassTeacher> teachers = ClassTeachersJsonParser.Parse(json);

        Assert.That(teachers, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(teachers[0].TeacherId, Is.EqualTo(teacherId));
            Assert.That(teachers[0].TeacherName, Is.EqualTo("Ada"));
        });
    }

    [Test]
    public void Parse_WhenTeacherNameIsNull_DefaultsToEmptyString()
    {
        var teacherId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        string json = $"[{{\"TeacherId\":\"{teacherId}\",\"TeacherName\":null}}]";

        List<ClassTeacher> teachers = ClassTeachersJsonParser.Parse(json);

        Assert.That(teachers[0].TeacherName, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Parse_WhenMultipleTeachers_ReturnsAllInOrder()
    {
        var firstId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var secondId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        string json = $"[{{\"TeacherId\":\"{firstId}\",\"TeacherName\":\"Grace\"}},"
                    + $"{{\"TeacherId\":\"{secondId}\",\"TeacherName\":\"Linus\"}}]";

        List<ClassTeacher> teachers = ClassTeachersJsonParser.Parse(json);

        Assert.That(teachers.Select(teacher => teacher.TeacherId), Is.EqualTo(new[] { firstId, secondId }));
    }
}
