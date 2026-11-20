namespace ParkirajBa.Services;

public static class RefundCalculator
{
    private static readonly TimeSpan PuniPovratniRok = TimeSpan.FromHours(24);
    private static readonly TimeSpan MinimalniPovratniRok = TimeSpan.FromHours(2);

    public static decimal IzracunajPovrat(decimal cijena, DateTime pocetak, DateTime trenutakOtkazivanja)
    {
        var preostalo = pocetak - trenutakOtkazivanja;

        if (preostalo <= TimeSpan.Zero)
            throw new InvalidOperationException("Rezervacija se ne može otkazati nakon početka.");

        return preostalo >= PuniPovratniRok
            ? cijena
            : preostalo >= MinimalniPovratniRok
                ? cijena / 2m
                : 0m;
    }
}
