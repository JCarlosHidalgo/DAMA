CREATE DATABASE IF NOT EXISTS Payment;

USE Payment;

-- DEBT TEMPLATE
CREATE TABLE IF NOT EXISTS DebtTemplate (
    Id            VARCHAR(36)  PRIMARY KEY NOT NULL,
    TenantId      VARCHAR(36)  NOT NULL,
    Description   VARCHAR(256) NOT NULL,
    ClassQuantity INT          NOT NULL,
    Cost          INT          NOT NULL,
    INDEX idx_DebtTemplate_TenantId (TenantId)
);

DELIMITER //
CREATE PROCEDURE GetDebtTemplatesByTenant(IN tenantId CHAR(36))
BEGIN
    SELECT
        dt.Id,
        dt.TenantId,
        dt.Description,
        dt.ClassQuantity,
        dt.Cost
    FROM DebtTemplate dt
    WHERE dt.TenantId = tenantId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetDebtTemplateByIdForTenant(IN tenantId CHAR(36), IN templateId CHAR(36))
BEGIN
    SELECT
        dt.Id,
        dt.TenantId,
        dt.Description,
        dt.ClassQuantity,
        dt.Cost
    FROM DebtTemplate dt
    WHERE dt.Id = templateId
      AND dt.TenantId = tenantId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE UpdateDebtTemplateForTenant(
    IN tenantId       CHAR(36),
    IN templateId     CHAR(36),
    IN description    VARCHAR(256),
    IN classQuantity  INT,
    IN cost           INT
)
BEGIN
    UPDATE DebtTemplate dt
    SET dt.Description   = description,
        dt.ClassQuantity = classQuantity,
        dt.Cost          = cost
    WHERE dt.Id = templateId
      AND dt.TenantId = tenantId;
    SELECT ROW_COUNT() AS Updated;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE DeleteDebtTemplateForTenant(IN tenantId CHAR(36), IN templateId CHAR(36))
BEGIN
    DELETE dt FROM DebtTemplate dt
    WHERE dt.Id = templateId
      AND dt.TenantId = tenantId;
    SELECT ROW_COUNT() AS Deleted;
END //
DELIMITER ;

-- TENANT PAYMENT CREDENTIALS
CREATE TABLE IF NOT EXISTS TenantPaymentCredentials (
    TenantId      VARCHAR(36)  PRIMARY KEY NOT NULL,
    TodotixAppKey VARCHAR(512) NOT NULL
);

DELIMITER //
CREATE PROCEDURE GetPaymentCredentialsByTenant(IN tenantId CHAR(36))
BEGIN
    SELECT
        tpc.TenantId,
        tpc.TodotixAppKey
    FROM TenantPaymentCredentials tpc
    WHERE tpc.TenantId = tenantId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE UpsertPaymentCredentialsForTenant(
    IN tenantId      CHAR(36),
    IN todotixAppKey VARCHAR(512)
)
BEGIN
    INSERT INTO TenantPaymentCredentials (TenantId, TodotixAppKey)
    VALUES (tenantId, todotixAppKey)
    ON DUPLICATE KEY UPDATE
        TodotixAppKey = todotixAppKey;
    SELECT ROW_COUNT() AS Affected;
END //
DELIMITER ;

-- PENDING QR PAYMENT
CREATE TABLE IF NOT EXISTS PendingQrPayment (
    Id            VARCHAR(36)  PRIMARY KEY NOT NULL,
    TenantId      VARCHAR(36)  NOT NULL,
    StudentId     VARCHAR(36)  NOT NULL,
    TemplateId    VARCHAR(36)  NOT NULL,
    ClassQuantity INT          NOT NULL,
    Cost          INT          NOT NULL,
    QrImageUrl    VARCHAR(512) NULL,
    CreatedAt     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ExpiresAt     DATETIME(6)  NOT NULL,
    INDEX idx_PendingQrPayment_TenantStudent (TenantId, StudentId),
    INDEX idx_PendingQrPayment_TenantStudentTemplate (TenantId, StudentId, TemplateId)
);

DELIMITER //
CREATE PROCEDURE GetPendingQrPaymentsByStudentForTenant(IN tenantId CHAR(36), IN studentId CHAR(36))
BEGIN
    SELECT
        pq.Id,
        pq.TenantId,
        pq.StudentId,
        pq.TemplateId,
        pq.ClassQuantity,
        pq.Cost,
        pq.QrImageUrl,
        pq.CreatedAt,
        pq.ExpiresAt
    FROM PendingQrPayment pq
    WHERE pq.TenantId = tenantId
      AND pq.StudentId = studentId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetPendingQrPaymentsByStudentTemplateForTenant(IN tenantId CHAR(36), IN studentId CHAR(36), IN templateId CHAR(36))
BEGIN
    SELECT
        pq.Id,
        pq.TenantId,
        pq.StudentId,
        pq.TemplateId,
        pq.ClassQuantity,
        pq.Cost,
        pq.QrImageUrl,
        pq.CreatedAt,
        pq.ExpiresAt
    FROM PendingQrPayment pq
    WHERE pq.TenantId = tenantId
      AND pq.StudentId = studentId
      AND pq.TemplateId = templateId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetPendingQrPaymentByIdForTenant(IN tenantId CHAR(36), IN paymentId CHAR(36))
