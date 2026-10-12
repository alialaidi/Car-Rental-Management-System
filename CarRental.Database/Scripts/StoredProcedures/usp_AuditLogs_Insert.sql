CREATE OR ALTER PROCEDURE dbo.usp_AuditLogs_Insert
    @Action NVARCHAR(100),
    @EntityName NVARCHAR(100),
    @Timestamp DATETIME2(3),
    @ActorUserId INT = NULL,
    @EntityId NVARCHAR(50) = NULL,
    @OldValue NVARCHAR(MAX) = NULL,
    @NewValue NVARCHAR(MAX) = NULL,
    @IPAddress NVARCHAR(50) = NULL,
    @DeviceInfo NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO dbo.AuditLogs
        (ActorUserId, Action, EntityName, EntityId, OldValue, NewValue, IPAddress, DeviceInfo, [Timestamp])
    VALUES
        (@ActorUserId, @Action, @EntityName, @EntityId, @OldValue, @NewValue, @IPAddress, @DeviceInfo, @Timestamp);
END;
GO
