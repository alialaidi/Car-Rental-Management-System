CREATE OR ALTER PROCEDURE dbo.usp_Users_Count @Search NVARCHAR(255)=NULL, @IsActive BIT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.Users
    WHERE (@IsActive IS NULL OR IsActive=@IsActive)
      AND (@Search IS NULL OR Username LIKE N'%' + @Search + N'%' OR Email LIKE N'%' + @Search + N'%');
END;
GO
