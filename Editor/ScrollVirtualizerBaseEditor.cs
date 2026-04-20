using UnityEditor;
using UnityEngine;

namespace NoranDev.ScrollVirtualizer.Editor
{
    /// <summary>
    /// Custom editor for ScrollVirtualizerBase that groups and makes Padding fields collapsible.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class ScrollVirtualizerBaseEditor : UnityEditor.Editor
    {
        private bool _paddingFoldout = true;

        public override void OnInspectorGUI()
        {
            if (!(target is IScrollVirtualizer))
            {
                DrawDefaultInspector();
                return;
            }

            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            SerializedProperty paddingLeft = null;
            SerializedProperty paddingRight = null;
            SerializedProperty paddingTop = null;
            SerializedProperty paddingBottom = null;

            SerializedProperty cellWidth = null;
            SerializedProperty cellHeight = null;

            SerializedProperty useDynamicCellWidth = serializedObject.FindProperty("useDynamicCellWidth");
            SerializedProperty useDynamicCellHeight = serializedObject.FindProperty("useDynamicCellHeight");

            SerializedProperty contentAlignment = serializedObject.FindProperty("verticalContentAlignment");
            if (contentAlignment == null)
            {
                contentAlignment = serializedObject.FindProperty("horizontalContentAlignment");
            }

            SerializedProperty constraintProp = serializedObject.FindProperty("constraint");

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                    continue;
                }

                if (iterator.name == "useDynamicCellWidth" || iterator.name == "useDynamicCellHeight")
                {
                    EditorGUILayout.PropertyField(iterator, true);
                    continue;
                }

                if (iterator.name == "cellWidth")
                {
                    cellWidth = serializedObject.FindProperty(iterator.propertyPath);
                    continue;
                }
                if (iterator.name == "cellHeight")
                {
                    cellHeight = serializedObject.FindProperty(iterator.propertyPath);

                    if (cellWidth != null && cellHeight != null)
                    {
                        bool isDynamicWidth = useDynamicCellWidth != null && useDynamicCellWidth.boolValue;
                        bool isDynamicHeight = useDynamicCellHeight != null && useDynamicCellHeight.boolValue;

                        if (!isDynamicWidth && !isDynamicHeight)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.PrefixLabel("Cell Size");
                            EditorGUILayout.LabelField("X", GUILayout.Width(15));
                            cellWidth.floatValue = EditorGUILayout.FloatField(cellWidth.floatValue, GUILayout.MinWidth(30));
                            EditorGUILayout.LabelField("Y", GUILayout.Width(15));
                            cellHeight.floatValue = EditorGUILayout.FloatField(cellHeight.floatValue, GUILayout.MinWidth(30));
                            EditorGUILayout.EndHorizontal();
                        }
                        else if (!isDynamicWidth && isDynamicHeight)
                        {
                            EditorGUILayout.PropertyField(cellWidth, new GUIContent("Cell Width"));
                        }
                        else if (isDynamicWidth && !isDynamicHeight)
                        {
                            EditorGUILayout.PropertyField(cellHeight, new GUIContent("Cell Height"));
                        }
                    }
                    continue;
                }

                if (iterator.name == "paddingLeft")
                {
                    paddingLeft = serializedObject.FindProperty(iterator.propertyPath);
                    continue;
                }
                if (iterator.name == "paddingRight")
                {
                    paddingRight = serializedObject.FindProperty(iterator.propertyPath);
                    continue;
                }
                if (iterator.name == "paddingTop")
                {
                    paddingTop = serializedObject.FindProperty(iterator.propertyPath);
                    continue;
                }
                if (iterator.name == "paddingBottom")
                {
                    paddingBottom = serializedObject.FindProperty(iterator.propertyPath);

                    if (paddingLeft != null && paddingRight != null && paddingTop != null && paddingBottom != null)
                    {
                        _paddingFoldout = EditorGUILayout.Foldout(_paddingFoldout, "Padding", true);
                        if (_paddingFoldout)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(paddingLeft, new GUIContent("Left"));
                            EditorGUILayout.PropertyField(paddingRight, new GUIContent("Right"));
                            EditorGUILayout.PropertyField(paddingTop, new GUIContent("Top"));
                            EditorGUILayout.PropertyField(paddingBottom, new GUIContent("Bottom"));
                            EditorGUI.indentLevel--;
                        }
                    }
                    continue;
                }

                if (iterator.name == "verticalContentAlignment" || iterator.name == "horizontalContentAlignment")
                {
                    continue;
                }

                if (iterator.name == "spacing")
                {
                    if (serializedObject.FindProperty("spacingX") != null)
                    {
                        continue;
                    }

                    if (contentAlignment != null)
                    {
                        bool isDynamicWidth = useDynamicCellWidth != null && useDynamicCellWidth.boolValue;
                        bool isDynamicHeight = useDynamicCellHeight != null && useDynamicCellHeight.boolValue;

                        if (!isDynamicWidth && !isDynamicHeight)
                        {
                            EditorGUILayout.PropertyField(contentAlignment);
                        }
                    }
                }

                if (iterator.name == "constraintCount")
                {
                    if (constraintProp != null && constraintProp.enumValueIndex == 0)
                    {
                        continue;
                    }
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
