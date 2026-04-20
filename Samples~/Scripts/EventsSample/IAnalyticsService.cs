using System.Collections.Generic;
using UnityEngine;

namespace ScrollVirtualizer.Samples
{
    /// <summary>
    /// Analytics service for tracking user interactions
    /// </summary>
    public interface IAnalyticsService
    {
        void TrackEvent(string eventName, int cellIndex);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly Dictionary<string, int> _eventCounts = new();

        public void TrackEvent(string eventName, int cellIndex)
        {
            var key = $"{eventName}_{cellIndex}";

            _eventCounts.TryAdd(key, 0);
            _eventCounts[key]++;

            Debug.Log($"[ScrollVirtualizer] {eventName} at Cell {cellIndex} (Count: {_eventCounts[key]})");
        }
    }
}
