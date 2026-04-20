using System.Collections.Generic;
using NoranDev.ScrollVirtualizer;

namespace ScrollVirtualizer.Samples
{
    public class BasicSampleScrollVirtualizer : VerticalScrollVirtualizer<BasicSampleCell, BasicSampleData>
    {
        private List<BasicSampleData> _items = new();
        
        public void Initialize(List<BasicSampleData> items)
        {
            _items = items;
            InitializeContents(_items);
        }

        public void UpdateList(List<BasicSampleData> items, bool resetScrollPosition = true)
        {
            _items = items;
            UpdateContents(_items, resetScrollPosition);
        }

        public void AddList(List<BasicSampleData> items, bool insertAtStart = false)
        {
            _items.AddRange(items);
            AddContents(items, insertAtStart);
        }
    }
}
