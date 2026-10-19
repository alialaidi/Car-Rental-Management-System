CREATE OR ALTER PROCEDURE dbo.usp_AuditLogs_Search
    @Offset BIGINT,
    @PageSize INT,
    @ActorUserId INT = NULL,
    @Action NVARCHAR(100) = NULL,
    @EntityName NVARCHAR(100) = NULL,
    @FromUtc DATETIME2(3) = NULL,
    @ToUtcExclusive DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @Offset = CASE WHEN @Offset < 0 THEN 0 ELSE @Offset END;
    SET @PageSize = CASE WHEN @PageSize < 1 THEN 1 WHEN @PageSize > 100 THEN 100 ELSE @PageSize END;

    SELECT COUNT(*) AS TotalCount
    FROM dbo.AuditLogs
    WHERE (@ActorUserId IS NULL OR ActorUserId = @ActorUserId)
      AND (@Action IS NULL OR Action = @Action)
      AND (@EntityName IS NULL OR EntityName = @EntityName)
      AND (@FromUtc IS NULL OR [Timestamp] >= @FromUtc)
      AND (@ToUtcExclusive IS NULL OR [Timestamp] < @ToUtcExclusive);

    SELECT AuditId, ActorUserId, Action, EntityName, EntityId, OldValue, NewValue, IPAddress, DeviceInfo, [Timestamp]
    FROM dbo.AuditLogs
    WHERE (@ActorUserId IS NULL OR ActorUserId = @ActorUserId)
      AND (@Action IS NULL OR Action = @Action)
      AND (@EntityName IS NULL OR EntityName = @EntityName)
      AND (@FromUtc IS NULL OR [Timestamp] >= @FromUtc)
      AND (@ToUtcExclusive IS NULL OR [Timestamp] < @ToUtcExclusive)
    ORDER BY [Timestamp] DESC, AuditId DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
