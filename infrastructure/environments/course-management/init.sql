CREATE DATABASE IF NOT EXISTS CourseManagement;

USE CourseManagement;

-- COURSE
CREATE TABLE IF NOT EXISTS Course(
    Id          VARCHAR(36) PRIMARY KEY NOT NULL,
    Name        VARCHAR(200),
    TenantId    VARCHAR(36)
);

DELIMITER //
CREATE PROCEDURE GetCoursesByTenantId(IN tenantId CHAR(36))
BEGIN
    SELECT
        course.Id,
        course.Name,
        course.TenantId
    FROM
        Course course
    WHERE
        course.TenantId = tenantId;
END //
DELIMITER ;

-- CLASS GROUP (tenant-scoped grouping of classes; overlap validation is per group)
CREATE TABLE IF NOT EXISTS ClassGroup(
    Id       VARCHAR(36) PRIMARY KEY NOT NULL,
    Name     VARCHAR(200) NOT NULL,
    TenantId VARCHAR(36)  NOT NULL,
    INDEX idx_classgroup_tenant (TenantId)
);

-- SCHEDULED CLASS
CREATE TABLE IF NOT EXISTS ScheduledClass(
    Id VARCHAR(36) PRIMARY KEY NOT NULL,
    DayOfWeekIndex TINYINT,
    MaxStudentLimit SMALLINT NOT NULL DEFAULT 0,
    StartTime TIME,
    EndTime TIME,
    CourseId VARCHAR(36),
    GroupId VARCHAR(36) NOT NULL,
    TenantId VARCHAR(36) NOT NULL,
    FOREIGN KEY (CourseId) REFERENCES Course(Id),
    FOREIGN KEY (GroupId) REFERENCES ClassGroup(Id),
    INDEX idx_scheduledclass_tenant (TenantId),
    INDEX idx_scheduledclass_group (GroupId)
);

-- UNIQUE CLASS
CREATE TABLE IF NOT EXISTS UniqueClass (
    Id VARCHAR(36) PRIMARY KEY NOT NULL,
    Date DATE,
    MaxStudentLimit SMALLINT NOT NULL DEFAULT 0,
    StartTime TIME,
    EndTime TIME,
    CourseId VARCHAR(36),
    GroupId VARCHAR(36) NOT NULL,
    TenantId VARCHAR(36) NOT NULL,
    FOREIGN KEY (CourseId) REFERENCES Course(Id),
    FOREIGN KEY (GroupId) REFERENCES ClassGroup(Id),
    INDEX idx_uniqueclass_tenant (TenantId),
    INDEX idx_uniqueclass_group (GroupId)
);

-- M:N JOIN TABLES (teacher membership with denormalized name snapshot)
CREATE TABLE IF NOT EXISTS ScheduledClassTeacher (
    ScheduledClassId VARCHAR(36)  NOT NULL,
    TeacherId        VARCHAR(36)  NOT NULL,
    TeacherName      VARCHAR(200) NOT NULL,
    TenantId         VARCHAR(36)  NOT NULL,
    PRIMARY KEY (ScheduledClassId, TeacherId),
    FOREIGN KEY (ScheduledClassId) REFERENCES ScheduledClass(Id) ON DELETE CASCADE,
    INDEX idx_sct_teacher_tenant (TeacherId, TenantId)
);

CREATE TABLE IF NOT EXISTS UniqueClassTeacher (
    UniqueClassId VARCHAR(36)  NOT NULL,
    TeacherId     VARCHAR(36)  NOT NULL,
    TeacherName   VARCHAR(200) NOT NULL,
    TenantId      VARCHAR(36)  NOT NULL,
    PRIMARY KEY (UniqueClassId, TeacherId),
    FOREIGN KEY (UniqueClassId) REFERENCES UniqueClass(Id) ON DELETE CASCADE,
    INDEX idx_uct_teacher_tenant (TeacherId, TenantId)
);

