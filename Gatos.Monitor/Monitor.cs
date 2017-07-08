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
    public abstract class Monitor
    {
        private static readonly TimeSpan SamplingInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan IntensiveSamplingDuration = TimeSpan.FromSeconds(5);
        private const int ConsecutiveViolationsThreshold = 3;
        private const int TopStacksToReport = 3;

        private Timer _sampleTimer;
        private PerformanceCounter _performanceCounter;
        private bool _intensiveMode;
        private int _consecutiveViolations;
        private StackResolver _resolver = new StackResolver();
        private string _name;

        protected abstract float CounterThreshold { get; }
        protected abstract string EventSpecification { get; }
        protected abstract string PerformanceCounter { get; }
        protected abstract string PerformanceCategory { get; }
        protected abstract string PerformanceInstance { get; }

        protected virtual void OnIntensiveSamplingDone()
        {
        }

        protected virtual void OnEventOccurred(TraceEvent @event)
        {
        }

        protected Monitor()
        {
            _name = GetType().Name;
            _performanceCounter = new PerformanceCounter(PerformanceCategory, PerformanceCounter, PerformanceInstance, readOnly: true);
            _sampleTimer = new Timer(SampleCounter, null, SamplingInterval, SamplingInterval);
        }

        protected void WriteTraceLine(string line)
        {
            Trace.WriteLine($"[{_name}] {line}");
        }

        private void SampleCounter(object ignore)
        {
            if (_intensiveMode)
                return;

            float counterValue = _performanceCounter.NextValue();
            WriteTraceLine($"Sampling timer invoked, current counter value: {counterValue}");
            if (counterValue > CounterThreshold)
            {
                if (++_consecutiveViolations == ConsecutiveViolationsThreshold)
                {
                    WriteTraceLine($"Counter value violates threshold {CounterThreshold} for {_consecutiveViolations} times, starting intensive sampling mode");
                    _consecutiveViolations = 0;
                    _intensiveMode = true;
                    Task.Run((Action)SampleIntensively);
                }
                else
                {
                    WriteTraceLine($"Counter value violates threshold {CounterThreshold} for {_consecutiveViolations} times, waiting for {ConsecutiveViolationsThreshold} violations");
                }
            }
            else
            {
                _consecutiveViolations = 0;
            }
        }

        private void SampleIntensively()
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

            OnIntensiveSamplingDone();
            _intensiveMode = false;
            WriteTraceLine("Intensive sampling mode done");
        }
    }
}
