using System.Text.Json;

using Backend.Builders;
using Backend.Dtos.External.Todotix;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities;
using Backend.Entities.Subscriptions;
using Backend.Entities.Todotix;
using Backend.Options;
using Backend.Services.Abstract;

using Microsoft.Extensions.Options;

using Moq;

namespace Test.Builders;

[TestFixture]
public class SubscriptionCreationBuilderTests
{
    private Mock<ICallbackSignature> _callbackSignature = null!;
    private TodotixOptions _todotixOptions = null!;
    private SubscriptionCreationBuilder _sut = null!;

    [SetUp]
    public void Setup()
    {
        _callbackSignature = new Mock<ICallbackSignature>(MockBehavior.Strict);
        _todotixOptions = new TodotixOptions
        {
            CallbackUrl = "https://payment.example.com/api/payment/subscription/callback"
        };
        _sut = new SubscriptionCreationBuilder(_callbackSignature.Object, Options.Create(_todotixOptions));
    }

    private static SubscriptionPlan NewPlan()
    {
        return new SubscriptionPlan
        {
            Level = 2,
            Price = 180,
            Currency = "BOB",
            DurationAmount = 1,
            DurationUnit = "Month"
        };
    }

    [Test]
    public void BuildPendingPayment_PopulatesAggregateFieldsFromPlan()
    {
        var debtIdentifier = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        SubscriptionPlan plan = NewPlan();
        DateTime expiresAt = DateTime.UtcNow.AddDays(30);

        PendingSubscriptionPayment pending = _sut.BuildPendingPayment(debtIdentifier, tenantId, plan, expiresAt);

        Assert.Multiple(() =>
        {
            Assert.That(pending.Id, Is.EqualTo(debtIdentifier));
            Assert.That(pending.TenantId, Is.EqualTo(tenantId));
            Assert.That(pending.Level, Is.EqualTo(plan.Level));
            Assert.That(pending.Cost, Is.EqualTo(plan.Price));
            Assert.That(pending.Currency, Is.EqualTo(plan.Currency));
            Assert.That(pending.QrImageUrl, Is.Null);
            Assert.That(pending.ExpiresAt, Is.EqualTo(expiresAt));
        });
    }

    [Test]
    public void BuildTodotixRequest_WithEmail_FormatsCallbackUrlAndExpiration()
    {
        var debtIdentifier = Guid.NewGuid();
        SubscriptionPlan plan = NewPlan();
        var expiresAt = new DateTime(2026, 1, 31, 18, 30, 0, DateTimeKind.Utc);
        _callbackSignature.Setup(s => s.Sign(debtIdentifier.ToString("D"))).Returns("sig123");

        RegisterDebtRequest request = _sut.BuildTodotixRequest(debtIdentifier, "client@example.com", plan, "America/La_Paz", "Suscripcion", expiresAt, "platform-key");

        Assert.Multiple(() =>
        {
            Assert.That(request.Appkey, Is.EqualTo("platform-key"));
            Assert.That(request.IdentificadorDeuda, Is.EqualTo(debtIdentifier.ToString()));
            Assert.That(request.EmailCliente, Is.EqualTo("client@example.com"));
            Assert.That(request.Descripcion, Is.EqualTo("Suscripcion"));
            Assert.That(request.LineasDetalleDeuda, Has.Count.EqualTo(1));
        });
        Assert.Multiple(() =>
        {
            Assert.That(request.LineasDetalleDeuda[0].Concepto, Is.EqualTo("Suscripcion"));
            Assert.That(request.LineasDetalleDeuda[0].Cantidad, Is.EqualTo(1));
            Assert.That(request.LineasDetalleDeuda[0].CostoUnitario, Is.EqualTo(plan.Price));
            Assert.That(request.LineasDetalleDeuda[0].DescuentoUnitario, Is.EqualTo(0));
            Assert.That(request.CallbackUrl, Does.Contain("sig=sig123"));
            Assert.That(request.FechaVencimiento, Does.Contain("2026"));
        });
    }