DELIMITER //
CREATE PROCEDURE GetScheduledClassesByCourseId(IN courseId CHAR(36))
BEGIN
    SELECT
        sc.Id,
        sc.DayOfWeekIndex,
        sc.MaxStudentLimit,
        sc.StartTime,
        sc.EndTime,
        sc.CourseId,
        sc.GroupId,
        cg.Name AS GroupName,
        sc.TenantId,
        CONCAT('[', IFNULL(GROUP_CONCAT(
            JSON_OBJECT('TeacherId', t.TeacherId, 'TeacherName', t.TeacherName)
        ), ''), ']') AS Teachers
    FROM ScheduledClass sc
    LEFT JOIN ClassGroup cg ON cg.Id = sc.GroupId
    LEFT JOIN ScheduledClassTeacher t ON t.ScheduledClassId = sc.Id
    WHERE sc.CourseId = courseId
    GROUP BY sc.Id;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetUniqueClassesOnSameWeekByDate(IN courseId CHAR(36),IN dateInput DATE)
BEGIN
    SELECT
        uc.Id,
        uc.Date,
        uc.MaxStudentLimit,
        uc.StartTime,
        uc.EndTime,
        uc.CourseId,
        uc.GroupId,
        cg.Name AS GroupName,
        uc.TenantId,
        CONCAT('[', IFNULL(GROUP_CONCAT(
            JSON_OBJECT('TeacherId', t.TeacherId, 'TeacherName', t.TeacherName)
        ), ''), ']') AS Teachers
    FROM UniqueClass uc
    LEFT JOIN ClassGroup cg ON cg.Id = uc.GroupId
    LEFT JOIN UniqueClassTeacher t ON t.UniqueClassId = uc.Id
    WHERE uc.CourseId = courseId
      AND YEAR(uc.Date) = YEAR(dateInput)
      AND WEEK(uc.Date) = WEEK(dateInput)
    GROUP BY uc.Id;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE ExistsScheduledClassForTenant(IN tenantId CHAR(36), IN classId CHAR(36), IN classDate DATE)
BEGIN
    SELECT sc.Id, sc.StartTime, sc.EndTime, sc.MaxStudentLimit
    FROM ScheduledClass sc
    WHERE sc.Id = classId
      AND sc.TenantId = tenantId
      AND sc.DayOfWeekIndex = (WEEKDAY(classDate) + 1)
    LIMIT 1;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE ExistsUniqueClassForTenant(IN tenantId CHAR(36), IN classId CHAR(36))
BEGIN
    SELECT uc.Id, uc.StartTime, uc.EndTime, uc.Date AS ClassDate, uc.MaxStudentLimit
    FROM UniqueClass uc
    WHERE uc.Id = classId
      AND uc.TenantId = tenantId
    LIMIT 1;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE CreateScheduledClass(
    IN id              CHAR(36),
    IN dayOfWeekIndex  TINYINT,
    IN maxStudentLimit SMALLINT,
    IN startTime       TIME,
    IN endTime         TIME,
    IN courseId        CHAR(36),
    IN groupId         CHAR(36),
    IN tenantId        CHAR(36)
)
BEGIN
    INSERT INTO ScheduledClass (Id, DayOfWeekIndex, MaxStudentLimit, StartTime, EndTime, CourseId, GroupId, TenantId)
    SELECT id, dayOfWeekIndex, maxStudentLimit, startTime, endTime, courseId, groupId, tenantId
    FROM Course c
    WHERE c.Id = courseId
      AND c.TenantId = tenantId
      AND EXISTS (SELECT 1 FROM ClassGroup g WHERE g.Id = groupId AND g.TenantId = tenantId);
    SELECT ROW_COUNT() AS Inserted;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE CreateUniqueClass(
    IN id              CHAR(36),
    IN classDate       DATE,
    IN maxStudentLimit SMALLINT,
    IN startTime       TIME,
    IN endTime         TIME,
    IN courseId        CHAR(36),
    IN groupId         CHAR(36),
    IN tenantId        CHAR(36)
)
BEGIN
    INSERT INTO UniqueClass (Id, Date, MaxStudentLimit, StartTime, EndTime, CourseId, GroupId, TenantId)
    SELECT id, classDate, maxStudentLimit, startTime, endTime, courseId, groupId, tenantId
    FROM Course c
    WHERE c.Id = courseId
      AND c.TenantId = tenantId
      AND EXISTS (SELECT 1 FROM ClassGroup g WHERE g.Id = groupId AND g.TenantId = tenantId);
    SELECT ROW_COUNT() AS Inserted;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE InsertScheduledClassTeacher(
    IN scheduledClassId CHAR(36),
    IN teacherId        CHAR(36),
    IN teacherName      VARCHAR(200),
    IN tenantId         CHAR(36)
)
BEGIN
    INSERT INTO ScheduledClassTeacher (ScheduledClassId, TeacherId, TeacherName, TenantId)
    VALUES (scheduledClassId, teacherId, teacherName, tenantId);
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE InsertUniqueClassTeacher(
    IN uniqueClassId CHAR(36),
    IN teacherId     CHAR(36),
    IN teacherName   VARCHAR(200),
    IN tenantId      CHAR(36)
)
BEGIN
    INSERT INTO UniqueClassTeacher (UniqueClassId, TeacherId, TeacherName, TenantId)
    VALUES (uniqueClassId, teacherId, teacherName, tenantId);
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE DeleteScheduledClassTeachers(IN scheduledClassId CHAR(36))
BEGIN
    DELETE sct FROM ScheduledClassTeacher sct WHERE sct.ScheduledClassId = scheduledClassId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE DeleteUniqueClassTeachers(IN uniqueClassId CHAR(36))
