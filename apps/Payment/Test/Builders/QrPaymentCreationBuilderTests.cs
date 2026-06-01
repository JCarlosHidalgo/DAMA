using System.Text.Json;

using Backend.Builders;
using Backend.Dtos.External.Todotix;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities;
using Backend.Entities.DebtTemplates;
using Backend.Entities.Todotix;
using Backend.Events;
using Backend.Options;
using Backend.Services.Abstract;

using Microsoft.Extensions.Options;

using Moq;

namespace Test.Builders;

[TestFixture]
public class QrPaymentCreationBuilderTests
{
    private Mock<ICallbackSignature> callbackSignature = null!;
    private TodotixOptions todotixOptions = null!;
    private QrPaymentCreationBuilder sut = null!;

    [SetUp]
    public void Setup()
    {
        callbackSignature = new Mock<ICallbackSignature>(MockBehavior.Strict);
        todotixOptions = new TodotixOptions
        {
            CallbackUrl = "https://payment.example.com/api/payment/qr/callback"
        };
        sut = new QrPaymentCreationBuilder(callbackSignature.Object, Options.Create(todotixOptions));
    }

    [Test]
    public void BuildPendingPayment_PopulatesAggregateFieldsFromTemplate()
    {
        var debtId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var template = new DebtTemplate { Id = templateId, TenantId = tenantId, Description = "d", ClassQuantity = 10, Cost = 99 };
        DateTime expiresAt = DateTime.UtcNow.AddDays(3);

        Backend.Entities.QrPayments.PendingQrPayment pending = sut.BuildPendingPayment(debtId, tenantId, studentId, templateId, template, expiresAt);

        Assert.Multiple(() =>
        {
            Assert.That(pending.Id, Is.EqualTo(debtId));
            Assert.That(pending.TenantId, Is.EqualTo(tenantId));
            Assert.That(pending.StudentId, Is.EqualTo(studentId));
            Assert.That(pending.TemplateId, Is.EqualTo(templateId));
            Assert.That(pending.ClassQuantity, Is.EqualTo(10));
            Assert.That(pending.Cost, Is.EqualTo(99));
            Assert.That(pending.QrImageUrl, Is.Null);
            Assert.That(pending.ExpiresAt, Is.EqualTo(expiresAt));
        });
    }

    [Test]
    public void BuildTodotixRequest_WithEmail_FormatsCallbackUrlAndExpiration()
    {
        var debtId = Guid.NewGuid();
        var template = new DebtTemplate { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Description = "Cuota", ClassQuantity = 4, Cost = 200 };
        var expiresAt = new DateTime(2025, 11, 30, 18, 30, 0, DateTimeKind.Utc);
        callbackSignature.Setup(s => s.Sign(debtId.ToString("D"))).Returns("sig123");

        RegisterDebtRequest request = sut.BuildTodotixRequest(debtId, "student@example.com", template, "America/La_Paz", "Pago", expiresAt, "appkey-xyz");

        Assert.Multiple(() =>
        {
            Assert.That(request.Appkey, Is.EqualTo("appkey-xyz"));
            Assert.That(request.IdentificadorDeuda, Is.EqualTo(debtId.ToString()));
            Assert.That(request.EmailCliente, Is.EqualTo("student@example.com"));
            Assert.That(request.Descripcion, Is.EqualTo("Pago"));
            Assert.That(request.LineasDetalleDeuda, Has.Count.EqualTo(1));
        });
        Assert.Multiple(() =>
        {
            Assert.That(request.LineasDetalleDeuda[0].Concepto, Is.EqualTo("Cuota"));
            Assert.That(request.LineasDetalleDeuda[0].CostoUnitario, Is.EqualTo(200));
            Assert.That(request.CallbackUrl, Does.Contain("sig=sig123"));
            Assert.That(request.FechaVencimiento, Is.Not.Null);
        });
        Assert.That(request.FechaVencimiento, Does.Contain("2025"));
    }

