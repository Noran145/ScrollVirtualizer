using UnityEngine;

namespace ScrollVirtualizer.Samples
{
    public readonly struct EventsSampleData
    {
        public readonly string CellText;
        public readonly Color CellColor;

        public EventsSampleData(string cellText, Color cellColor)
        {
            CellText = cellText;
            CellColor = cellColor;
        }
    }
}
