namespace ParkirajBa.Services;

public interface IPaymentGateway
{
    Task RefundAsync(string kod, decimal iznos);
}