    [Test]
    public void BuildTodotixRequest_WithEmptyEmail_ClearsEmail()
    {
        var debtIdentifier = Guid.NewGuid();
        SubscriptionPlan plan = NewPlan();
        _callbackSignature.Setup(s => s.Sign(It.IsAny<string>())).Returns("sig");

        RegisterDebtRequest request = _sut.BuildTodotixRequest(debtIdentifier, string.Empty, plan, "America/La_Paz", "desc", DateTime.UtcNow.AddDays(1), "platform-key");

        Assert.That(request.EmailCliente, Is.Null);
    }

    [Test]
    public void BuildTodotixRequest_WithExistingCallbackQuery_AppendsSignature()
    {
        _todotixOptions.CallbackUrl = "https://payment.example.com/api/payment/subscription/callback?source=todotix";
        _sut = new SubscriptionCreationBuilder(_callbackSignature.Object, Options.Create(_todotixOptions));
        var debtIdentifier = Guid.NewGuid();
        SubscriptionPlan plan = NewPlan();
        _callbackSignature.Setup(s => s.Sign(It.IsAny<string>())).Returns("sigZ");

        RegisterDebtRequest request = _sut.BuildTodotixRequest(debtIdentifier, "a@b.com", plan, "America/La_Paz", "desc", DateTime.UtcNow.AddDays(1), "platform-key");

        Assert.Multiple(() =>
        {
            Assert.That(request.CallbackUrl, Does.Contain("source=todotix"));
            Assert.That(request.CallbackUrl, Does.Contain("sig=sigZ"));
        });
    }

    [Test]
    public void BuildOutboxEvent_CarriesPayloadAndSubscriptionKind()
    {
        var debtIdentifier = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var todotixRequest = new RegisterDebtRequest { Appkey = "platform-key", IdentificadorDeuda = debtIdentifier.ToString(), Descripcion = "d" };
        DateTime before = DateTime.UtcNow;

        TodotixOutboxEvent outboxEvent = _sut.BuildOutboxEvent(debtIdentifier, tenantId, todotixRequest);

        Assert.Multiple(() =>
        {
            Assert.That(outboxEvent.Id, Is.EqualTo(debtIdentifier));
            Assert.That(outboxEvent.PendingId, Is.EqualTo(debtIdentifier));
            Assert.That(outboxEvent.TenantId, Is.EqualTo(tenantId));
            Assert.That(outboxEvent.DebtKind, Is.EqualTo(DebtKind.TenantSubscription));
            Assert.That(outboxEvent.Status, Is.EqualTo("Pending"));
            Assert.That(outboxEvent.OccurredAt, Is.GreaterThanOrEqualTo(before));
            Assert.That(outboxEvent.PayloadJson, Does.Contain(debtIdentifier.ToString()));
        });

        RegisterDebtRequest? deserialized = JsonSerializer.Deserialize<RegisterDebtRequest>(outboxEvent.PayloadJson);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Appkey, Is.EqualTo("platform-key"));
    }

    [Test]
    public void BuildPendingDebtDto_DefaultAlreadyGenerated_IsFalse()
    {
        var debtIdentifier = Guid.NewGuid();

        QrDebtPendingDto pendingDebt = _sut.BuildPendingDebtDto(debtIdentifier);

        Assert.Multiple(() =>
        {
            Assert.That(pendingDebt.IdentificadorDeuda, Is.EqualTo(debtIdentifier));
            Assert.That(pendingDebt.Status, Is.EqualTo("Pending"));
            Assert.That(pendingDebt.AlreadyGenerated, Is.False);
        });
    }

    [Test]
    public void BuildPendingDebtDto_WhenAlreadyGenerated_IsTrue()
    {
        var debtIdentifier = Guid.NewGuid();

        QrDebtPendingDto pendingDebt = _sut.BuildPendingDebtDto(debtIdentifier, true);

        Assert.That(pendingDebt.AlreadyGenerated, Is.True);
    }
}
