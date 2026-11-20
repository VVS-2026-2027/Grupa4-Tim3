namespace ParkirajBa.Services;

public static class RefundCalculator
{
    private static readonly TimeSpan PuniPovratniRok = TimeSpan.FromHours(24);

    public static decimal IzracunajPovrat(decimal cijena, DateTime pocetak, DateTime trenutakOtkazivanja)
    {
        var preostalo = pocetak - trenutakOtkazivanja;
        return preostalo >= PuniPovratniRok ? cijena : cijena / 2m;
    }
}
