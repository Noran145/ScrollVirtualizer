using System;
using System.Collections.Generic;
using NoranDev.ScrollVirtualizer;

namespace ScrollVirtualizer.Samples
{
    public class EventsSampleScrollVirtualizer : HorizontalScrollVirtualizerWithContext<EventsSampleCell, EventsSampleData, EventsSampleContext>
    {
        private List<EventsSampleData> _items = new();
        private IAnalyticsService _analyticsService;

        public Action<string> OnItemClicked;

        protected override EventsSampleContext CreateContext()
        {
            _analyticsService = new AnalyticsService();
            
            return new EventsSampleContext(
                onItemClicked: ClickHandler,
                analyticsService: _analyticsService
            );
        }

        private void ClickHandler(string eventName)
        {
            OnItemClicked.Invoke(eventName);
        }

        public void Initialize(List<EventsSampleData> items)
        {
            _items = items;
            InitializeContents(_items);
        }

        public void UpdateList(List<EventsSampleData> items, bool resetScrollPosition = true)
        {
            _items = items;
            UpdateContents(_items, resetScrollPosition);
        }

        public void AddList(List<EventsSampleData> items, bool insertAtStart = false)
        {
            _items.AddRange(items);
            AddContents(items, insertAtStart);
        }
    }
}
