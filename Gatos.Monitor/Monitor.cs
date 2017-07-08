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
        private const int ConsecutiveViolationsThreshold = 3;

        private Timer _sampleTimer;
        private PerformanceCounter _performanceCounter;
        private volatile bool _intensiveMode;
        private int _consecutiveViolations;
        private string _name;

        protected abstract float CounterThreshold { get; }
        protected abstract string PerformanceCounter { get; }
        protected abstract string PerformanceCategory { get; }
        protected abstract string PerformanceInstance { get; }

        protected virtual void OnIntensiveSamplingStart()
        {
        }

        protected virtual void OnIntensiveSamplingDone()
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
            OnIntensiveSamplingStart();
            OnIntensiveSamplingDone();
            _intensiveMode = false;
            WriteTraceLine("Intensive sampling mode done");
        }
    }
}
