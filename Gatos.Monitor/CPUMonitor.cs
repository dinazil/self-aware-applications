using LiveStacks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gatos.Monitor
{
    public class CPUMonitor : Monitor
    {
        protected override float CounterThreshold => 90.0f / Environment.ProcessorCount; // 90% of one CPU

        protected override string EventSpecification => "kernel:profile";

        protected override string PerformanceCounter => "% Processor Time";

        protected override string PerformanceCategory => "Processor";

        protected override string PerformanceInstance => "_Total";

        public static CPUMonitor Start()
        {
            return new CPUMonitor();
        }
    }
}
