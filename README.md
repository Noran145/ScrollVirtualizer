# ScrollVirtualizer

A virtualized scroll library that extends Unity's ScrollRect.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Features

- 🎯 **Template Auto-Generation** - Automatically generate necessary files with Editor extensions
- 🎯 **Simple API** - Easy to use with 3 main methods (initialize, update, add) plus navigation methods
- ⚡ **High Performance** - Smooth operation even with large datasets through cell recycling
- 🔧 **Context Support** - Pass external services and dependencies to cells
- 📐 **Dynamic Cell Size** - Automatic size adjustment based on Viewport
- 📐 **3 Layouts** - Vertical Scroll / Horizontal Scroll / Grid
- 🎨 **Rich Events** - Pull gestures, scroll completion, cell visibility changes, etc.

### Feature List

⚪︎ Supported | - Not supported

| Feature | Support |
|---------|:-------:|
| Template Auto-Generation | ⚪︎ |
| Async Cell Updates (Task/async-await) | ⚪︎ |
| UniTask Support (Optional) | ⚪︎ |
| Context Feature (Pass External Dependencies) | ⚪︎ |
| Dynamic Cell Size (Viewport-based) | ⚪︎ |
| Cell Recycling (Virtualization) | ⚪︎ |
| 3 Layouts (Vertical/Horizontal/Grid) | ⚪︎ |
| Detailed Pull Gesture Events | ⚪︎ |
| Infinite Loop Scroll | - |
| Custom Scroll Animations | - |
| 3D Effects | - |
| Snap Feature | - |

## Installation

### Via Unity Package Manager

1. Open your Unity project
2. Go to `Window` > `Package Manager`
3. Click `+` > `Add package from git URL...`
4. Enter the following URL:
   ```
   https://github.com/Noran145/ScrollVirtualizer.git
   ```
5. Click `Add`

To specify a particular version, use a tagged URL:

```
https://github.com/Noran145/ScrollVirtualizer.git#v1.0.0
```

## Requirements

- Unity: 6000.0+ (Unity 6)

**Tested:**
- Unity: 6000.0.58f2

### UniTask Support (Optional)

