using LiveStacks;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gatos.Monitor
{
    public abstract class EventStacksMonitor : Monitor
    {
        private static readonly TimeSpan IntensiveSamplingDuration = TimeSpan.FromSeconds(5);
        private const int TopStacksToReport = 3;

        private StackResolver _resolver = new StackResolver();

        protected abstract string EventSpecification { get; }

        protected virtual void OnEventOccurred(TraceEvent @event)
        {
        }

        protected override void OnIntensiveSamplingStart()
        {
            int currentPid = Process.GetCurrentProcess().Id;
            var session = new LiveSession(EventSpecification, new[] { currentPid }, includeKernelFrames: false);
            session.EventOccurred += OnEventOccurred;
            Task.Run(() => session.Start());
            Thread.Sleep(IntensiveSamplingDuration);
            session.Stop();

            WriteTraceLine($"Top {TopStacksToReport} stacks gathered during intensive sampling:");
            foreach (var stack in session.Stacks.TopStacks(TopStacksToReport, 0))
            {
                WriteTraceLine("");
                WriteTraceLine($"  {stack.Count,10}");
                foreach (var symbol in _resolver.Resolve(currentPid, stack.Addresses))
                {
                    WriteTraceLine("    " + symbol.ToString());
                }
            }
        }
    }
}
