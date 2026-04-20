using System.Threading;
using Cysharp.Threading.Tasks;
using NoranDev.ScrollVirtualizer;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollVirtualizer.Samples
{
    public class EventsSampleCell : ScrollVirtualizerCellWithContext<EventsSampleData, EventsSampleContext>
    {
        [SerializeField] private Image cellImage;
        [SerializeField] private Text cellText;
        [SerializeField] private Button myButton;
        
        private EventsSampleData _data;

        public override void Initialize(EventsSampleContext context)
        {
            myButton.onClick.AddListener(() =>
            {
                // Track analytics
                context.AnalyticsService?.TrackEvent("CellClick", Index);

                // Notify ScrollVirtualizer
                context.OnItemClicked?.Invoke($"Cell {Index} clicked");
            });
        }

        public override void UpdateCell(EventsSampleData data)
        {
            _data = data;

            cellText.text = _data.CellText;
            cellImage.color = _data.CellColor;
        }

        public override async UniTask UpdateCellAsync(EventsSampleData data, CancellationToken ct)
        {
            _data = data;

            cellText.text = _data.CellText;
            cellImage.color = _data.CellColor;
        }
    }
}
