﻿using HouseofCat.Utilities.Errors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace HouseofCat.Metrics;

public class OpenTelemetryMetricsProvider : IMetricsProvider, IDisposable
{
    private readonly IMeterFactory _factory;
    private readonly Meter _meter;
    private readonly string _meterName;
    private readonly ActivitySource _activitySource;

    public ConcurrentDictionary<string, object> Counters { get; } = new ConcurrentDictionary<string, object>();
    public ConcurrentDictionary<string, object> Gauges { get; } = new ConcurrentDictionary<string, object>();
    public ConcurrentDictionary<string, object> Histograms { get; } = new ConcurrentDictionary<string, object>();

    private bool _disposedValue;

    public OpenTelemetryMetricsProvider(IMeterFactory meterFactory, string meterName, string activitySourceName = null, string activityVersion = null)
    {
        Guard.AgainstNull(meterFactory, nameof(meterFactory));
        Guard.AgainstNullOrEmpty(meterName, nameof(meterName));

        _factory = meterFactory;
        _meterName = meterName;

        _meter = _factory.Create(_meterName);
        _activitySource = new ActivitySource(activitySourceName ?? "HouseofCat.Metrics", activityVersion);
    }

    public Counter<T> GetOrAddCounter<T>(string name, string unit = null, string description = null) where T : struct
    {
        return (Counter<T>)Counters.GetOrAdd(name, _meter.CreateCounter<T>(name, unit, description));
    }

    public ObservableGauge<T> GetOrAddGauge<T>(string name, Func<T> observableValue, string unit = null, string description = null) where T : struct
    {
        return (ObservableGauge<T>)Gauges.GetOrAdd(
            name,
            _meter.CreateObservableGauge(name, observableValue, unit, description));
    }

    public Histogram<T> GetOrAddHistogram<T>(string name, string unit = null, string description = null) where T : struct
    {
        return (Histogram<T>)Histograms.GetOrAdd(name, _meter.CreateHistogram<T>(name, unit, description));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _meter.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #region IMetricsProvider Implementation

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ObserveValueFluctuation(string name, double value, string unit = null, string description = null)
    {
        Guard.AgainstNull(name, nameof(name));
        name = $"{name}_Histogram";
        GetOrAddHistogram<double>(name, unit: unit, description: description)
            .Record(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ObserveValue(string name, double value, string unit = null, string description = null)
    {
        Guard.AgainstNull(name, nameof(name));
        name = $"{name}_Summary";
        GetOrAddHistogram<double>(name, unit: unit, description: description)
            .Record(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementGauge(string name, string unit = null, string description = null)
    {
        Guard.AgainstNull(name, nameof(name));
        name = $"{name}_Gauge";
        GetOrAddCounter<int>(name, unit: unit, description: description)
            .Add(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DecrementGauge(string name, string unit = null, string description = null)
    {
        Guard.AgainstNull(name, nameof(name));
        name = $"{name}_Gauge";
        GetOrAddCounter<int>(name, unit: unit, description: description)
            .Add(-1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementCounter(string name, string unit = null, string description = null)
    {
        Guard.AgainstNull(name, nameof(name));
        name = $"{name}_Counter";
        GetOrAddCounter<int>(name, unit: unit, description: description)
            .Add(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DecrementCounter(string name, string unit = null, string description = null)
    {
        Guard.AgainstNull(name, nameof(name));
        name = $"{name}_Counter";
        GetOrAddCounter<int>(name, unit: unit, description: description)
            .Add(-1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable Duration(string name, bool microScale = false, string unit = null, string description = null)
    {
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable Track(string name, string unit = null, string description = null)
    {
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable TrackAndDuration(
        string name,
        bool microScale = false,
        string unit = null,
        string description = null,
        IDictionary<string, string> tags = null)
    {
        return GetActivity(name, tags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable Trace(
        string name,
        IDictionary<string, string> metricTags = null)
    {
        return GetActivity(name, metricTags);
    }

    private Activity GetActivity(string name, IDictionary<string, string> metricTags)
    {
        var activity = _activitySource.StartActivity(name);
        if (activity is not null
            && metricTags is not null
            && activity.IsAllDataRequested)
        {
            foreach (var tag in metricTags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }

    #endregion
}
