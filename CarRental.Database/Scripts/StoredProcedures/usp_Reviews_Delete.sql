CREATE OR ALTER PROCEDURE dbo.usp_Reviews_Delete @ReviewId INT, @CustomerId INT
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
        DELETE FROM dbo.Reviews WHERE ReviewId = @ReviewId AND ContractId = @OwnedContractId;
        IF @@ROWCOUNT = 0 THROW 50093, 'Review not found.', 1;
        IF @OwnTransaction = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @OwnTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