BEGIN
    DELETE uct FROM UniqueClassTeacher uct WHERE uct.UniqueClassId = uniqueClassId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetCourseByIdForTenant(IN tenantId CHAR(36), IN courseId CHAR(36))
BEGIN
    SELECT
        c.Id,
        c.Name,
        c.TenantId
    FROM Course c
    WHERE c.Id = courseId
      AND c.TenantId = tenantId;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE ExistsCourseForTenant(IN tenantId CHAR(36), IN courseId CHAR(36))
BEGIN
    SELECT 1
    FROM Course c
    WHERE c.Id = courseId
      AND c.TenantId = tenantId
    LIMIT 1;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE UpdateCourseForTenant(
    IN courseId  CHAR(36),
    IN newName   VARCHAR(200),
    IN tenantId  CHAR(36)
)
BEGIN
    UPDATE Course c
    SET c.Name = newName
    WHERE c.Id = courseId
      AND c.TenantId = tenantId;
    SELECT ROW_COUNT() AS Updated;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE UpdateScheduledClassForTenant(
    IN id              CHAR(36),
    IN dayOfWeekIndex  TINYINT,
    IN maxStudentLimit SMALLINT,
    IN startTime       TIME,
    IN endTime         TIME,
    IN tenantId        CHAR(36)
)
BEGIN
    UPDATE ScheduledClass sc
    SET sc.DayOfWeekIndex  = dayOfWeekIndex,
        sc.MaxStudentLimit = maxStudentLimit,
        sc.StartTime       = startTime,
        sc.EndTime         = endTime
    WHERE sc.Id = id
      AND sc.TenantId = tenantId;
    SELECT ROW_COUNT() AS Updated;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE UpdateUniqueClassForTenant(
    IN id              CHAR(36),
    IN classDate       DATE,
    IN maxStudentLimit SMALLINT,
    IN startTime       TIME,
    IN endTime         TIME,
    IN tenantId        CHAR(36)
)
BEGIN
    UPDATE UniqueClass uc
    SET uc.Date            = classDate,
        uc.MaxStudentLimit = maxStudentLimit,
        uc.StartTime       = startTime,
        uc.EndTime         = endTime
    WHERE uc.Id = id
      AND uc.TenantId = tenantId;
    SELECT ROW_COUNT() AS Updated;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetScheduledClassesByTeacherForTenant(IN tenantId CHAR(36), IN teacherId CHAR(36))
BEGIN
    SELECT
        sc.Id,
        sc.DayOfWeekIndex,
        sc.MaxStudentLimit,
        sc.StartTime,
        sc.EndTime,
        sc.CourseId,
        sc.GroupId,
        cg.Name AS GroupName,
        sc.TenantId,
        CONCAT('[', IFNULL(GROUP_CONCAT(
            JSON_OBJECT('TeacherId', t2.TeacherId, 'TeacherName', t2.TeacherName)
        ), ''), ']') AS Teachers
    FROM ScheduledClass sc
    INNER JOIN ScheduledClassTeacher tm
        ON tm.ScheduledClassId = sc.Id
       AND tm.TeacherId = teacherId
       AND tm.TenantId  = tenantId
    LEFT JOIN ClassGroup cg ON cg.Id = sc.GroupId
    LEFT JOIN ScheduledClassTeacher t2 ON t2.ScheduledClassId = sc.Id
    WHERE sc.TenantId = tenantId
    GROUP BY sc.Id;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetUniqueClassesByTeacherForTenant(IN tenantId CHAR(36), IN teacherId CHAR(36))
