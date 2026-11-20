namespace ParkirajBa.Services;

public static class RefundCalculator
{
    private const string OtkazivanjeNakonPocetkaPoruka = "Rezervacija se ne može otkazati nakon početka.";
    private static readonly TimeSpan PuniPovratniRok = TimeSpan.FromHours(24);
    private static readonly TimeSpan MinimalniPovratniRok = TimeSpan.FromHours(2);

    public static decimal IzracunajPovrat(decimal cijena, DateTime pocetak, DateTime trenutakOtkazivanja)
    {
        if (cijena is < 0m)
            throw new ArgumentOutOfRangeException(nameof(cijena));

        var preostalo = pocetak - trenutakOtkazivanja;

        if (preostalo <= TimeSpan.Zero)
            throw new InvalidOperationException(OtkazivanjeNakonPocetkaPoruka);

        return preostalo >= PuniPovratniRok
            ? Math.Round(cijena, 2, MidpointRounding.AwayFromZero)
            : preostalo >= MinimalniPovratniRok
                ? Math.Round(cijena / 2m, 2, MidpointRounding.AwayFromZero)
                : 0m;
    }
}
