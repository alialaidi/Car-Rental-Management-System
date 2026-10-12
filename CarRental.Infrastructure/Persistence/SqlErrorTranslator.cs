using _CarRental.Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence;

internal static class SqlErrorTranslator
{
    public static void ThrowIfKnown(SqlException exception)
    {
        switch (exception.Number)
        {
            case 2601:
            case 2627:
                throw new ConflictException("DUPLICATE_VALUE", "A record with the supplied value already exists.");
            case 547:
                throw new ConflictException("RELATED_RECORD_CONFLICT", "The operation conflicts with related data.");
            case 50001:
                throw new ConflictException("VEHICLE_NOT_AVAILABLE", "The selected vehicle is not available.");
            case 50004:
                throw new ConflictException("RESERVATION_STATE_CONFLICT", "The reservation or pickup branch changed and the request could not be applied.");
            case 50002:
                throw new BusinessRuleException("INVALID_STATE_TRANSITION", "The requested state change is not allowed.");
            case 50006:
                throw new BusinessRuleException("DRIVING_LICENSE_EXPIRED", "The driving license must remain valid through the expected return date.");
            case 50003:
                throw new ConflictException("OVERPAYMENT_NOT_ALLOWED", "The payment exceeds the outstanding balance.");
            case 50020:
                throw new ConflictException("USERNAME_ALREADY_EXISTS", "A user with that username already exists.");
            case 50021:
                throw new ConflictException("EMAIL_ALREADY_EXISTS", "A user with that email already exists.");
            case 50022:
                throw new ConflictException("DRIVING_LICENSE_ALREADY_EXISTS", "A customer with that driving license already exists.");
            case 50040:
            case 50043:
            case 50044:
                throw new NotFoundException("The requested record does not exist.");
            case 50041:
            case 50042:
                throw new ConflictException("RELATED_RECORD_CONFLICT", "The requested change conflicts with related records.");
            case 50051:
                throw new ConflictException("LOOKUP_IN_USE", "The lookup value is referenced by existing vehicles.");
            case 50054:
            case 50056:
            case 50057:
                throw new NotFoundException("The requested fleet record does not exist.");
            case 50060:
                throw new NotFoundException("The requested reservation does not exist.");
            case 50061:
                throw new ConflictException("RESERVATION_NOT_CONFIRMED", "Only a confirmed reservation with a reserved vehicle can be converted to a contract.");
            case 50062:
                throw new BusinessRuleException("INVALID_PICKUP_MILEAGE", "Pickup mileage cannot be less than the vehicle's current mileage.");
            case 50063:
                throw new ConflictException("VEHICLE_PRICE_CHANGED", "The vehicle price changed while the contract was being prepared. Please retry.");
            case 50064:
                throw new ConflictException("CONTRACT_RESERVATION_REQUIRED", "The contract is not linked to a reservation.");
            case 50065:
                throw new BusinessRuleException("RETURN_BEFORE_PICKUP", "Actual return date cannot be before pickup.");
            case 50066:
                throw new BusinessRuleException("INVALID_RETURN_MILEAGE", "Return mileage cannot be less than pickup mileage.");
            case 50067:
                throw new ConflictException("CONTRACT_NOT_ACTIVE", "Only active contracts can be returned.");
            case 50068:
                throw new ConflictException("RESERVATION_STATE_CONFLICT", "The reservation is not in a returnable state.");
            case 50069:
                throw new ConflictException("VEHICLE_STATE_CONFLICT", "The vehicle is not in a returnable state.");
            case 50070:
                throw new BusinessRuleException("INVALID_RENTAL_DAYS", "Final rental days do not match the return date.");
            case 50055:
                throw new BusinessRuleException("INVALID_LOOKUP_TYPE", "The lookup type is invalid.");
            default:
                if (exception.Number >= 50000)
                {
                    throw new BusinessRuleException("DATABASE_BUSINESS_RULE", "The requested operation could not be completed because of a business rule.");
                }

                return;
        }
    }
}
