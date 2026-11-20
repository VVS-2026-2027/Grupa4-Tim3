namespace ParkirajBa.Services;

public class RefundService
{
    private readonly IPaymentGateway _paymentGateway;

    public RefundService(IPaymentGateway paymentGateway)
    {
        _paymentGateway = paymentGateway;
    }

    public Task RefundAsync(string kod, decimal iznos) =>
        _paymentGateway.RefundAsync(kod, iznos);
}