BEGIN
    SELECT
        uc.Id,
        uc.Date,
        uc.MaxStudentLimit,
        uc.StartTime,
        uc.EndTime,
        uc.CourseId,
        uc.GroupId,
        cg.Name AS GroupName,
        uc.TenantId,
        CONCAT('[', IFNULL(GROUP_CONCAT(
            JSON_OBJECT('TeacherId', t2.TeacherId, 'TeacherName', t2.TeacherName)
        ), ''), ']') AS Teachers
    FROM UniqueClass uc
    INNER JOIN UniqueClassTeacher tm
        ON tm.UniqueClassId = uc.Id
       AND tm.TeacherId = teacherId
       AND tm.TenantId  = tenantId
    LEFT JOIN ClassGroup cg ON cg.Id = uc.GroupId
    LEFT JOIN UniqueClassTeacher t2 ON t2.UniqueClassId = uc.Id
    WHERE uc.TenantId = tenantId
    GROUP BY uc.Id;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetScheduledClassByIdForTenant(IN tenantId CHAR(36), IN id CHAR(36))
BEGIN
    SELECT
        sc.Id,
        sc.DayOfWeekIndex,
        sc.MaxStudentLimit,
        sc.StartTime,
        sc.EndTime,
        sc.CourseId,
        sc.GroupId,
        cg.Name AS GroupName,
        sc.TenantId,
        CONCAT('[', IFNULL(GROUP_CONCAT(
            JSON_OBJECT('TeacherId', t.TeacherId, 'TeacherName', t.TeacherName)
        ), ''), ']') AS Teachers
    FROM ScheduledClass sc
    LEFT JOIN ClassGroup cg ON cg.Id = sc.GroupId
    LEFT JOIN ScheduledClassTeacher t ON t.ScheduledClassId = sc.Id
    WHERE sc.Id = id
      AND sc.TenantId = tenantId
    GROUP BY sc.Id;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetUniqueClassByIdForTenant(IN tenantId CHAR(36), IN id CHAR(36))
BEGIN
    SELECT
        uc.Id,
        uc.Date,
        uc.MaxStudentLimit,
        uc.StartTime,
        uc.EndTime,
        uc.CourseId,
        uc.GroupId,
        cg.Name AS GroupName,
        uc.TenantId,
        CONCAT('[', IFNULL(GROUP_CONCAT(
            JSON_OBJECT('TeacherId', t.TeacherId, 'TeacherName', t.TeacherName)
        ), ''), ']') AS Teachers
    FROM UniqueClass uc
    LEFT JOIN ClassGroup cg ON cg.Id = uc.GroupId
    LEFT JOIN UniqueClassTeacher t ON t.UniqueClassId = uc.Id
    WHERE uc.Id = id
      AND uc.TenantId = tenantId
    GROUP BY uc.Id;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetScheduledClassesForTenant(IN tenantId CHAR(36))
BEGIN
    SELECT
        sc.Id,
        sc.DayOfWeekIndex,
        sc.MaxStudentLimit,
        sc.StartTime,
        sc.EndTime,
        sc.CourseId,
        sc.GroupId,
        cg.Name AS GroupName,
        sc.TenantId,
        CONCAT('[', IFNULL(GROUP_CONCAT(
            JSON_OBJECT('TeacherId', t.TeacherId, 'TeacherName', t.TeacherName)
        ), ''), ']') AS Teachers
    FROM ScheduledClass sc
    LEFT JOIN ClassGroup cg ON cg.Id = sc.GroupId
    LEFT JOIN ScheduledClassTeacher t ON t.ScheduledClassId = sc.Id
    WHERE sc.TenantId = tenantId
    GROUP BY sc.Id;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetUniqueClassesOnWeekForTenant(IN tenantId CHAR(36), IN dateInput DATE)
