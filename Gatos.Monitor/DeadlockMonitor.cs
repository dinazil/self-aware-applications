using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gatos.Monitor
{
    public class DeadlockMonitor
    {
        private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(10);

        private Timer _sampleTimer;

        public static DeadlockMonitor Start()
        {
            return new DeadlockMonitor();
        }

        private DeadlockMonitor()
        {
            _sampleTimer = new Timer(FindDeadlocks, null, SampleInterval, SampleInterval);
        }

        private void WriteTraceLine(string line)
        {
            Trace.WriteLine($"[DeadlockMonitor] {line}");
        }

        private void FindDeadlocks(object dummy)
        {
            WriteTraceLine("Attempting to find deadlocks");
            using (var target = DataTarget.AttachToProcess(Process.GetCurrentProcess().Id, 1000, AttachFlag.Passive))
            {
                var runtime = target.ClrVersions[0].CreateRuntime();
                foreach (var thread in runtime.Threads)
                {
                    FindDeadlockStartingFrom(thread, new HashSet<uint>(), new Stack<string>());
                }
            }
        }

        private void FindDeadlockStartingFrom(ClrThread thread, HashSet<uint> visitedThreadIds, Stack<string> chain)
        {
            if (thread == null)
                return;

            if (visitedThreadIds.Contains(thread.OSThreadId))
            {
                WriteTraceLine("Deadlock found between the following threads:");
                foreach (var entry in chain.Reverse())
                {
                    WriteTraceLine($"  {entry} -->");
                }
                WriteTraceLine($"  Thread {thread.OSThreadId}, DEADLOCK!");
                return;
            }

            visitedThreadIds.Add(thread.OSThreadId);
            string topStackTrace = String.Join("\n      ",
                thread.StackTrace.Where(f => f.Kind == ClrStackFrameType.ManagedMethod)
                                 .Select(f => f.DisplayString)
                                 .Take(5));
            chain.Push($"Thread {thread.OSThreadId} at {topStackTrace}");

            foreach (var blockingObject in thread.BlockingObjects)
            {
                chain.Push($"{blockingObject.Reason} at {blockingObject.Object:X}");
                foreach (var owner in blockingObject.Owners)
                {
                    FindDeadlockStartingFrom(owner, visitedThreadIds, chain);
                }
                chain.Pop();
            }

            chain.Pop();
            visitedThreadIds.Remove(thread.OSThreadId);
        }
    }
}
