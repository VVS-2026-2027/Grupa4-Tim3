using ParkirajBa.Models;

namespace ParkirajBa.Services;

public static class ParkingFilter
{
    public static List<ParkingObject> FiltrirajPoSlobodnimMjestima(IEnumerable<ParkingObject> parkinzi, int minimalnoMjesta)
    {
        ArgumentNullException.ThrowIfNull(parkinzi);

        if (minimalnoMjesta < 0)
        {
            throw new ArgumentException("Minimalni broj mjesta ne može biti negativan.", nameof(minimalnoMjesta));
        }

        return parkinzi
            .Where(parking => parking.availableSpots >= minimalnoMjesta)
            .OrderByDescending(parking => parking.availableSpots)
            .ToList();
    }
}
