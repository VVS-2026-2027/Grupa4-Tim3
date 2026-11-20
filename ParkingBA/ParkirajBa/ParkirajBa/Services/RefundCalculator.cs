namespace ParkirajBa.Services;

public static class RefundCalculator
{
    public static decimal IzracunajPovrat(
        decimal cijena,
        DateTime pocetak,
        DateTime trenutakOtkazivanja)
    {
        var preostaloVrijeme = pocetak - trenutakOtkazivanja;

        if (preostaloVrijeme >= TimeSpan.FromHours(2) &&
            preostaloVrijeme < TimeSpan.FromHours(24))
        {
            return cijena * 0.5m;
        }

        return 0m;
    }
}