ScrollVirtualizer supports `Task`-based asynchronous processing by default, but also supports [UniTask](https://github.com/Cysharp/UniTask).

**UniTask is not required.** If UniTask is installed in your project, the template generator will create UniTask-based code. If UniTask is not present, standard `Task`-based code will be generated.

## Getting Started

### File Generation

1. Right-click in Project window → `Create` → `ScrollVirtualizer` → `Create ScrollVirtualizer Files...`
2. A window will open, configure the following:
   - **Class Name**: Class name (without suffix, e.g., `MyScroll`)
   - **Namespace**: Namespace (optional)
   - **ScrollVirtualizer Type**: `Vertical` / `Horizontal` / `Grid`
   - **Data Type**: `Class` / `Struct`
   - **Use Context**: Check if using Context
3. Click `Create`

Generated files:
- `{ClassName}Data.cs` - Data class
- `{ClassName}Cell.cs` - Cell class
- `{ClassName}Context.cs` - Context class (only when Use Context is enabled)
- `{ClassName}ScrollVirtualizer.cs` - ScrollVirtualizer class

### Basic Usage

**Data** - Represents the data you want to display in each cell. This is the information that will be passed to and rendered by the cells.

**Cell** - The UI component that is actually instantiated and displayed. Attach this to your cell prefab and register it in the ScrollVirtualizer's SerializeField.

**Context** (Optional) - Acts as a bridge between Cell and ScrollVirtualizer. It handles communication from Cell to ScrollVirtualizer (e.g., cell events) and passes external service instances from ScrollVirtualizer to Cell.

**ScrollVirtualizer** - The main component that manages the scroll list:
- `InitializeContents`: Call this to create the initial scroll list. This is all you need for basic setup.
- `UpdateContents`: Use this when you want to update the existing list with new data.
- `AddContents`: Use this when you want to add more items to the existing list.

```csharp
// InitializeContents - Create the scroll list
scroller.InitializeContents(itemList);

// UpdateContents - Update the list (reset scroll position)
scroller.UpdateContents(newItemList);

// UpdateContents - Update the list (maintain scroll position)
scroller.UpdateContents(newItemList, resetScrollPosition: false);

// UpdateContents - Update the list without refreshing visible cells
scroller.UpdateContents(newItemList, refreshVisibleCells: false);

// AddContents - Add items (append to end)
scroller.AddContents(additionalItems);

// AddContents - Add items (insert at start)
scroller.AddContents(additionalItems, insertAtStart: true);
```

### For Context Version

When using the Context version, override `CreateContext()` to provide your Context instance:

```csharp
// Override CreateContext() to return Context
protected override MyContext CreateContext()
{
    return new MyContext();
}
```

In the Context version, the Cell's `Initialize` method receives the Context, allowing you to set up event handlers, access shared resources, or establish communication back to the ScrollVirtualizer. Data is provided separately through `UpdateCell` and `UpdateCellAsync` methods.

### Setup Notes

- To enable scrolling by dragging over spacing areas (not just cells), attach an `Image` component to the **Viewport** GameObject with `Raycast Target` enabled and `Color` alpha set to 0.

## Examples

Here are complete examples generated from the template. This demonstrates a vertical scroll list with Context support.

**Note:**
- These examples demonstrate UniTask usage. (When you generate files using the template, `Task`-based code will be created if UniTask is not installed.)
- This is the Context-enabled version. For simpler use cases without Context, you can use the non-Context version (`ScrollVirtualizerCell<TData>` instead of `ScrollVirtualizerCellWithContext<TData, TContext>`).
- These examples use TextMeshPro (Unity's recommended text solution). You can use legacy uGUI Text if needed.

### ExampleData.cs

```csharp
namespace YourNamespace
{
    public readonly struct ExampleData
    {
        public readonly string Title;
        public readonly string Description;
        public readonly int Value;
        public readonly string ImageUrl;

        public ExampleData(string title, string description, int value, string imageUrl)
        {
            Title = title;
            Description = description;
            Value = value;
            ImageUrl = imageUrl;
        }
    }
}
```

### ExampleCell.cs

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NoranDev.ScrollVirtualizer;

namespace YourNamespace
{
    public class ExampleCell : ScrollVirtualizerCellWithContext<ExampleData, ExampleContext>
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Button actionButton;

        private ExampleData _data;

        public override void Initialize(ExampleContext context)
        {
            // Set up button click event to notify ScrollVirtualizer via Context
            actionButton.onClick.AddListener(() =>
            {
                // Communicate from Cell to ScrollVirtualizer
                context.OnCellButtonClicked?.Invoke(_data);

                // Use instance provided from ScrollVirtualizer (e.g., play sound)
                context.AudioManager?.PlaySound("Click");
            });
        }

        // Called when Cell Update Mode is SyncOnly or Both
        // Use SyncOnly when you don't need async operations
        // Use Both when you need immediate UI updates followed by async loading
        public override void UpdateCell(ExampleData data)
        {
            _data = data;

            // Set text synchronously (no async needed)
            titleText.text = data.Title;
            descriptionText.text = data.Description;

            // Clear image if no URL provided
            if (string.IsNullOrEmpty(data.ImageUrl))
            {
                thumbnailImage.sprite = null;
            }
        }

        // Called when Cell Update Mode is AsyncOnly or Both
        // Use AsyncOnly (default) when you need async operations like image loading
        public override async UniTask UpdateCellAsync(ExampleData data, CancellationToken ct)
        {
            _data = data;

            titleText.text = data.Title;
            descriptionText.text = data.Description;

            // Download and display image asynchronously
            if (!string.IsNullOrEmpty(data.ImageUrl))
            {
                if (ct.IsCancellationRequested) return;

                var sprite = await Context.ImageLoader.LoadImageAsync(data.ImageUrl, ct);

                if (sprite != null && !ct.IsCancellationRequested)
                {
                    thumbnailImage.sprite = sprite;
                }
            }
            else
            {
                thumbnailImage.sprite = null;
            }
        }
    }
}
```

### ExampleContext.cs

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YourNamespace
{
    public readonly struct ExampleContext
    {
        // Event for Cell to communicate with ScrollVirtualizer
        public readonly Action<ExampleData> OnCellButtonClicked;

        // Shared instances passed from ScrollVirtualizer to Cell
        public readonly IAudioManager AudioManager;
        public readonly IImageLoader ImageLoader;

        public ExampleContext(Action<ExampleData> onCellButtonClicked, IAudioManager audioManager, IImageLoader imageLoader)
        {
            OnCellButtonClicked = onCellButtonClicked;
            AudioManager = audioManager;
            ImageLoader = imageLoader;
        }
    }

    // Example service interfaces
    public interface IAudioManager
    {
        void PlaySound(string soundName);
    }

    public interface IImageLoader
    {
        UniTask<Sprite> LoadImageAsync(string url, CancellationToken ct);
    }
}
```

### ExampleScrollVirtualizer.cs

```csharp
using System.Collections.Generic;
using UnityEngine;
using NoranDev.ScrollVirtualizer;

namespace YourNamespace
{
    public class ExampleScrollVirtualizer : VerticalScrollVirtualizerWithContext<ExampleCell, ExampleData, ExampleContext>
    {
        [SerializeField] private AudioManager audioManager; // Your AudioManager component
        [SerializeField] private ImageLoader imageLoader; // Your ImageLoader component

        protected override ExampleContext CreateContext()
        {
            return new ExampleContext(
                onCellButtonClicked: OnCellButtonClicked,
                audioManager: audioManager,
                imageLoader: imageLoader
            );
        }

        private void OnCellButtonClicked(ExampleData data)
        {
            Debug.Log($"Cell button clicked: {data.Title}");
            // Handle cell events here
        }

        // Example: Initialize with sample data
        private void Start()
        {
            var items = new List<ExampleData>
            {
                new ExampleData("Item 1", "First item", 1, "https://example.com/image1.png"),
                new ExampleData("Item 2", "Second item", 2, "https://example.com/image2.png"),
                new ExampleData("Item 3", "Third item", 3, "https://example.com/image3.png"),
            };

            InitializeContents(items);
        }
    }
}
```

## API Reference

### ScrollVirtualizer Methods

| API Name | Parameters | Description |
|----------|------------|-------------|
| `InitializeContents` | `IReadOnlyList<TData> items` | Initialize the data list. Use when setting data for the first time. |
| `UpdateContents` | `IReadOnlyList<TData> items`, `bool resetScrollPosition = true`, `bool refreshVisibleCells = true` | Update the data list. If resetScrollPosition is true, reset scroll position to 0; if false, maintain current position. If refreshVisibleCells is true, refresh currently visible cells with the new data. |
| `AddContents` | `IReadOnlyList<TData> items`, `bool insertAtStart = false`, `Action onComplete = null` | Add items. If insertAtStart is true, insert at the beginning; if false, append at the end. onComplete is a callback on completion. |
| `ClearContents` | - | Clear all data and reset state. Releases object references. |
| `JumpTo` | `int index = 0` | Jump immediately to the specified index. |
| `ScrollToIndex` | `int index` | Scroll immediately to the specified index. |
| `ScrollTo` | `int index`, `float duration`, `Ease ease`, `Action onComplete = null` | Scroll to the specified index with animation. `Ease` is a ScrollVirtualizer enum (`NoranDev.ScrollVirtualizer.Ease`) with options like Linear, InQuad, OutQuad, InOutQuad, etc. |

### ScrollVirtualizer Properties (Protected)

The following properties are available in subclasses:

| Property | Type | Description |
|----------|------|-------------|
| `ScrollPosition` | `float` | Current scroll position in pixels. |
| `MaxScrollPosition` | `float` | Maximum scroll position in pixels. |
| `SetScrollPosition` | `float position` | Set the scroll position directly in pixels. The value is clamped between 0 and `MaxScrollPosition`. |

### ScrollVirtualizerCell Methods

Both Context and non-Context versions use the same method names (`UpdateCell` and `UpdateCellAsync`). The Context version additionally provides an `Initialize` method.

**Non-Context version** (`ScrollVirtualizerCell<TData>`):

| API Name | Parameters | Description |
|----------|------------|-------------|
| `UpdateCell` | `TData data` | Update cell content synchronously. Called when Cell Update Mode is `SyncOnly` or `Both`. |
| `UpdateCellAsync` | `TData data`, `CancellationToken ct` | Update cell content asynchronously. Called when Cell Update Mode is `AsyncOnly` (default) or `Both`. |

**Context version** (`ScrollVirtualizerCellWithContext<TData, TContext>`):

| API Name | Parameters | Description |
|----------|------------|-------------|
| `Initialize` | `TContext context` | Initialize the cell. Called only on first display. |
| `UpdateCell` | `TData data` | Update cell content synchronously. Called when Cell Update Mode is `SyncOnly` or `Both`. |
| `UpdateCellAsync` | `TData data`, `CancellationToken ct` | Update cell content asynchronously. Called when Cell Update Mode is `AsyncOnly` (default) or `Both`. |

**Cell Update Mode** (configurable in Inspector under Common Settings):
- **SyncOnly**: Calls only `UpdateCell`. Use when you don't need async operations.
- **AsyncOnly** (Default): Calls only `UpdateCellAsync`. Use when you need async operations like image loading.
- **Both**: Calls `UpdateCell` first, then `UpdateCellAsync`. Use when you need immediate UI updates followed by async data loading.

**Note**: Using the Editor extension's template feature, files with the above methods already implemented will be automatically generated.

### Events

| API Name | Type | Description |
|----------|------|-------------|
| `CellButtonClicked` | `Action<TData>` | Fired when the button assigned to the cell's default `button` field is clicked. Argument: data of the clicked cell. |
| `CellTouched` | `Action<TData>` | Fired when a cell is touched. If a button is assigned, this event will not fire and `CellButtonClicked` will fire instead. Argument: data of the touched cell. |
| `ScrollCompleted` | `Action` | Fired when scroll animation completes. Only triggered by `ScrollTo`. Not fired by `ScrollToIndex` or `JumpTo`. |
| `ScrollPullReleased` | `Action<PullDirection>` | Fired when pulled and released (after threshold exceeded). Argument: PullDirection.Start (top/left) or PullDirection.End (bottom/right). |
| `ElasticPullStarted` | `Action<PullDirection>` | Fired when elastic pull starts (drag begins at edge). Argument: PullDirection.Start (top/left) or PullDirection.End (bottom/right). |
| `ElasticPullReleased` | `Action<PullDirection>` | Fired when elastic pull is released (content returns to edge). Argument: PullDirection.Start (top/left) or PullDirection.End (bottom/right). |
| `PullThresholdExceeded` | `Action<PullDirection>` | Fired when pull amount exceeds threshold (during drag, before release). Argument: PullDirection.Start (top/left) or PullDirection.End (bottom/right). |
| `CellVisibilityChanged` | `Action<int, TCell, CellVisibilityState>` | Fired when cell visibility state changes. Arg1: cell index, Arg2: cell instance, Arg3: CellVisibilityState.Visible (on screen) or CellVisibilityState.Invisible (off screen). |