    [Test]
    public void BuildTodotixRequest_WithEmptyEmail_ClearsEmail()
    {
        var debtId = Guid.NewGuid();
        var template = new DebtTemplate { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Description = "C", ClassQuantity = 1, Cost = 10 };
        callbackSignature.Setup(s => s.Sign(It.IsAny<string>())).Returns("sig");

        RegisterDebtRequest request = sut.BuildTodotixRequest(debtId, string.Empty, template, "America/La_Paz", "desc", DateTime.UtcNow.AddDays(1), "appkey-xyz");

        Assert.That(request.EmailCliente, Is.Null);
    }

    [Test]
    public void BuildTodotixRequest_WithExistingCallbackQuery_AppendsSignature()
    {
        todotixOptions.CallbackUrl = "https://payment.example.com/api/payment/qr/callback?source=todotix";
        sut = new QrPaymentCreationBuilder(callbackSignature.Object, Options.Create(todotixOptions));
        var debtId = Guid.NewGuid();
        var template = new DebtTemplate { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Description = "C", ClassQuantity = 1, Cost = 10 };
        callbackSignature.Setup(s => s.Sign(It.IsAny<string>())).Returns("sigZ");

        RegisterDebtRequest request = sut.BuildTodotixRequest(debtId, "a@b.com", template, "America/La_Paz", "desc", DateTime.UtcNow.AddDays(1), "appkey-xyz");

        Assert.That(request.CallbackUrl, Does.Contain("source=todotix"));
        Assert.That(request.CallbackUrl, Does.Contain("sig=sigZ"));
    }

    [Test]
    public void BuildOutboxEvent_CarriesPayloadAndKeepsDebtIdAsId()
    {
        var debtId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new RegisterDebtRequest { Appkey = "k", IdentificadorDeuda = debtId.ToString(), Descripcion = "d" };

        TodotixOutboxEvent outboxEvent = sut.BuildOutboxEvent(debtId, tenantId, request);

        Assert.Multiple(() =>
        {
            Assert.That(outboxEvent.Id, Is.EqualTo(debtId));
            Assert.That(outboxEvent.PendingId, Is.EqualTo(debtId));
            Assert.That(outboxEvent.TenantId, Is.EqualTo(tenantId));
            Assert.That(outboxEvent.Status, Is.EqualTo("Pending"));
            Assert.That(outboxEvent.PayloadJson, Does.Contain(debtId.ToString()));
        });
    }

    [Test]
    public void BuildExpirationOutboxEvent_MintsFreshEventIdAndCarriesAvailableAt()
    {
        var debtId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        DateTime availableAt = DateTime.UtcNow.AddMinutes(10);

        ExpirationOutboxEvent outboxEvent = sut.BuildExpirationOutboxEvent(debtId, tenantId, studentId, availableAt);

        Assert.That(outboxEvent.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.Multiple(() =>
        {
            Assert.That(outboxEvent.Id, Is.Not.EqualTo(debtId), "EventId is a fresh Guid distinct from the debt id");
            Assert.That(outboxEvent.AggregateId, Is.EqualTo(debtId));
            Assert.That(outboxEvent.EventType, Is.EqualTo("DebtExpired"));
            Assert.That(outboxEvent.RoutingKey, Is.EqualTo("debt.expired"));
            Assert.That(outboxEvent.AvailableAt, Is.EqualTo(availableAt));
        });

        DebtExpiredEvent? domainEvent = JsonSerializer.Deserialize<DebtExpiredEvent>(outboxEvent.Payload);
        Assert.That(domainEvent, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(domainEvent!.EventId, Is.EqualTo(outboxEvent.Id), "Row Id and payload EventId must be the same Guid");
            Assert.That(domainEvent.Data.PendingId, Is.EqualTo(debtId));
            Assert.That(domainEvent.Data.TenantId, Is.EqualTo(tenantId));
            Assert.That(domainEvent.Data.StudentId, Is.EqualTo(studentId));
        });
    }

    [Test]
    public void BuildPendingDebtDto_StatusIsPending()
    {
        var debtId = Guid.NewGuid();

        QrDebtPendingDto pendingDebt = sut.BuildPendingDebtDto(debtId);

        Assert.Multiple(() =>
        {
            Assert.That(pendingDebt.IdentificadorDeuda, Is.EqualTo(debtId));
            Assert.That(pendingDebt.Status, Is.EqualTo("Pending"));
        });
    }
}
