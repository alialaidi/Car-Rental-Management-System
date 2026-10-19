CREATE OR ALTER PROCEDURE dbo.usp_Reviews_Create @ContractId INT, @CustomerId INT, @Rating INT, @Comment NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @OwnTransaction BIT = 0, @OwnerCustomerId INT, @ContractStatus NVARCHAR(20), @ReviewId INT;
    IF @@TRANCOUNT = 0
    BEGIN
        BEGIN TRANSACTION;
        SET @OwnTransaction = 1;
    END;
    BEGIN TRY
        SELECT @OwnerCustomerId = CustomerId, @ContractStatus = Status
        FROM dbo.RentalContracts WITH (UPDLOCK, HOLDLOCK) WHERE ContractId = @ContractId;
        IF @OwnerCustomerId IS NULL THROW 50040, 'Contract not found.', 1;
        IF @OwnerCustomerId <> @CustomerId THROW 50093, 'Contract not found for this customer.', 1;
        IF @ContractStatus <> N'Completed' THROW 50091, 'Only completed contracts can be reviewed.', 1;
        IF @Rating NOT BETWEEN 1 AND 5 OR LEN(COALESCE(@Comment, N'')) > 1000
            THROW 50002, 'Review details are invalid.', 1;
        INSERT dbo.Reviews(ContractId, Rating, Comment) VALUES (@ContractId, @Rating, NULLIF(LTRIM(RTRIM(@Comment)), N''));
        SET @ReviewId = CONVERT(INT, SCOPE_IDENTITY());
        IF @OwnTransaction = 1 COMMIT TRANSACTION;
        SELECT @ReviewId;
    END TRY
    BEGIN CATCH
        IF @OwnTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
