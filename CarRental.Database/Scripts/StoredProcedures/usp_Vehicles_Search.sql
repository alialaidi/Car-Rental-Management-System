CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_Search
    @CategoryId INT = NULL,
    @FuelTypeId INT = NULL,
    @TransmissionTypeId INT = NULL,
    @BranchId INT = NULL,
    @MinimumDailyPrice DECIMAL(18,2) = NULL,
    @MaximumDailyPrice DECIMAL(18,2) = NULL,
    @MinimumSeats INT = NULL,
    @PickupDateTime DATETIME2(3) = NULL,
    @ExpectedReturnDateTime DATETIME2(3) = NULL,
    @Page INT = 1,
    @PageSize INT = 20,
    @SortBy NVARCHAR(20) = N'price',
    @SortDirection NVARCHAR(4) = N'asc'
AS
BEGIN
    SET NOCOUNT ON;

    IF (@PickupDateTime IS NULL AND @ExpectedReturnDateTime IS NOT NULL)
       OR (@PickupDateTime IS NOT NULL AND @ExpectedReturnDateTime IS NULL)
        THROW 50010, 'Pickup and return dates must be supplied together.', 1;
    IF @PickupDateTime IS NOT NULL AND @ExpectedReturnDateTime <= @PickupDateTime
        THROW 50011, 'Expected return must be after pickup.', 1;
    IF @Page < 1 SET @Page = 1;
    IF @PageSize < 1 SET @PageSize = 1;
    IF @PageSize > 100 SET @PageSize = 100;

    SELECT
        v.VehicleId,
        v.PlateNumber,
        v.Make,
        v.Model,
        v.Year,
        v.CategoryId,
        c.Name AS CategoryName,
        v.FuelTypeId,
        f.Name AS FuelTypeName,
        v.TransmissionTypeId,
        t.Name AS TransmissionTypeName,
        v.SeatsCount,
        v.DailyPrice,
        v.BranchId,
        v.Status,
        v.CurrentMileage,
        primaryImage.ImageUrl AS PrimaryImageUrl
    INTO #FilteredVehicles
    FROM dbo.Vehicles AS v
    INNER JOIN dbo.VehicleCategories AS c ON c.CategoryId = v.CategoryId
    INNER JOIN dbo.FuelTypes AS f ON f.FuelTypeId = v.FuelTypeId
    INNER JOIN dbo.TransmissionTypes AS t ON t.TransmissionTypeId = v.TransmissionTypeId
    CROSS APPLY dbo.fn_Vehicles_IsAvailable(v.VehicleId, @PickupDateTime, @ExpectedReturnDateTime) AS availability
    OUTER APPLY
    (
        SELECT TOP (1) vi.ImageUrl
        FROM dbo.VehicleImages AS vi
        WHERE vi.VehicleId = v.VehicleId
        ORDER BY vi.IsPrimary DESC, vi.ImageId ASC
    ) AS primaryImage
    WHERE availability.IsAvailable = 1
      AND (@CategoryId IS NULL OR v.CategoryId = @CategoryId)
      AND (@FuelTypeId IS NULL OR v.FuelTypeId = @FuelTypeId)
      AND (@TransmissionTypeId IS NULL OR v.TransmissionTypeId = @TransmissionTypeId)
      AND (@BranchId IS NULL OR v.BranchId = @BranchId)
      AND (@MinimumDailyPrice IS NULL OR v.DailyPrice >= @MinimumDailyPrice)
      AND (@MaximumDailyPrice IS NULL OR v.DailyPrice <= @MaximumDailyPrice)
      AND (@MinimumSeats IS NULL OR v.SeatsCount >= @MinimumSeats);

    SELECT COUNT(*) AS TotalCount FROM #FilteredVehicles;

    SELECT
        VehicleId, PlateNumber, Make, Model, Year,
        CategoryId, CategoryName, FuelTypeId, FuelTypeName,
        TransmissionTypeId, TransmissionTypeName, SeatsCount,
        DailyPrice, BranchId, Status, CurrentMileage, PrimaryImageUrl
    FROM #FilteredVehicles
    ORDER BY
        CASE WHEN @SortBy = N'price' AND @SortDirection = N'asc' THEN DailyPrice END ASC,
        CASE WHEN @SortBy = N'price' AND @SortDirection = N'desc' THEN DailyPrice END DESC,
        CASE WHEN @SortBy = N'year' AND @SortDirection = N'asc' THEN [Year] END ASC,
        CASE WHEN @SortBy = N'year' AND @SortDirection = N'desc' THEN [Year] END DESC,
        CASE WHEN @SortBy = N'make' AND @SortDirection = N'asc' THEN Make END ASC,
        CASE WHEN @SortBy = N'make' AND @SortDirection = N'desc' THEN Make END DESC,
        CASE WHEN @SortBy = N'model' AND @SortDirection = N'asc' THEN Model END ASC,
        CASE WHEN @SortBy = N'model' AND @SortDirection = N'desc' THEN Model END DESC,
        VehicleId ASC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
