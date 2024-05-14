using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Worker
{
    public static class WorkerActivitySource
    {
        private const string WorkerActivitySourceName = "AZAllocator.WorkerService";
        private const string version = "1.0";
        public static readonly ActivitySource Value = new ActivitySource(WorkerActivitySourceName, version);
    }
}
