using UnityEngine;
using UnityEditor;
using System.IO;

namespace NoranDev.ScrollVirtualizer.Editor
{
    public class ScrollVirtualizerCreatorWindow : EditorWindow
    {
        private string _className = "MyScroll";
        private string _namespace = "";
        private ScrollVirtualizerType _virtualizerType = ScrollVirtualizerType.Vertical;
        private DataType _dataType = DataType.Class;
        private bool _useContext = false;
        private DataType _contextType = DataType.Class;
        private string _targetPath = "Assets";
        private Vector2 _scrollPosition;

        private static bool HasUniTask => System.Type.GetType("Cysharp.Threading.Tasks.UniTask, UniTask") != null;

        private enum ScrollVirtualizerType
        {
            Vertical,
            Horizontal,
            Grid
        }

        private enum DataType
        {
            Class,
            Struct
        }

        /// <summary>
        /// Shows the ScrollVirtualizer creator window with the specified target path.
        /// </summary>
        public static void ShowWindow(string selectedPath)
        {
            var window = GetWindow<ScrollVirtualizerCreatorWindow>("Create ScrollVirtualizer");
            window._targetPath = selectedPath;
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Create ScrollVirtualizer Files", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var hasUniTask = HasUniTask;
            var message = hasUniTask
                ? "UniTask detected"
                : "UniTask not found - Task will be used";
            var messageType = hasUniTask ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(message, messageType);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Class Name (without suffix)", EditorStyles.label);
            _className = EditorGUILayout.TextField(_className);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Namespace (optional)", EditorStyles.label);
            _namespace = EditorGUILayout.TextField(_namespace);

            var fileCount = _useContext ? 4 : 3;
            EditorGUILayout.HelpBox($"{fileCount} files will be generated: {_className}Data, {_className}Cell" +
                                   (_useContext ? $", {_className}Context" : "") +
                                   $", {_className}ScrollVirtualizer", MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("ScrollVirtualizer Type", EditorStyles.label);
            _virtualizerType = (ScrollVirtualizerType)EditorGUILayout.EnumPopup(_virtualizerType);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Data Type", EditorStyles.label);
            var previousDataType = _dataType;
            _dataType = (DataType)EditorGUILayout.EnumPopup(_dataType);

            if (previousDataType != _dataType)
            {
                _contextType = _dataType;
            }

            EditorGUILayout.Space(10);

            _useContext = EditorGUILayout.Toggle("Use Context", _useContext);
            if (_useContext)
            {
                EditorGUILayout.HelpBox("For DI and cell-to-virtualizer communication", MessageType.Info);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Context Type", EditorStyles.label);
                _contextType = (DataType)EditorGUILayout.EnumPopup(_contextType);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Target Path:", EditorStyles.label);
            EditorGUILayout.LabelField(_targetPath, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                Close();
            }

            GUI.enabled = !string.IsNullOrWhiteSpace(_className);
            if (GUILayout.Button("Create", GUILayout.Width(100)))
            {
                CreateFiles();
                Close();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Creates all necessary files for the ScrollVirtualizer.
        /// </summary>
        private void CreateFiles()
        {
            if (string.IsNullOrWhiteSpace(_className))
            {
                EditorUtility.DisplayDialog("Error", "Class name cannot be empty.", "OK");
                return;
            }

            CreateDataFile();
            CreateCellFile();
            if (_useContext)
            {
                CreateContextFile();
            }
            CreateScrollVirtualizerFile();

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "ScrollVirtualizer files created successfully!", "OK");
        }

        /// <summary>
        /// Creates the Data file for the ScrollVirtualizer.
        /// </summary>
        private void CreateDataFile()
        {
            var fileName = $"{_className}Data.cs";
            var filePath = Path.Combine(_targetPath, fileName);

            var typeKeyword = _dataType == DataType.Class ? "class" : "readonly struct";

            var content = string.IsNullOrWhiteSpace(_namespace)
                ? $@"public {typeKeyword} {_className}Data
{{
}}
"
                : $@"namespace {_namespace}
{{
    public {typeKeyword} {_className}Data
    {{
    }}
}}
";
            File.WriteAllText(filePath, content);
            Debug.Log($"Created: {filePath}");
        }

        /// <summary>
        /// Creates the Cell file for the ScrollVirtualizer.
        /// </summary>
        private void CreateCellFile()
        {
            var fileName = $"{_className}Cell.cs";
            var filePath = Path.Combine(_targetPath, fileName);

            var hasUniTask = HasUniTask;
            var asyncUsing = hasUniTask ? "using Cysharp.Threading.Tasks;" : "using System.Threading.Tasks;";
            var taskType = hasUniTask ? "UniTask" : "Task";

            string content;
            if (_useContext)
            {
                content = string.IsNullOrWhiteSpace(_namespace)
                    ? $@"using System.Threading;
{asyncUsing}
using NoranDev.ScrollVirtualizer;

public class {_className}Cell : ScrollVirtualizerCellWithContext<{_className}Data, {_className}Context>
{{
    private {_className}Data _data;

    public override void Initialize({_className}Context context)
    {{
    }}

    public override void UpdateCell({_className}Data data)
    {{
        _data = data;
    }}

    public override async {taskType} UpdateCellAsync({_className}Data data, CancellationToken ct)
    {{
        _data = data;
    }}
}}
"
                    : $@"using System.Threading;
{asyncUsing}
using NoranDev.ScrollVirtualizer;

namespace {_namespace}
{{
    public class {_className}Cell : ScrollVirtualizerCellWithContext<{_className}Data, {_className}Context>
    {{
        private {_className}Data _data;

        public override void Initialize({_className}Context context)
        {{
        }}

        public override void UpdateCell({_className}Data data)
        {{
            _data = data;
        }}

        public override async {taskType} UpdateCellAsync({_className}Data data, CancellationToken ct)
        {{
            _data = data;
        }}
    }}
}}
";
            }
            else
            {
                content = string.IsNullOrWhiteSpace(_namespace)
                    ? $@"using System.Threading;
{asyncUsing}
using NoranDev.ScrollVirtualizer;

public class {_className}Cell : ScrollVirtualizerCell<{_className}Data>
{{
    public override void UpdateCell({_className}Data data)
    {{
    }}

    public override async {taskType} UpdateCellAsync({_className}Data data, CancellationToken ct)
    {{
    }}
}}
"
                    : $@"using System.Threading;
{asyncUsing}
using NoranDev.ScrollVirtualizer;

namespace {_namespace}
{{
    public class {_className}Cell : ScrollVirtualizerCell<{_className}Data>
    {{
        public override void UpdateCell({_className}Data data)
        {{
        }}

        public override async {taskType} UpdateCellAsync({_className}Data data, CancellationToken ct)
        {{
        }}
    }}
}}
";
            }

            File.WriteAllText(filePath, content);
            Debug.Log($"Created: {filePath}");
        }

        /// <summary>
        /// Creates the Context file for the ScrollVirtualizer.
        /// </summary>
        private void CreateContextFile()
        {
            var fileName = $"{_className}Context.cs";
            var filePath = Path.Combine(_targetPath, fileName);

            var typeKeyword = _contextType == DataType.Class ? "class" : "readonly struct";

            var content = string.IsNullOrWhiteSpace(_namespace)
                ? $@"using System;

public {typeKeyword} {_className}Context
{{
}}
"
                : $@"using System;

namespace {_namespace}
{{
    public {typeKeyword} {_className}Context
    {{
    }}
}}
";
            File.WriteAllText(filePath, content);
            Debug.Log($"Created: {filePath}");
        }

        /// <summary>
        /// Creates the ScrollVirtualizer file.
        /// </summary>
        private void CreateScrollVirtualizerFile()
        {
            var fileName = $"{_className}ScrollVirtualizer.cs";
            var filePath = Path.Combine(_targetPath, fileName);

            string content;
            var baseClass = _virtualizerType switch
            {
                ScrollVirtualizerType.Vertical => _useContext ? "VerticalScrollVirtualizerWithContext" : "VerticalScrollVirtualizer",
                ScrollVirtualizerType.Horizontal => _useContext ? "HorizontalScrollVirtualizerWithContext" : "HorizontalScrollVirtualizer",
                ScrollVirtualizerType.Grid => _useContext ? "GridScrollVirtualizerWithContext" : "GridScrollVirtualizer",
                _ => _useContext ? "VerticalScrollVirtualizerWithContext" : "VerticalScrollVirtualizer"
            };

            if (_useContext)
            {
                content = string.IsNullOrWhiteSpace(_namespace)
                    ? $@"using System.Collections.Generic;
using NoranDev.ScrollVirtualizer;

public class {_className}ScrollVirtualizer : {baseClass}<{_className}Cell, {_className}Data, {_className}Context>
{{
    private List<{_className}Data> _items = new();

    protected override {_className}Context CreateContext()
    {{
        return new {_className}Context();
    }}

    public void Initialize(List<{_className}Data> items)
    {{
        _items = items;
        InitializeContents(_items);
    }}

    public void UpdateList(List<{_className}Data> items, bool resetScrollPosition = true)
    {{
        _items = items;
        UpdateContents(_items, resetScrollPosition);
    }}

    public void AddList(List<{_className}Data> items, bool insertAtStart = false)
    {{
        _items.AddRange(items);
        AddContents(items, insertAtStart);
    }}
}}
"
                    : $@"using System.Collections.Generic;
using NoranDev.ScrollVirtualizer;

namespace {_namespace}
{{
    public class {_className}ScrollVirtualizer : {baseClass}<{_className}Cell, {_className}Data, {_className}Context>
    {{
        private List<{_className}Data> _items = new();

        protected override {_className}Context CreateContext()
        {{
            return new {_className}Context();
        }}

        public void Initialize(List<{_className}Data> items)
        {{
            _items = items;
            InitializeContents(_items);
        }}

        public void UpdateList(List<{_className}Data> items, bool resetScrollPosition = true)
        {{
            _items = items;
            UpdateContents(_items, resetScrollPosition);
        }}

        public void AddList(List<{_className}Data> items, bool insertAtStart = false)
        {{
            _items.AddRange(items);
            AddContents(items, insertAtStart);
        }}
    }}
}}
";
            }
            else
            {
                content = string.IsNullOrWhiteSpace(_namespace)
                    ? $@"using System.Collections.Generic;
using NoranDev.ScrollVirtualizer;

public class {_className}ScrollVirtualizer : {baseClass}<{_className}Cell, {_className}Data>
{{
    private List<{_className}Data> _items = new();

    public void Initialize(List<{_className}Data> items)
    {{
        _items = items;
        InitializeContents(_items);
    }}

    public void UpdateList(List<{_className}Data> items, bool resetScrollPosition = true)
    {{
        _items = items;
        UpdateContents(_items, resetScrollPosition);
    }}

    public void AddList(List<{_className}Data> items, bool insertAtStart = false)
    {{
        _items.AddRange(items);
        AddContents(items, insertAtStart);
    }}
}}
"
                    : $@"using System.Collections.Generic;
using NoranDev.ScrollVirtualizer;

namespace {_namespace}
{{
    public class {_className}ScrollVirtualizer : {baseClass}<{_className}Cell, {_className}Data>
    {{
        private List<{_className}Data> _items = new();

        public void Initialize(List<{_className}Data> items)
        {{
            _items = items;
            InitializeContents(_items);
        }}

        public void UpdateList(List<{_className}Data> items, bool resetScrollPosition = true)
        {{
            _items = items;
            UpdateContents(_items, resetScrollPosition);
        }}

        public void AddList(List<{_className}Data> items, bool insertAtStart = false)
        {{
            _items.AddRange(items);
            AddContents(items, insertAtStart);
        }}
    }}
}}
";
            }

            File.WriteAllText(filePath, content);
            Debug.Log($"Created: {filePath}");
        }
    }
}
