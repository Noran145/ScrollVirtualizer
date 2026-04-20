using System.Collections.Generic;
using NoranDev.ScrollVirtualizer;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollVirtualizer.Samples
{
    public class EventsSampleView : MonoBehaviour
    {
        [SerializeField] private EventsSampleScrollVirtualizer eventsSampleScrollVirtualizer;
        [SerializeField] private Text mainDisplayText;
        [SerializeField] private Text subDisplayText;
        
        private void Start()
        {
            var list = new List<EventsSampleData>
            {
                new("Cell 0", new Color(0.100f, 1.000f, 0.330f, 1.000f)),
                new("Cell 1", new Color(0.100f, 1.000f, 0.400f, 1.000f)),
                new("Cell 2", new Color(0.100f, 1.000f, 0.492f, 1.000f)),
                new("Cell 3", new Color(0.100f, 1.000f, 0.583f, 1.000f)),
                new("Cell 4", new Color(0.100f, 1.000f, 0.675f, 1.000f)),
                new("Cell 5", new Color(0.100f, 1.000f, 0.767f, 1.000f)),
                new("Cell 6", new Color(0.100f, 1.000f, 0.858f, 1.000f)),
                new("Cell 7", new Color(0.100f, 1.000f, 0.950f, 1.000f)),
                new("Cell 8", new Color(0.100f, 0.958f, 1.000f, 1.000f)),
                new("Cell 9", new Color(0.100f, 0.866f, 1.000f, 1.000f)),
                new("Cell 10", new Color(0.100f, 0.775f, 1.000f, 1.000f)),
                new("Cell 11", new Color(0.100f, 0.683f, 1.000f, 1.000f)),
                new("Cell 12", new Color(0.100f, 0.592f, 1.000f, 1.000f)),
                new("Cell 13", new Color(0.100f, 0.500f, 1.000f, 1.000f)),
                new("Cell 14", new Color(0.100f, 0.408f, 1.000f, 1.000f)),
                new("Cell 15", new Color(0.100f, 0.317f, 1.000f, 1.000f)),
                new("Cell 16", new Color(0.100f, 0.225f, 1.000f, 1.000f)),
                new("Cell 17", new Color(0.133f, 0.100f, 1.000f, 1.000f)),
                new("Cell 18", new Color(0.225f, 0.100f, 1.000f, 1.000f)),
                new("Cell 19", new Color(0.317f, 0.100f, 1.000f, 1.000f)),
                new("Cell 20", new Color(0.342f, 0.100f, 1.000f, 1.000f)),
                new("Cell 21", new Color(0.433f, 0.100f, 1.000f, 1.000f)),
                new("Cell 22", new Color(0.525f, 0.100f, 1.000f, 1.000f)),
                new("Cell 23", new Color(0.617f, 0.100f, 1.000f, 1.000f)),
                new("Cell 24", new Color(0.708f, 0.100f, 1.000f, 1.000f)),
                new("Cell 25", new Color(0.800f, 0.100f, 1.000f, 1.000f)),
                new("Cell 26", new Color(0.892f, 0.100f, 1.000f, 1.000f)),
                new("Cell 27", new Color(0.983f, 0.100f, 1.000f, 1.000f)),
                new("Cell 28", new Color(1.000f, 0.100f, 0.958f, 1.000f)),
                new("Cell 29", new Color(1.000f, 0.100f, 0.866f, 1.000f)),
                new("Cell 30", new Color(1.000f, 0.100f, 0.775f, 1.000f)),
                new("Cell 31", new Color(1.000f, 0.100f, 0.683f, 1.000f)),
                new("Cell 32", new Color(1.000f, 0.100f, 0.592f, 1.000f)),
                new("Cell 33", new Color(1.000f, 0.100f, 0.500f, 1.000f)),
                new("Cell 34", new Color(1.000f, 0.100f, 0.408f, 1.000f)),
                new("Cell 35", new Color(1.000f, 0.100f, 0.317f, 1.000f)),
                new("Cell 36", new Color(1.000f, 0.100f, 0.225f, 1.000f)),
                new("Cell 37", new Color(1.000f, 0.133f, 0.100f, 1.000f)),
                new("Cell 38", new Color(1.000f, 0.225f, 0.100f, 1.000f)),
                new("Cell 39", new Color(1.000f, 0.317f, 0.100f, 1.000f)),
                new("Cell 40", new Color(1.000f, 0.408f, 0.100f, 1.000f)),
                new("Cell 41", new Color(1.000f, 0.500f, 0.100f, 1.000f)),
                new("Cell 42", new Color(1.000f, 0.592f, 0.100f, 1.000f)),
                new("Cell 43", new Color(1.000f, 0.683f, 0.100f, 1.000f)),
                new("Cell 44", new Color(1.000f, 0.775f, 0.100f, 1.000f)),
                new("Cell 45", new Color(1.000f, 0.866f, 0.100f, 1.000f)),
                new("Cell 46", new Color(0.958f, 1.000f, 0.100f, 1.000f)),
                new("Cell 47", new Color(0.866f, 1.000f, 0.100f, 1.000f)),
                new("Cell 48", new Color(0.775f, 1.000f, 0.100f, 1.000f)),
                new("Cell 49", new Color(0.708f, 1.000f, 0.100f, 1.000f)),
                new("Cell 50", new Color(0.617f, 1.000f, 0.100f, 1.000f)),
                new("Cell 51", new Color(0.617f, 1.000f, 0.100f, 1.000f)),
                new("Cell 52", new Color(0.525f, 1.000f, 0.100f, 1.000f)),
                new("Cell 53", new Color(0.433f, 1.000f, 0.100f, 1.000f)),
                new("Cell 54", new Color(0.342f, 1.000f, 0.100f, 1.000f)),
                new("Cell 55", new Color(0.250f, 1.000f, 0.100f, 1.000f))
            };
            
            eventsSampleScrollVirtualizer.Initialize(list);

            eventsSampleScrollVirtualizer.CellTouched += data =>
            {
                mainDisplayText.text = $"CellTouched: {data.CellText}";
            };
            
            eventsSampleScrollVirtualizer.ScrollCompleted += () =>
            {
                mainDisplayText.text = "ScrollCompleted";
            };
            
            eventsSampleScrollVirtualizer.ScrollPullReleased += direction =>
            {
                switch (direction)
                {
                    case PullDirection.Start:
                        subDisplayText.text = "ScrollPullReleased at Start";
                        break;
                    case PullDirection.End:
                        subDisplayText.text = "ScrollPullReleased at End";
                        break;
                }
            };
            
            eventsSampleScrollVirtualizer.ElasticPullStarted += direction =>
            {
                switch (direction)
                {
                    case PullDirection.Start:
                        mainDisplayText.text = "ElasticPullStarted at Start";
                        break;
                    case PullDirection.End:
                        mainDisplayText.text = "ElasticPullStarted at End";
                        break;
                }
            };
            
            eventsSampleScrollVirtualizer.ElasticPullReleased += direction =>
            {
                switch (direction)
                {
                    case PullDirection.Start:
                        mainDisplayText.text = "ElasticPullReleased at Start";
                        break;
                    case PullDirection.End:
                        mainDisplayText.text = "ElasticPullReleased at End";
                        break;
                }
            };
            
            eventsSampleScrollVirtualizer.PullThresholdExceeded += direction =>
            {
                switch (direction)
                {
                    case PullDirection.Start:
                        mainDisplayText.text = "PullThresholdExceeded at Start";
                        break;
                    case PullDirection.End:
                        mainDisplayText.text = "PullThresholdExceeded at End";
                        break;
                }
            };

            eventsSampleScrollVirtualizer.CellVisibilityChanged += (index, cell, state) =>
            {
                switch (state)
                {
                    case CellVisibilityState.Visible:
                        mainDisplayText.text = "CellVisibilityChanged: Visible Index=" + index;
                        break;
                    case CellVisibilityState.Invisible:
                        subDisplayText.text = "CellVisibilityChanged: Invisible Index=" + index;
                        break;
                }
            };

            eventsSampleScrollVirtualizer.OnItemClicked += eventName =>
            {
                subDisplayText.text = $"OnItemClicked: {eventName}";
            };
        }
    }
}