BEGIN
    SELECT
        uc.Id,
        uc.Date,
        uc.MaxStudentLimit,
        uc.StartTime,
        uc.EndTime,
        uc.CourseId,
        uc.GroupId,
        cg.Name AS GroupName,
        uc.TenantId,
        CONCAT('[', IFNULL(GROUP_CONCAT(
            JSON_OBJECT('TeacherId', t.TeacherId, 'TeacherName', t.TeacherName)
        ), ''), ']') AS Teachers
    FROM UniqueClass uc
    LEFT JOIN ClassGroup cg ON cg.Id = uc.GroupId
    LEFT JOIN UniqueClassTeacher t ON t.UniqueClassId = uc.Id
    WHERE uc.TenantId = tenantId
      AND uc.Date BETWEEN
            DATE_SUB(dateInput, INTERVAL WEEKDAY(dateInput) DAY)
            AND
            DATE_ADD(DATE_SUB(dateInput, INTERVAL WEEKDAY(dateInput) DAY), INTERVAL 6 DAY)
    GROUP BY uc.Id;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetUniqueClassesOnWeekByTeacherForTenant(IN tenantId CHAR(36), IN teacherId CHAR(36), IN dateInput DATE)
BEGIN
    SELECT
        uc.Id,
        uc.Date,
        uc.MaxStudentLimit,
        uc.StartTime,
        uc.EndTime,
        uc.CourseId,
        uc.GroupId,
        cg.Name AS GroupName,
        uc.TenantId,
        CONCAT('[', IFNULL(GROUP_CONCAT(
            JSON_OBJECT('TeacherId', t2.TeacherId, 'TeacherName', t2.TeacherName)
        ), ''), ']') AS Teachers
    FROM UniqueClass uc
    INNER JOIN UniqueClassTeacher tm
        ON tm.UniqueClassId = uc.Id
       AND tm.TeacherId = teacherId
       AND tm.TenantId  = tenantId
    LEFT JOIN ClassGroup cg ON cg.Id = uc.GroupId
    LEFT JOIN UniqueClassTeacher t2 ON t2.UniqueClassId = uc.Id
    WHERE uc.TenantId = tenantId
      AND uc.Date BETWEEN
            DATE_SUB(dateInput, INTERVAL WEEKDAY(dateInput) DAY)
            AND
            DATE_ADD(DATE_SUB(dateInput, INTERVAL WEEKDAY(dateInput) DAY), INTERVAL 6 DAY)
    GROUP BY uc.Id;
END //
DELIMITER ;

-- COURSE IDEMPOTENCY (ledger of processed external references for create ops)
CREATE TABLE IF NOT EXISTS CourseIdempotency (
    TenantId          VARCHAR(36)  NOT NULL,
    ExternalReference VARCHAR(128) NOT NULL,
    EntityType        VARCHAR(32)  NOT NULL,
    EntityId          VARCHAR(36)  NOT NULL,
    ProcessedAt       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (TenantId, ExternalReference)
);

-- CLASS GROUP MANAGEMENT
DELIMITER //
CREATE PROCEDURE CreateClassGroup(
    IN id       CHAR(36),
    IN name     VARCHAR(200),
    IN tenantId CHAR(36)
)
BEGIN
    INSERT INTO ClassGroup (Id, Name, TenantId)
    VALUES (id, name, tenantId);
    SELECT ROW_COUNT() AS Inserted;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE UpdateClassGroupForTenant(
    IN id       CHAR(36),
    IN newName  VARCHAR(200),
    IN tenantId CHAR(36)
)
BEGIN
    UPDATE ClassGroup g
    SET g.Name = newName
    WHERE g.Id = id
      AND g.TenantId = tenantId;
    SELECT ROW_COUNT() AS Updated;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE DeleteClassGroupForTenant(IN id CHAR(36), IN tenantId CHAR(36))
BEGIN
    DELETE g FROM ClassGroup g
    WHERE g.Id = id
      AND g.TenantId = tenantId
      AND NOT EXISTS (SELECT 1 FROM ScheduledClass sc WHERE sc.GroupId = id)
      AND NOT EXISTS (SELECT 1 FROM UniqueClass uc WHERE uc.GroupId = id);
    SELECT ROW_COUNT() AS Deleted;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetClassGroupsForTenant(IN tenantId CHAR(36))
