namespace ParkirajBa.Services;

public static class RefundCalculator
{
    private static readonly TimeSpan MinimalnoVrijemeZaPovrat = TimeSpan.FromHours(2);
    private static readonly TimeSpan GranicaZaPuniPovrat = TimeSpan.FromHours(24);

    public static decimal IzracunajPovrat(
        decimal cijena,
        DateTime pocetak,
        DateTime trenutakOtkazivanja)
    {
        var preostaloVrijeme = pocetak - trenutakOtkazivanja;

        if (preostaloVrijeme >= MinimalnoVrijemeZaPovrat &&
            preostaloVrijeme < GranicaZaPuniPovrat)
        {
            return cijena * 0.5m;
        }

        return 0m;
    }
}
