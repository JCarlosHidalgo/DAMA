CREATE DATABASE IF NOT EXISTS ClassAttendance;

USE ClassAttendance;

-- SCHEDULED CLASS ATTENDANCE
CREATE TABLE IF NOT EXISTS ScheduledClassAttendance (
    TenantId VARCHAR(36) NOT NULL,
    ClassId VARCHAR(36) NOT NULL,
    ClassDate DATE NOT NULL,
    StartTime TIME NOT NULL DEFAULT '00:00:00',
    EndTime   TIME NOT NULL DEFAULT '00:00:00',
    CourseName VARCHAR(80),
    StudentId VARCHAR(36) NOT NULL,
    StudentName VARCHAR(80),
    UNIQUE KEY uq_sched_tenant_class_date_student (TenantId, ClassId, ClassDate, StudentId),
    INDEX idx_sched_tenant_student (TenantId, StudentId)
);

DELIMITER //
CREATE PROCEDURE GetScheduledAttendance(IN tenantId CHAR(36), IN classId CHAR(36), IN classDate DATE)
BEGIN
    SELECT
        *
    FROM
        ScheduledClassAttendance scheduledClassAttendance
    WHERE
        scheduledClassAttendance.TenantId = tenantId
        AND
        scheduledClassAttendance.ClassId = classId
        AND
        scheduledClassAttendance.ClassDate = classDate;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetScheduledAttendanceByStudentId(IN tenantId CHAR(36), IN studentId CHAR(36))
BEGIN
    SELECT
        *
    FROM
        ScheduledClassAttendance scheduledClassAttendance
    WHERE
        scheduledClassAttendance.TenantId = tenantId
        AND
        scheduledClassAttendance.StudentId = studentId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE CountScheduledAttendanceByStudentForTenant(IN tenantId CHAR(36), IN studentId CHAR(36))
BEGIN
    SELECT COUNT(*) AS total
    FROM ScheduledClassAttendance sca
    WHERE sca.TenantId = tenantId AND sca.StudentId = studentId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetScheduledAttendancePageByStudentForTenant(
    IN tenantId  CHAR(36),
    IN studentId CHAR(36),
    IN pageOffset INT,
    IN pageLimit  INT
)
BEGIN
    SELECT sca.*
    FROM ScheduledClassAttendance sca
    WHERE sca.TenantId = tenantId AND sca.StudentId = studentId
    ORDER BY sca.ClassDate DESC, sca.ClassId ASC
    LIMIT pageLimit OFFSET pageOffset;
END //
DELIMITER ;

-- UNIQUE CLASS ATTENDANCE
CREATE TABLE IF NOT EXISTS UniqueClassAttendance (
    TenantId VARCHAR(36) NOT NULL,
    ClassId VARCHAR(36) NOT NULL,
    ClassDate DATE NOT NULL DEFAULT '1970-01-01',
    StartTime TIME NOT NULL DEFAULT '00:00:00',
    EndTime   TIME NOT NULL DEFAULT '00:00:00',
    CourseName VARCHAR(80),
    StudentId VARCHAR(36) NOT NULL,
    StudentName VARCHAR(80),
    UNIQUE KEY uq_uniq_tenant_class_student (TenantId, ClassId, StudentId),
    INDEX idx_uniq_tenant_student (TenantId, StudentId)
);

DELIMITER //
CREATE PROCEDURE GetUniqueAttendance(IN tenantId CHAR(36), IN classId CHAR(36))
BEGIN
    SELECT
        *
    FROM
        UniqueClassAttendance uniqueClassAttendance
    WHERE
        uniqueClassAttendance.TenantId = tenantId
        AND
        uniqueClassAttendance.ClassId = classId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetUniqueAttendanceByStudentId(IN tenantId CHAR(36), IN studentId CHAR(36))
BEGIN
    SELECT
        *
    FROM
        UniqueClassAttendance uniqueClassAttendance
    WHERE
        uniqueClassAttendance.TenantId = tenantId
        AND
        uniqueClassAttendance.StudentId = studentId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE CountUniqueAttendanceByStudentForTenant(IN tenantId CHAR(36), IN studentId CHAR(36))
BEGIN
    SELECT COUNT(*) AS total
    FROM UniqueClassAttendance uca
    WHERE uca.TenantId = tenantId AND uca.StudentId = studentId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetUniqueAttendancePageByStudentForTenant(
    IN tenantId  CHAR(36),
    IN studentId CHAR(36),
    IN pageOffset INT,
    IN pageLimit  INT
)
BEGIN
    SELECT uca.*
    FROM UniqueClassAttendance uca
    WHERE uca.TenantId = tenantId AND uca.StudentId = studentId
    ORDER BY uca.ClassId ASC
    LIMIT pageLimit OFFSET pageOffset;
END //
DELIMITER ;

-- STUDENT REMAIN CLASSES
CREATE TABLE IF NOT EXISTS StudentRemainClasses (
    TenantId VARCHAR(36) NOT NULL,
    Id VARCHAR(36) NOT NULL,
    NumberOfClasses INT NOT NULL DEFAULT 0,
    StudentName VARCHAR(80),
    PRIMARY KEY (TenantId, Id)
);

-- PROCESSED EVENTS (consumer-side idempotency ledger)
CREATE TABLE IF NOT EXISTS processed_events (
    EventId     CHAR(36)    NOT NULL PRIMARY KEY,
    ProcessedAt DATETIME(6) NOT NULL,
    INDEX idx_processed_at (ProcessedAt)
);

-- PAYMENT CREDIT LEDGER (durable traceability of credited payments)
CREATE TABLE IF NOT EXISTS payment_credit_ledger (
    EventId           CHAR(36)     NOT NULL PRIMARY KEY,
    TenantId          CHAR(36)     NOT NULL,
    StudentId         CHAR(36)     NOT NULL,
    Quantity          INT          NOT NULL,
    ExternalReference VARCHAR(100) NOT NULL,
    OccurredAt        DATETIME(6)  NOT NULL,
    CreatedAt         DATETIME(6)  NOT NULL,
    INDEX idx_payment_credit_student (TenantId, StudentId),
    INDEX idx_payment_credit_reference (ExternalReference)
);

-- PROCESSED REMAIN REQUESTS (client-request idempotency for manual remain increments)
CREATE TABLE IF NOT EXISTS processed_remain_requests (
    RequestId   CHAR(36)    NOT NULL PRIMARY KEY,
    ProcessedAt DATETIME(6) NOT NULL,
    INDEX idx_processed_remain_at (ProcessedAt)
);

-- TRUNCATE ALL TABLES
DELIMITER //
CREATE PROCEDURE TruncateAllTables()
BEGIN
    SET FOREIGN_KEY_CHECKS = 0;
    TRUNCATE TABLE ScheduledClassAttendance;
    TRUNCATE TABLE UniqueClassAttendance;
    TRUNCATE TABLE StudentRemainClasses;
    TRUNCATE TABLE processed_events;
    TRUNCATE TABLE payment_credit_ledger;
    TRUNCATE TABLE processed_remain_requests;
    SET FOREIGN_KEY_CHECKS = 1;
END //
DELIMITER ;