BEGIN
    SELECT
        pq.Id,
        pq.TenantId,
        pq.StudentId,
        pq.TemplateId,
        pq.ClassQuantity,
        pq.Cost,
        pq.QrImageUrl,
        pq.CreatedAt,
        pq.ExpiresAt
    FROM PendingQrPayment pq
    WHERE pq.Id = paymentId
      AND pq.TenantId = tenantId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE DeletePendingQrPaymentForTenant(IN tenantId CHAR(36), IN paymentId CHAR(36))
BEGIN
    DELETE pqp FROM PendingQrPayment pqp
    WHERE pqp.Id = paymentId
      AND pqp.TenantId = tenantId;
    SELECT ROW_COUNT() AS Deleted;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetPendingQrPaymentsPageByStudentForTenant(
    IN tenantId   CHAR(36),
    IN studentId  CHAR(36),
    IN pageOffset INT,
    IN pageLimit  INT
)
BEGIN
    SELECT
        pq.Id,
        pq.TenantId,
        pq.StudentId,
        pq.TemplateId,
        pq.ClassQuantity,
        pq.Cost,
        pq.QrImageUrl,
        pq.CreatedAt,
        pq.ExpiresAt
    FROM PendingQrPayment pq
    WHERE pq.TenantId  = tenantId
      AND pq.StudentId = studentId
    ORDER BY pq.CreatedAt DESC, pq.Id DESC
    LIMIT pageLimit OFFSET pageOffset;
END //
DELIMITER ;

-- SUCCESS QR PAYMENT
CREATE TABLE IF NOT EXISTS SuccessQrPayment (
    Id            VARCHAR(36) PRIMARY KEY NOT NULL,
    TenantId      VARCHAR(36) NOT NULL,
    StudentId     VARCHAR(36) NOT NULL,
    ClassQuantity INT         NOT NULL,
    Cost          INT         NOT NULL,
    PaidAt        DATETIME    NOT NULL,
    INDEX idx_SuccessQrPayment_TenantStudent (TenantId, StudentId),
    INDEX idx_SuccessQrPayment_TenantPaidAt (TenantId, PaidAt)
);

DELIMITER //
CREATE PROCEDURE GetPaymentSummaryForTenant(
    IN tenantId CHAR(36),
    IN fromDate DATETIME
)
BEGIN
    SELECT
        COALESCE(SUM(sqp.Cost), 0) AS TotalEarnings,
        COALESCE(SUM(CASE WHEN sqp.PaidAt >= fromDate THEN sqp.Cost ELSE 0 END), 0) AS MonthEarnings,
        MIN(sqp.PaidAt) AS FirstPaymentDate
    FROM SuccessQrPayment sqp
    WHERE sqp.TenantId = tenantId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetSuccessQrPaymentsPageByStudentForTenant(
    IN tenantId   CHAR(36),
    IN studentId  CHAR(36),
    IN pageOffset INT,
    IN pageLimit  INT
)
BEGIN
    SELECT
        sq.Id,
        sq.TenantId,
        sq.StudentId,
        sq.ClassQuantity,
        sq.Cost,
        sq.PaidAt
    FROM SuccessQrPayment sq
    WHERE sq.TenantId  = tenantId
      AND sq.StudentId = studentId
    ORDER BY sq.PaidAt DESC, sq.Id DESC
    LIMIT pageLimit OFFSET pageOffset;
END //
DELIMITER ;

-- FAILED QR PAYMENT
CREATE TABLE IF NOT EXISTS FailedQrPayment (
    Id            VARCHAR(36) PRIMARY KEY NOT NULL,
    TenantId      VARCHAR(36) NOT NULL,
    StudentId     VARCHAR(36) NOT NULL,
    ClassQuantity INT         NOT NULL,
    Cost          INT         NOT NULL,
    FailedAt      DATETIME    NOT NULL,
    INDEX idx_FailedQrPayment_TenantStudent (TenantId, StudentId)
);

DELIMITER //
CREATE PROCEDURE GetFailedQrPaymentsPageByStudentForTenant(
    IN tenantId   CHAR(36),
    IN studentId  CHAR(36),
    IN pageOffset INT,
    IN pageLimit  INT
)
BEGIN
    SELECT
        fq.Id,
        fq.TenantId,
        fq.StudentId,
        fq.ClassQuantity,
        fq.Cost,
        fq.FailedAt
    FROM FailedQrPayment fq
    WHERE fq.TenantId  = tenantId
      AND fq.StudentId = studentId
    ORDER BY fq.FailedAt DESC, fq.Id DESC
    LIMIT pageLimit OFFSET pageOffset;
END //
DELIMITER ;

-- QR PAYMENT IDEMPOTENCY (ledger of processed external references for QR debt creation)
CREATE TABLE IF NOT EXISTS QrPaymentIdempotency (
    TenantId          VARCHAR(36)  NOT NULL,
    ExternalReference VARCHAR(128) NOT NULL,
    EntityId          VARCHAR(36)  NOT NULL,
    ProcessedAt       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (TenantId, ExternalReference)
);

