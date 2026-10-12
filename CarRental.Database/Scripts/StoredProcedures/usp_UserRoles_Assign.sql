CREATE OR ALTER PROCEDURE dbo.usp_UserRoles_Assign
    @UserId INT,
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.UserRoles WITH (UPDLOCK, HOLDLOCK)
        WHERE UserId = @UserId AND RoleId = @RoleId
    )
    BEGIN
        INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
    END;
END;
GO
