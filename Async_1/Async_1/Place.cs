using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Async_1
{
    public class Place
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public decimal? Rating { get; set; }
        public string Cost { get; set; }
        public decimal? Distance { get; set; }

        public static Place FromCSV(string text)
        {
            var segments = text.Split(',');
            if (segments.Length != 5) return null;
            var place = new Place
            {
                Type = segments[0].Trim(),
                Name = segments[1].Trim(),
                Rating = Convert.ToDecimal(segments[2].Trim(), CultureInfo.InvariantCulture),
                Cost = segments[3].Trim(),
                Distance = Convert.ToDecimal(segments[4].Trim(), CultureInfo.InvariantCulture)
            };

            return place;
        }
    }
}
