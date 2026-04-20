using System.Threading;
using Cysharp.Threading.Tasks;
using NoranDev.ScrollVirtualizer;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollVirtualizer.Samples
{
    public class BasicSampleCell : ScrollVirtualizerCell<BasicSampleData>
    {
        [SerializeField] private Image cellImage;
        [SerializeField] private Text cellText;
        
        private BasicSampleData _data;

        public override void UpdateCell(BasicSampleData data)
        {
            _data = data;

            cellText.text = _data.CellText;
            cellImage.color = _data.CellColor;
        }

        public override async UniTask UpdateCellAsync(BasicSampleData data, CancellationToken ct)
        {
            _data = data;

            cellText.text = _data.CellText;
            cellImage.color = _data.CellColor;
        }
    }
}
