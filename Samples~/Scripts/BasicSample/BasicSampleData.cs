using UnityEngine;

namespace ScrollVirtualizer.Samples
{
    public readonly struct BasicSampleData
    {
        public readonly string CellText;
        public readonly Color CellColor;

        public BasicSampleData(string cellText, Color cellColor)
        {
            CellText = cellText;
            CellColor = cellColor;
        }
    }
}
