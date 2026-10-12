CREATE OR ALTER PROCEDURE dbo.usp_Users_List @Page INT, @PageSize INT, @Search NVARCHAR(255)=NULL, @IsActive BIT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Username, Email, PasswordHash, PhoneNumber, IsActive, CreatedAt
    FROM dbo.Users
    WHERE (@IsActive IS NULL OR IsActive=@IsActive)
      AND (@Search IS NULL OR Username LIKE N'%' + @Search + N'%' OR Email LIKE N'%' + @Search + N'%')
    ORDER BY UserId OFFSET (CONVERT(BIGINT, @Page) - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

