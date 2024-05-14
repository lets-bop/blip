using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlacementStore
{
    public class PlacementStoreActivitySource
    {
        private const string PlacementStoreActivitySourceName = "AZAllocator.PlacementStore";
        private const string version = "1.0";
        public static readonly ActivitySource Value = new ActivitySource(PlacementStoreActivitySourceName, version);
    }
}
