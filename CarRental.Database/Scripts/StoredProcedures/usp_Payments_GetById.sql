CREATE OR ALTER PROCEDURE dbo.usp_Payments_GetById @PaymentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.PaymentId, p.ContractId, c.CustomerId, p.PaymentMethod, p.Amount, p.TransactionReference, p.PaymentStatus, p.PaymentDate
    FROM dbo.Payments p INNER JOIN dbo.RentalContracts c ON c.ContractId = p.ContractId WHERE p.PaymentId = @PaymentId;
END;
GO
