CREATE OR ALTER PROCEDURE dbo.usp_Reviews_Update @ReviewId INT, @CustomerId INT, @Rating INT, @Comment NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @OwnTransaction BIT = 0, @ContractId INT, @OwnedContractId INT;
    IF @@TRANCOUNT = 0
    BEGIN
        BEGIN TRANSACTION;
        SET @OwnTransaction = 1;
    END;
    BEGIN TRY
        SELECT @ContractId = ContractId FROM dbo.Reviews WHERE ReviewId = @ReviewId;
        IF @ContractId IS NULL THROW 50093, 'Review not found.', 1;
        SELECT @OwnedContractId = ContractId FROM dbo.RentalContracts WITH (UPDLOCK, HOLDLOCK)
        WHERE ContractId = @ContractId AND CustomerId = @CustomerId;
        IF @OwnedContractId IS NULL THROW 50093, 'Review not found.', 1;
        SET @ContractId = NULL;
        SELECT @ContractId = ContractId FROM dbo.Reviews WITH (UPDLOCK, HOLDLOCK)
        WHERE ReviewId = @ReviewId AND ContractId = @OwnedContractId;
        IF @ContractId IS NULL THROW 50093, 'Review not found.', 1;
        IF @Rating NOT BETWEEN 1 AND 5 OR LEN(COALESCE(@Comment, N'')) > 1000
            THROW 50002, 'Review details are invalid.', 1;
        UPDATE dbo.Reviews SET Rating = @Rating, Comment = NULLIF(LTRIM(RTRIM(@Comment)), N'') WHERE ReviewId = @ReviewId;
        IF @OwnTransaction = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @OwnTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
