-- =====================================================================
-- Table: AuditLogs
-- Purpose: Audit trail for security-sensitive / business-critical actions.
-- =====================================================================
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
GO

CREATE TABLE dbo.AuditLogs
(
    AuditId       INT IDENTITY(1,1)  NOT NULL,
    ActorUserId    INT                NULL,          -- NULL allowed: e.g. failed login with unknown username
    Action          NVARCHAR(100)      NOT NULL,
    EntityName       NVARCHAR(100)      NOT NULL,
    EntityId          NVARCHAR(50)       NULL,
    OldValue           NVARCHAR(MAX)      NULL,
    NewValue            NVARCHAR(MAX)      NULL,
    IPAddress            NVARCHAR(50)       NULL,
    DeviceInfo            NVARCHAR(255)      NULL,
    [Timestamp]             DATETIME2(3)       NOT NULL CONSTRAINT DF_AuditLogs_Timestamp DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (AuditId),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (ActorUserId) REFERENCES dbo.Users (UserId)
);
GO

CREATE NONCLUSTERED INDEX IX_AuditLogs_ActorUserId ON dbo.AuditLogs (ActorUserId);
CREATE NONCLUSTERED INDEX IX_AuditLogs_Timestamp ON dbo.AuditLogs ([Timestamp]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_EntityName_EntityId ON dbo.AuditLogs (EntityName, EntityId);
GO
