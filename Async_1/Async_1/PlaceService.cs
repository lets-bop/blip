using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Async_1
{
    public interface IPlaceService
    {
        public Task<IList<Place>> GetAllPlacesAsync(string placeTypes, CancellationToken cancellationToken);
    }

    public class PlaceService : IPlaceService
    {
        public async Task<IList<Place>> GetAllPlacesAsync(string placeTypes, CancellationToken cancellationToken = default)
        {
            var places = new List<Place>();

            using (var stream =
                new StreamReader(File.OpenRead(@"places.csv")))
            {
                await stream.ReadLineAsync(); // Skip the header how in the CSV

                string line;
                while ((line = await stream.ReadLineAsync()) != null)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var place = Place.FromCSV(line);
                    await Task.Delay(100); // simulating a delay

                    if (place.Type.ToLowerInvariant() == placeTypes.ToLowerInvariant())
                    {
                        places.Add(place);
                    }
                }
            }

            if (!places.Any())
            {
                throw new KeyNotFoundException($"Could not find any matches for {placeTypes}");
            }

            return places;
        }
    }

    public class MockPlaceService : IPlaceService
    {
        public async Task<IList<Place>> GetAllPlacesAsync(string placeTypes, CancellationToken cancellationToken = default)
        {
            var places = new List<Place>();

            await Task.Delay(500);
            places.Add(new Place { Type = "Restaurant", Name = "R1", Rating = 3.5m, Cost = "$$$$", Distance = 0.1m });

            await Task.Delay(500);
            places.Add(new Place { Type = "Cafe", Name = "C1", Rating = 3.5m, Cost = "$$", Distance = 0.1m });

            await Task.Delay(500);
            places.Add(new Place { Type = "Restaurant", Name = "R2", Rating = 3.5m, Cost = "$$$", Distance = 0.2m });

            await Task.Delay(500);
            places.Add(new Place { Type = "Restaurant", Name = "R3", Rating = 3.5m, Cost = "$", Distance = 0.3m });

            await Task.Delay(500);
            places.Add(new Place { Type = "Cafe", Name = "C2", Rating = 3.5m, Cost = "$$", Distance = 0.5m });

            return places;
        }
    }
}
