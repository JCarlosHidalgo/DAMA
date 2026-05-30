using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Infrastructure;

public static class UnitOfWorkMockHelper
{
    public static (Mock<IUnitOfWork> UnitOfWork, Mock<ITransactionScope> Scope) BuildCommittingMocks()
    {
        Mock<ITransactionScope> scope = new(MockBehavior.Strict);
        scope.Setup(s => s.CommitAsync()).Returns(Task.CompletedTask);
        scope.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);

        Mock<IUnitOfWork> unitOfWork = new(MockBehavior.Strict);
        unitOfWork.Setup(unit => unit.BeginAsync()).ReturnsAsync(scope.Object);
        return (unitOfWork, scope);
    }
}
