using System;

namespace ScrollVirtualizer.Samples
{
    public readonly struct EventsSampleContext
    {
        public readonly Action<string> OnItemClicked;
        public readonly IAnalyticsService AnalyticsService;

        public EventsSampleContext(Action<string> onItemClicked, IAnalyticsService analyticsService)
        {
            OnItemClicked = onItemClicked;
            AnalyticsService = analyticsService;
        }
    }
}
