namespace _CarRental.Application.Common.Interfaces;

public interface IAlertNotifier
{
    void RecordServerError();
    void RecordFailedLogin(string? sourceAddress);
}
