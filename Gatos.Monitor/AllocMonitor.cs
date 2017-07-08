using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Runtime;

namespace Gatos.Monitor
{
    public class AllocMonitor : Monitor
    {
        private long _totalAllocs;
        private long _totalAllocatedBytes;
        private Dictionary<string, long> _allocsPerType = new Dictionary<string, long>();

        protected override float CounterThreshold => 100000; // ~100KB allocated per second

        protected override string EventSpecification => "clr:gc:gc/allocationtick";

        protected override string PerformanceCounter => "Allocated Bytes/sec";

        protected override string PerformanceCategory => ".NET CLR Memory";

        protected override string PerformanceInstance => Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);

        protected override void OnEventOccurred(TraceEvent @event)
        {
            var allocTick = @event as GCAllocationTickTraceData;
            if (allocTick == null)
                return;

            _totalAllocs++;
            _totalAllocatedBytes += allocTick.AllocationAmount64;

            long typeAllocations;
            if (!_allocsPerType.TryGetValue(allocTick.TypeName, out typeAllocations))
                _allocsPerType.Add(allocTick.TypeName, 1);
            else
                _allocsPerType[allocTick.TypeName] = typeAllocations + 1;
        }

        protected override void OnIntensiveSamplingDone()
        {
            WriteTraceLine($"Total {_totalAllocs} allocations, {_totalAllocatedBytes} bytes, from the top 10 following types:");
            foreach (var typeAllocation in _allocsPerType.OrderByDescending(kvp => kvp.Value).Take(10).Reverse()) // Print most-allocated types last
                WriteTraceLine($"  {typeAllocation.Value} allocations from type {typeAllocation.Key}");

            PrintHeapBreakdown();

            _totalAllocs = 0;
            _totalAllocatedBytes = 0;
            _allocsPerType.Clear();
        }

        private void PrintHeapBreakdown()
        {
            using (var target = DataTarget.AttachToProcess(Process.GetCurrentProcess().Id, 1000, AttachFlag.Passive))
            {
                var runtime = target.ClrVersions[0].CreateRuntime();

                WriteTraceLine("Heap breakdown:");
                WriteTraceLine($"  Total heap size: {runtime.Heap.TotalHeapSize.ToMemoryUnits()}");
                WriteTraceLine($"  Gen 0: {runtime.Heap.GetSizeByGen(0).ToMemoryUnits()}  Gen 1: {runtime.Heap.GetSizeByGen(1).ToMemoryUnits()}" +
                               $"  Gen 2: {runtime.Heap.GetSizeByGen(2).ToMemoryUnits()}  LOH: {runtime.Heap.GetSizeByGen(3).ToMemoryUnits()}");

                WriteTraceLine("Top 10 heap types:");
                var query = from address in runtime.Heap.EnumerateObjectAddresses()
                            let type = runtime.Heap.GetObjectType(address)
                            where type != null && !type.IsFree
                            let size = type.GetSize(address)
                            group size by type.Name into g
                            let totalSize = g.Sum(s => (long)s)
                            orderby totalSize descending
                            select new { Type = g.Key, Size = totalSize };
                foreach (var typeSize in query.Take(10).Reverse()) // Print biggest types last
                    WriteTraceLine($"  {typeSize.Size,10} {typeSize.Type}");
            }
        }

        public static AllocMonitor Start()
        {
            return new AllocMonitor();
        }
    }

    public static class Extensions
    {
        public static string ToMemoryUnits(this ulong value)
        {
            if (value < 1024)
                return $"{value} bytes";

            if (value < 1024 * 1024)
                return $"{value/1024.0,3} KB";

            if (value < 1024 * 1024 * 1024)
                return $"{value/(1024.0*1024.0),3} MB";

            return $"{value/(1024.0*1024.0*1024.0),3} GB";
        }
    }
}
