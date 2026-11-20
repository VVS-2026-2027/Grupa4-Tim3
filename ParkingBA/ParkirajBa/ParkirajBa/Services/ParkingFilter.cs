using ParkirajBa.Models;

namespace ParkirajBa.Services;

public static class ParkingFilter
{
    public static List<ParkingObject> FiltrirajPoSlobodnimMjestima(IEnumerable<ParkingObject> parkinzi, int minimalnoMjesta)
    {
        return parkinzi
            .Where(parking => parking.availableSpots >= minimalnoMjesta)
            .ToList();
    }
}
