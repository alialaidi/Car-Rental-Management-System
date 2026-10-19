CREATE OR ALTER PROCEDURE dbo.usp_Reviews_GetById @ReviewId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.ReviewId, r.ContractId, c.CustomerId, c.VehicleId, cu.FullName AS CustomerName,
        r.Rating, r.Comment, r.CreatedAt
    FROM dbo.Reviews r
    INNER JOIN dbo.RentalContracts c ON c.ContractId = r.ContractId
    INNER JOIN dbo.Customers cu ON cu.CustomerId = c.CustomerId
    WHERE r.ReviewId = @ReviewId;
END;
GO
