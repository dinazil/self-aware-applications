﻿using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gatos.Monitor
{
    public class LeakMonitor : Monitor
    {
        private static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(5);
        private const int SnapshotCount = 3;
        private const int TopTypes = 20;

        protected override float CounterThreshold => 10 * 1024 * 1024; // 1024 * 1024 * 1024; // 1 GB

        protected override string PerformanceCounter => "# Bytes in all Heaps";

        protected override string PerformanceCategory => ".NET CLR Memory";

        protected override string PerformanceInstance => Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);

        protected override void OnIntensiveSamplingStart()
        {
            var snapshots = new List<HeapSnapshot>();
            for (int i = 0; i < SnapshotCount; ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                using (var target = DataTarget.AttachToProcess(Process.GetCurrentProcess().Id, 1000, AttachFlag.Passive))
                {
                    var runtime = target.ClrVersions[0].CreateRuntime();
                    snapshots.Add(new HeapSnapshot(runtime.Heap, TopTypes));
                    Thread.Sleep(SnapshotInterval);
                }
            }

            for (int i = 0; i < SnapshotCount; ++i)
            {
                for (int j = i + 1; j < SnapshotCount; ++j)
                {
                    var diff = HeapSnapshot.Diff(snapshots[i], snapshots[j]);
                    WriteTraceLine($"Diff between snapshot {i} (baseline) and snapshot {j}:");
                    foreach (var typeSize in diff.SizeByType.OrderBy(v => v.Value)) // Biggest offenders printed last
                    {
                        WriteTraceLine($"  {typeSize.Key,10} {typeSize.Value,8} {diff.CountByType[typeSize.Key],8}");
                    }
                }
            }
        }

        public static LeakMonitor Start()
        {
            return new LeakMonitor();
        }
    }

    internal class HeapSnapshot
    {
        public IDictionary<string, long> CountByType { get; } = new Dictionary<string, long>();
        public IDictionary<string, long> SizeByType { get; } = new Dictionary<string, long>();

        public HeapSnapshot(ClrHeap heap, int topTypes)
        {
            var query = from address in heap.EnumerateObjectAddresses()
                        let type = heap.GetObjectType(address)
                        where type != null && !type.IsFree
                        let size = type.GetSize(address)
                        group size by type.Name into g
                        let totalSize = g.Sum(v => (long)v)
                        let count = g.Count()
                        orderby totalSize descending
                        select new { Type = g.Key, Size = totalSize, Count = count };
            foreach (var typeStats in query.Take(topTypes))
            {
                SizeByType.Add(typeStats.Type, typeStats.Size);
                CountByType.Add(typeStats.Type, typeStats.Count);
            }
        }

        public static HeapSnapshot Diff(HeapSnapshot baseline, HeapSnapshot current)
        {
            var result = new HeapSnapshot();

            var baselineTypes = new HashSet<string>(baseline.SizeByType.Keys);
            var currentTypes = new HashSet<string>(current.SizeByType.Keys);
            baselineTypes.IntersectWith(currentTypes);
            foreach (var type in baselineTypes) // In both baseline and current
            {
                result.SizeByType.Add(type, current.SizeByType[type] - baseline.SizeByType[type]);
                result.CountByType.Add(type, current.CountByType[type] - baseline.CountByType[type]);
            }

            baselineTypes = new HashSet<string>(baseline.SizeByType.Keys);
            baselineTypes.ExceptWith(currentTypes);
            foreach (var type in baselineTypes) // Only in baseline
            {
                result.SizeByType.Add(type, -baseline.SizeByType[type]);
                result.CountByType.Add(type, -baseline.CountByType[type]);
            }

            baselineTypes = new HashSet<string>(baseline.SizeByType.Keys);
            currentTypes.ExceptWith(baselineTypes);
            foreach (var type in currentTypes) // Only in current
            {
                result.SizeByType.Add(type, +current.SizeByType[type]);
                result.CountByType.Add(type, +current.CountByType[type]);
            }

            return result;
        }

        private HeapSnapshot()
        {
        }
    }
}
