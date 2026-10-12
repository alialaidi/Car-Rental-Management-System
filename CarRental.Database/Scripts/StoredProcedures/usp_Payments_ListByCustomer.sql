CREATE OR ALTER PROCEDURE dbo.usp_Payments_ListByCustomer @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.PaymentId, p.ContractId, c.CustomerId, p.PaymentMethod, p.Amount, p.TransactionReference, p.PaymentStatus, p.PaymentDate
    FROM dbo.Payments p INNER JOIN dbo.RentalContracts c ON c.ContractId = p.ContractId
    WHERE c.CustomerId = @CustomerId ORDER BY p.PaymentDate DESC, p.PaymentId DESC;
END;
GO
