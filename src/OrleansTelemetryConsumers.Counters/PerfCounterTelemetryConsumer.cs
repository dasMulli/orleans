﻿using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OrleansTelemetryConsumers.Counters
{
    public class PerfCounterTelemetryConsumer : IMetricTelemetryConsumer
    {
        internal const string CATEGORY_NAME = "OrleansRuntime";
        internal const string CATEGORY_DESCRIPTION = "Orleans Runtime Counters";
        private const string CounterControlProgName = "OrleansCounterControl.exe";
        private const string ExplainHowToCreateOrleansPerfCounters = "Run " + CounterControlProgName + " as Administrator to create perf counters for Orleans.";

        private static readonly Logger logger = LogManager.GetLogger("OrleansPerfCounterManager", LoggerType.Runtime);
        private readonly List<PerfCounterConfigData> perfCounterData = new List<PerfCounterConfigData>();

        public PerfCounterTelemetryConsumer(bool installMode = false)
        {
            if (!AreWindowsPerfCountersAvailable())
            {
                logger.Warn(ErrorCode.PerfCounterNotFound, "Windows perf counters not found -- defaulting to in-memory counters. " + ExplainHowToCreateOrleansPerfCounters);
                return;
            }

            GetCounterData(installMode);

            foreach (var cd in perfCounterData)
            {
                var perfCounterName = GetPerfCounterName(cd);
                cd.PerfCounter = CreatePerfCounter(perfCounterName);
            }
        }

        #region Counter Management methods

        private void GetCounterData(bool installMode)
        {
            perfCounterData.Clear();

            // (1) Start with list of static counters
            perfCounterData.AddRange(PerfCounterConfigData.StaticPerfCounters);

            if (!installMode)
            {
                // (2) Then search for grain DLLs and pre-create activation counters for any grain types found
                var loadedGrainClasses = GrainTypeManager.Instance.GrainClassTypeData;
                foreach (var grainClass in loadedGrainClasses)
                {
                    var counterName = new StatisticName(StatisticNames.GRAIN_COUNTS_PER_GRAIN, grainClass.Key);
                    perfCounterData.Add(new PerfCounterConfigData
                    {
                        Name = counterName,
                        UseDeltaValue = false
                    });
                }
            }
        }

        public static bool AreWindowsPerfCountersAvailable()
        {
            try
            {
                if (Environment.OSVersion.ToString().StartsWith("unix", StringComparison.InvariantCultureIgnoreCase))
                {
                    logger.Warn(ErrorCode.PerfCounterNotFound, "Windows perf counters are only available on Windows :) -- defaulting to in-memory counters.");
                    return false;
                }

                return PerformanceCounterCategory.Exists(CATEGORY_NAME);
            }
            catch (Exception exc)
            {
                logger.Warn(ErrorCode.PerfCounterCategoryCheckError,
                    string.Format("Ignoring error checking for {0} perf counter category", CATEGORY_NAME), exc);
            }
            return false;
        }

        private PerformanceCounter CreatePerfCounter(string perfCounterName)
        {
            logger.Verbose(ErrorCode.PerfCounterRegistering, "Creating perf counter {0}", perfCounterName);
            return new PerformanceCounter(CATEGORY_NAME, perfCounterName, false);
        }

        private string GetPerfCounterName(PerfCounterConfigData cd)
        {
            return cd.Name.Name + "." + (cd.UseDeltaValue ? "Delta" : "Current");
        }

        internal CounterCreationData[] GetCounterCreationData()
        {
            GetCounterData(false);
            var ctrCreationData = new List<CounterCreationData>();
            foreach (PerfCounterConfigData cd in perfCounterData)
            {
                var perfCounterName = GetPerfCounterName(cd);
                var description = cd.Name.Name;

                var msg = string.Format("Registering perf counter {0}", perfCounterName);
                Console.WriteLine(msg);

                ctrCreationData.Add(new CounterCreationData(perfCounterName, description, PerformanceCounterType.NumberOfItems32));
            }
            return ctrCreationData.ToArray();
        }

        /// <summary>
        /// Register Orleans perf counters with Windows
        /// </summary>
        /// <remarks>Note: Program needs to be running as Administrator to be able to delete Windows perf counters.</remarks>
        public void InstallCounters()
        {
            var collection = new CounterCreationDataCollection();
            collection.AddRange(GetCounterCreationData());

            PerformanceCounterCategory.Create(
                CATEGORY_NAME,
                CATEGORY_DESCRIPTION,
                PerformanceCounterCategoryType.SingleInstance,
                collection);
        }

        /// <summary>
        /// Delete any existing perf counters registered with Windows
        /// </summary>
        /// <remarks>Note: Program needs to be running as Administrator to be able to delete Windows perf counters.</remarks>
        public void DeleteCounters()
        {
            PerformanceCounterCategory.Delete(CATEGORY_NAME);
        }

        private PerfCounterConfigData GetCounter(string counterName)
        {
            return perfCounterData.Where(pcd => GetPerfCounterName(pcd) == counterName).SingleOrDefault();
        }

        #endregion

        #region IMetricTelemetryConsumer Methods

        public void IncrementMetric(string name) => WriteMetric(name, UpdateMode.Increment);

        public void IncrementMetric(string name, double value) => WriteMetric(name, UpdateMode.Increment, value);

        public void TrackMetric(string name, double value, IDictionary<string, string> properties = null) => WriteMetric(name, UpdateMode.Set, value);

        public void TrackMetric(string name, TimeSpan value, IDictionary<string, string> properties = null) => WriteMetric(name, UpdateMode.Set, value.Ticks);

        public void DecrementMetric(string name) => WriteMetric(name, UpdateMode.Decrement);

        public void DecrementMetric(string name, double value) => WriteMetric(name, UpdateMode.Decrement, value);

        public void Flush() { }

        public void Close() { }

        private void WriteMetric(string name, UpdateMode mode = UpdateMode.Increment, double? value = null)
        {
            PerfCounterConfigData cd = GetCounter(name);
            if (cd == null)
            {
                if (logger.IsVerbose) logger.Verbose(ErrorCode.PerfCounterNotFound, "No perf counter found for {0}", name);
                return;
            }

            StatisticName statsName = cd.Name;
            string perfCounterName = GetPerfCounterName(cd);

            try
            {
                if (cd.PerfCounter == null)
                {
                    if (logger.IsVerbose) logger.Verbose(ErrorCode.PerfCounterUnableToConnect, "No perf counter found for {0}", name);
                    cd.PerfCounter = CreatePerfCounter(perfCounterName);
                }
                
                if (logger.IsVerbose3) logger.Verbose3(ErrorCode.PerfCounterWriting, "Writing perf counter {0}", perfCounterName);

                switch (mode)
                {
                    case UpdateMode.Increment:
                        if (value.HasValue)
                        {
                            cd.PerfCounter.IncrementBy((long)value.Value);
                        }
                        else
                        {
                            cd.PerfCounter.Increment();
                        }
                        break;
                    case UpdateMode.Decrement:
                        if (value.HasValue)
                        {
                            cd.PerfCounter.RawValue = cd.PerfCounter.RawValue - (long)value.Value;
                        }
                        else
                        {
                            cd.PerfCounter.Decrement();
                        }
                        break;
                    case UpdateMode.Set:
                        cd.PerfCounter.RawValue = (long)value.Value;
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ErrorCode.PerfCounterUnableToWrite, string.Format("Unable to write to Windows perf counter '{0}'", statsName), ex);
            }
        }

        private enum UpdateMode
        {
            Increment = 0,
            Decrement,
            Set
        }

        #endregion
    }
}
