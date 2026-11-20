namespace ParkirajBa.Services;

public static class RefundCalculator
{
    public static decimal IzracunajPovrat(decimal cijena, DateTime pocetak, DateTime trenutakOtkazivanja)
    {
        var preostalo = pocetak - trenutakOtkazivanja;
        return preostalo >= TimeSpan.FromHours(24) ? cijena : cijena / 2m;
    }
}