BEGIN
    SELECT g.Id, g.Name, g.TenantId
    FROM ClassGroup g
    WHERE g.TenantId = tenantId
    ORDER BY g.Name;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE GetClassGroupsByTeacherForTenant(IN tenantId CHAR(36), IN teacherId CHAR(36))
BEGIN
    SELECT g.Id, g.Name, g.TenantId
    FROM ClassGroup g
    WHERE g.TenantId = tenantId
      AND (
            EXISTS (
                SELECT 1
                FROM ScheduledClass sc
                JOIN ScheduledClassTeacher sct ON sct.ScheduledClassId = sc.Id
                WHERE sc.GroupId = g.Id AND sct.TeacherId = teacherId
            )
         OR EXISTS (
                SELECT 1
                FROM UniqueClass uc
                JOIN UniqueClassTeacher uct ON uct.UniqueClassId = uc.Id
                WHERE uc.GroupId = g.Id AND uct.TeacherId = teacherId
            )
      )
    ORDER BY g.Name;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE ExistsClassGroupForTenant(IN tenantId CHAR(36), IN groupId CHAR(36))
BEGIN
    SELECT 1
    FROM ClassGroup g
    WHERE g.Id = groupId
      AND g.TenantId = tenantId
    LIMIT 1;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE TransferScheduledClassToGroup(IN id CHAR(36), IN targetGroupId CHAR(36), IN tenantId CHAR(36))
BEGIN
    UPDATE ScheduledClass sc
    SET sc.GroupId = targetGroupId
    WHERE sc.Id = id
      AND sc.TenantId = tenantId;
    SELECT ROW_COUNT() AS Updated;
END //
DELIMITER ;

DELIMITER //
CREATE PROCEDURE TransferUniqueClassToGroup(IN id CHAR(36), IN targetGroupId CHAR(36), IN tenantId CHAR(36))
BEGIN
    UPDATE UniqueClass uc
    SET uc.GroupId = targetGroupId
    WHERE uc.Id = id
      AND uc.TenantId = tenantId;
    SELECT ROW_COUNT() AS Updated;
END //
DELIMITER ;

-- GROUP OVERLAP CHECK (cross-kind: scheduled recurrence vs unique date)
DELIMITER //
CREATE PROCEDURE HasGroupClassOverlap(
    IN tenantId       CHAR(36),
    IN groupId        CHAR(36),
    IN candidateKind  VARCHAR(16),
    IN dayOfWeekIndex INT,
    IN classDate      DATE,
    IN startTime      TIME,
    IN endTime        TIME,
    IN excludeId      CHAR(36)
)
BEGIN
    SELECT (
        EXISTS (
            SELECT 1
            FROM ScheduledClass sc
            WHERE sc.TenantId = tenantId
              AND sc.GroupId = groupId
              AND sc.DayOfWeekIndex = dayOfWeekIndex
              AND sc.StartTime < endTime
              AND sc.EndTime > startTime
              AND (excludeId IS NULL OR sc.Id <> excludeId)
        )
        OR EXISTS (
            SELECT 1
            FROM UniqueClass uc
            WHERE uc.TenantId = tenantId
              AND uc.GroupId = groupId
              AND uc.StartTime < endTime
              AND uc.EndTime > startTime
              AND (
                    (candidateKind = 'Scheduled' AND (WEEKDAY(uc.Date) + 1) = dayOfWeekIndex)
                 OR (candidateKind = 'Unique'    AND uc.Date = classDate)
              )
              AND (excludeId IS NULL OR uc.Id <> excludeId)
        )
    ) AS HasOverlap;
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
    TRUNCATE TABLE Course;
    TRUNCATE TABLE ClassGroup;
    TRUNCATE TABLE ScheduledClass;
    TRUNCATE TABLE UniqueClass;
    TRUNCATE TABLE ScheduledClassTeacher;
    TRUNCATE TABLE UniqueClassTeacher;
    TRUNCATE TABLE CourseIdempotency;
    TRUNCATE TABLE outbox_events;
    SET FOREIGN_KEY_CHECKS = 1;
END //
DELIMITER ;
