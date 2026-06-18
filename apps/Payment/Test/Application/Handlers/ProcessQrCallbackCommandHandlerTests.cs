using Backend.Application.Callbacks;
using Backend.Application.Commands;
using Backend.Application.Handlers;
using Backend.Application.Results;

using Moq;

namespace Test.Application.Handlers;

[TestFixture]
public class ProcessQrCallbackCommandHandlerTests
{
    private static ProcessQrCallbackCommand NewCommand(Guid transactionId) => new(transactionId, 0, 0);

    [Test]
    public async Task Handle_WhenFirstStrategyHandles_ReturnsProcessed()
    {
        var transactionId = Guid.NewGuid();
        Mock<IDebtCallbackStrategy> first = new(MockBehavior.Strict);
        Mock<IDebtCallbackStrategy> second = new(MockBehavior.Strict);
        first.Setup(s => s.TryHandleAsync(transactionId)).ReturnsAsync(true);

        ProcessQrCallbackCommandHandler sut = new(new[] { first.Object, second.Object });

        ProcessQrCallbackResult result = await sut.Handle(NewCommand(transactionId));

        Assert.That(result, Is.TypeOf<ProcessQrCallbackResult.Processed>());
        second.Verify(s => s.TryHandleAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Handle_WhenLaterStrategyHandles_ReturnsProcessed()
    {
        var transactionId = Guid.NewGuid();
        Mock<IDebtCallbackStrategy> first = new(MockBehavior.Strict);
        Mock<IDebtCallbackStrategy> second = new(MockBehavior.Strict);
        first.Setup(s => s.TryHandleAsync(transactionId)).ReturnsAsync(false);
        second.Setup(s => s.TryHandleAsync(transactionId)).ReturnsAsync(true);

        ProcessQrCallbackCommandHandler sut = new(new[] { first.Object, second.Object });

        ProcessQrCallbackResult result = await sut.Handle(NewCommand(transactionId));

        Assert.That(result, Is.TypeOf<ProcessQrCallbackResult.Processed>());
    }

    [Test]
    public async Task Handle_WhenNoStrategyHandles_ReturnsDebtNotFound()
    {
        var transactionId = Guid.NewGuid();
        Mock<IDebtCallbackStrategy> first = new(MockBehavior.Strict);
        first.Setup(s => s.TryHandleAsync(transactionId)).ReturnsAsync(false);

        ProcessQrCallbackCommandHandler sut = new(new[] { first.Object });

        ProcessQrCallbackResult result = await sut.Handle(NewCommand(transactionId));

        Assert.That(result, Is.TypeOf<ProcessQrCallbackResult.DebtNotFound>());
    }
}