-- TODOTIX OUTBOX (drained by TodotixOutboxWorker to register debts with Todotix off the request path)
CREATE TABLE IF NOT EXISTS todotix_outbox (
    Id           CHAR(36)     NOT NULL PRIMARY KEY,
    PendingId    CHAR(36)     NOT NULL,
    TenantId     CHAR(36)     NOT NULL,
    PayloadJson  JSON         NOT NULL,
    OccurredAt   DATETIME(6)  NOT NULL,
    ProcessedAt  DATETIME(6)  NULL,
    LeasedUntil  DATETIME(6)  NULL,
    Attempts     INT          NOT NULL DEFAULT 0,
    LastError    VARCHAR(500) NULL,
    Status       VARCHAR(16)  NOT NULL DEFAULT 'Pending',
    INDEX idx_todotix_outbox_unprocessed (ProcessedAt, LeasedUntil, OccurredAt),
    INDEX idx_todotix_outbox_pending (PendingId)
);

-- EXPIRATION OUTBOX (drained by ExpirationOutboxPublisher to schedule debt-expired events on dama.delayed)
CREATE TABLE IF NOT EXISTS expiration_outbox (
    Id          CHAR(36)     NOT NULL PRIMARY KEY,
    AggregateId CHAR(36)     NOT NULL,
    EventType   VARCHAR(128) NOT NULL,
    RoutingKey  VARCHAR(128) NOT NULL,
    Payload     JSON         NOT NULL,
    OccurredAt  DATETIME(6)  NOT NULL,
    AvailableAt DATETIME(6)  NOT NULL,
    PublishedAt DATETIME(6)  NULL,
    LeasedUntil DATETIME(6)  NULL,
    Attempts    INT          NOT NULL DEFAULT 0,
    LastError   VARCHAR(500) NULL,
    INDEX idx_expiration_outbox_unpublished (PublishedAt, LeasedUntil, OccurredAt)
);

-- PROCESSED EVENTS (consumer-side idempotency ledger for delayed debt-expired events)
CREATE TABLE IF NOT EXISTS processed_events (
    EventId     CHAR(36)    NOT NULL PRIMARY KEY,
    ProcessedAt DATETIME(6) NOT NULL,
    INDEX idx_processed_at (ProcessedAt)
);

-- OUTBOX EVENTS (drained by DomainEventOutboxPublisher to publish domain events on dama.events)
CREATE TABLE IF NOT EXISTS outbox_events (
    Id            CHAR(36)     NOT NULL PRIMARY KEY,
    AggregateType VARCHAR(64)  NOT NULL,
    AggregateId   CHAR(36)     NOT NULL,
    EventType     VARCHAR(128) NOT NULL,
    RoutingKey    VARCHAR(128) NOT NULL,
    Payload       JSON         NOT NULL,
    OccurredAt    DATETIME(6)  NOT NULL,
    PublishedAt   DATETIME(6)  NULL,
    LeasedUntil   DATETIME(6)  NULL,
    Attempts      INT          NOT NULL DEFAULT 0,
    LastError     VARCHAR(500) NULL,
    INDEX idx_outbox_events_unpublished (PublishedAt, LeasedUntil, OccurredAt)
);

-- CALLBACK INBOX (Todotix payment callbacks; drained by PaymentCallbackWorker which consults Todotix and transitions the debt)
CREATE TABLE IF NOT EXISTS payment_callback_inbox (
    Id          CHAR(36)     NOT NULL PRIMARY KEY,
    Error       INT          NOT NULL,
    CancelOrder INT          NOT NULL,
    OccurredAt  DATETIME(6)  NOT NULL,
    ProcessedAt DATETIME(6)  NULL,
    LeasedUntil DATETIME(6)  NULL,
    Attempts    INT          NOT NULL DEFAULT 0,
    LastError   VARCHAR(500) NULL,
    Status      VARCHAR(16)  NOT NULL DEFAULT 'Pending',
    INDEX idx_payment_callback_inbox_unprocessed (ProcessedAt, LeasedUntil, OccurredAt)
);

-- TRUNCATE ALL TABLES
DELIMITER //
CREATE PROCEDURE TruncateAllTables()
BEGIN
    SET FOREIGN_KEY_CHECKS = 0;
    TRUNCATE TABLE DebtTemplate;
    TRUNCATE TABLE TenantPaymentCredentials;
    TRUNCATE TABLE PendingQrPayment;
    TRUNCATE TABLE SuccessQrPayment;
    TRUNCATE TABLE FailedQrPayment;
    TRUNCATE TABLE QrPaymentIdempotency;
    TRUNCATE TABLE todotix_outbox;
    TRUNCATE TABLE expiration_outbox;
    TRUNCATE TABLE processed_events;
    TRUNCATE TABLE outbox_events;
    TRUNCATE TABLE payment_callback_inbox;
    SET FOREIGN_KEY_CHECKS = 1;
END //
DELIMITER ;
