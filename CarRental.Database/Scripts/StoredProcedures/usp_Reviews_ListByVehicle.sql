CREATE OR ALTER PROCEDURE dbo.usp_Reviews_ListByVehicle @VehicleId INT, @Page INT, @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(r.ReviewId) AS TotalCount, COUNT(r.ReviewId) AS ReviewCount,
        CAST(AVG(CAST(r.Rating AS DECIMAL(5,2))) AS DECIMAL(5,2)) AS AverageRating
    FROM dbo.RentalContracts c LEFT JOIN dbo.Reviews r ON r.ContractId = c.ContractId
    WHERE c.VehicleId = @VehicleId;

    SELECT r.ReviewId, r.ContractId, c.CustomerId, c.VehicleId, cu.FullName AS CustomerName,
        r.Rating, r.Comment, r.CreatedAt
    FROM dbo.Reviews r
    INNER JOIN dbo.RentalContracts c ON c.ContractId = r.ContractId
    INNER JOIN dbo.Customers cu ON cu.CustomerId = c.CustomerId
    WHERE c.VehicleId = @VehicleId
    ORDER BY r.CreatedAt DESC, r.ReviewId DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
