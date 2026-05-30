using System.Linq.Expressions;

using Backend.Dtos;

using FluentValidation;

namespace Backend.Validators.Extensions;

public static class TeacherListRuleExtensions
{
    private const string EmptyTeachersMessage = "Teachers debe contener al menos un docente";
    private const string DuplicateTeacherMessage = "Teachers contiene TeacherId duplicados";

    private static readonly ClassTeacherDtoValidator TeacherValidator = new ClassTeacherDtoValidator();

    public static void MustBeValidTeacherList<TDto>(this AbstractValidator<TDto> validator,
        Expression<Func<TDto, List<ClassTeacherDto>>> teachersSelector)
    {
        validator.RuleFor(teachersSelector)
            .NotNull().WithMessage(EmptyTeachersMessage)
            .Must(teachers => teachers != null && teachers.Count > 0).WithMessage(EmptyTeachersMessage)
            .Must(teachers => teachers == null
                              || teachers.Select(teacher => teacher.TeacherId).Distinct().Count() == teachers.Count)
                .WithMessage(DuplicateTeacherMessage);

        Expression<Func<TDto, IEnumerable<ClassTeacherDto>>> enumerableSelector =
            Expression.Lambda<Func<TDto, IEnumerable<ClassTeacherDto>>>(
                teachersSelector.Body, teachersSelector.Parameters);
        validator.RuleForEach(enumerableSelector).SetValidator(TeacherValidator);
    }
}
