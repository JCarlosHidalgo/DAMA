CREATE DATABASE IF NOT EXISTS Auth;

USE Auth;

-- USER
CREATE TABLE IF NOT EXISTS User(
    Id              VARCHAR(36) PRIMARY KEY NOT NULL,
    UserName        VARCHAR(80) NOT NULL,
    PasswordHash    VARCHAR(200),
    Role            VARCHAR(50),
    IsDeleted       TINYINT(1) NOT NULL DEFAULT 0,
    ActiveUserName  VARCHAR(80) GENERATED ALWAYS AS (CASE WHEN IsDeleted = 0 THEN UserName ELSE NULL END) STORED,
    UNIQUE KEY uk_user_active_username (ActiveUserName)
);

-- TENANT
CREATE TABLE IF NOT EXISTS Tenant(
    Id       VARCHAR(36) PRIMARY KEY NOT NULL,
    Name     VARCHAR(200),
    Timezone VARCHAR(64) NOT NULL DEFAULT 'America/La_Paz'
);

-- TENANT DOMAIN
CREATE TABLE IF NOT EXISTS TenantDomain(
    UserId VARCHAR(36),
    TenantId VARCHAR(36),
    FOREIGN KEY (UserId) REFERENCES User(Id),
    FOREIGN KEY (TenantId) REFERENCES Tenant(Id)
);

DELIMITER //
CREATE PROCEDURE GetUserWithTenantByUserName(IN userName CHAR(80))
BEGIN
    SELECT
        u.Id,
        u.UserName,
        u.PasswordHash,
        u.Role,
        t.Id       AS TenantId,
        t.Name,
        t.Timezone
    FROM
        User u
        INNER JOIN TenantDomain td ON td.UserId = u.Id
        INNER JOIN Tenant t ON t.Id = td.TenantId
    WHERE
        u.UserName = userName
        AND u.IsDeleted = 0;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetUsersByRoleForTenantPaged(
    IN tenantId VARCHAR(36),
    IN userRole VARCHAR(50),
    IN pageOffset INT,
    IN pageSize INT
)
BEGIN
    SELECT
        u.Id,
        u.UserName
    FROM
        User u
        INNER JOIN TenantDomain td ON td.UserId = u.Id
    WHERE
        td.TenantId = tenantId
        AND u.Role = userRole
        AND u.IsDeleted = 0
    ORDER BY u.UserName ASC, u.Id ASC
    LIMIT pageSize OFFSET pageOffset;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE CountUsersByRoleForTenant(
    IN tenantId VARCHAR(36),
    IN userRole VARCHAR(50)
)
BEGIN
    SELECT COUNT(*) AS Total
    FROM
        User u
        INNER JOIN TenantDomain td ON td.UserId = u.Id
    WHERE
        td.TenantId = tenantId
        AND u.Role = userRole
        AND u.IsDeleted = 0;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetUserByIdForTenant(
    IN userId   VARCHAR(36),
    IN tenantId VARCHAR(36)
)
BEGIN
    SELECT
        u.Id,
        u.UserName,
        u.PasswordHash,
        u.Role,
        u.IsDeleted
    FROM
        User u
        INNER JOIN TenantDomain td ON td.UserId = u.Id
    WHERE
        u.Id = userId
        AND td.TenantId = tenantId
        AND u.IsDeleted = 0;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetStudentByExactNameForTenant(
    IN tenantId VARCHAR(36),
    IN userName VARCHAR(80)
)
BEGIN
    SELECT
        u.Id,
        u.UserName
    FROM
        User u
        INNER JOIN TenantDomain td ON td.UserId = u.Id
    WHERE
        td.TenantId = tenantId
        AND u.Role = 'Student'
        AND u.UserName = userName
        AND u.IsDeleted = 0
    LIMIT 1;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE SoftDeleteUserForTenant(
    IN userId   VARCHAR(36),
    IN tenantId VARCHAR(36)
)
BEGIN
    UPDATE User u
        INNER JOIN TenantDomain td ON td.UserId = u.Id
    SET u.IsDeleted = 1
    WHERE
        u.Id = userId
        AND td.TenantId = tenantId
        AND u.IsDeleted = 0;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE UpdateTenantTimezone(
    IN tenantId    VARCHAR(36),
    IN newTimezone VARCHAR(64)
)
BEGIN
    UPDATE Tenant
    SET Timezone = newTimezone
    WHERE Id = tenantId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetAllTenants()
BEGIN
    SELECT
        Id,
        Name,
        Timezone
    FROM Tenant
    ORDER BY Name ASC, Id ASC;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE CreateTenant(
    IN tenantId       VARCHAR(36),
    IN tenantName     VARCHAR(200),
    IN tenantTimezone VARCHAR(64)
)
BEGIN
    INSERT INTO Tenant (Id, Name, Timezone)
    VALUES (tenantId, tenantName, tenantTimezone);
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE UpdateTenantName(
    IN tenantId VARCHAR(36),
    IN newName  VARCHAR(200)
)
BEGIN
    UPDATE Tenant
    SET Name = newName
    WHERE Id = tenantId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE UpdateUserNameForTenant(
    IN userId      VARCHAR(36),
    IN tenantId    VARCHAR(36),
    IN newUserName VARCHAR(80)
)
BEGIN
    UPDATE User u
        INNER JOIN TenantDomain td ON td.UserId = u.Id
    SET u.UserName = newUserName
    WHERE
        u.Id = userId
        AND td.TenantId = tenantId
        AND u.IsDeleted = 0;
END //
DELIMITER ;


-- OUTBOX EVENTS
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
    INDEX idx_unpublished (PublishedAt, LeasedUntil, OccurredAt)
);

-- TRUNCATE ALL TABLES
DELIMITER //
CREATE PROCEDURE TruncateAllTables()
BEGIN
    SET FOREIGN_KEY_CHECKS = 0;
    TRUNCATE TABLE User;
    TRUNCATE TABLE Tenant;
    TRUNCATE TABLE TenantDomain;
    TRUNCATE TABLE outbox_events;
    SET FOREIGN_KEY_CHECKS = 1;
END //
DELIMITER ;