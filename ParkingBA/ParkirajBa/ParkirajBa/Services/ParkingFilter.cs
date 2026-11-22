using ParkirajBa.Models;

namespace ParkirajBa.Services;

public static class ParkingFilter
{
    public static List<ParkingObject> FiltrirajPoSlobodnimMjestima(IEnumerable<ParkingObject> parkinzi, int minimalnoMjesta)
    {
        if (minimalnoMjesta < 0)
        {
            throw new ArgumentException();
        }

        return parkinzi
            .Where(parking => parking.availableSpots >= minimalnoMjesta)
            .OrderByDescending(parking => parking.availableSpots)
            .ToList();
    }
}
