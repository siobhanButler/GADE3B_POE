using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FurnitureObj))]
public class FurnitureObjectEditor : Editor
{
    private SerializedProperty _takenSmallCellGridProperty;

    private void OnEnable()
    {
        _takenSmallCellGridProperty = serializedObject.FindProperty("takenSmallCellGrid");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "takenSmallCellGrid");

        if (_takenSmallCellGridProperty == null)
        {
            EditorGUILayout.HelpBox("takenSmallCellGrid property not found.", MessageType.Error);
        }
        else
        {
            FurnitureObj furniture = (FurnitureObj)target;
            int subAxis = Mathf.Max(1, furniture.subCellsPerAxis);
            int totalBigCells = Mathf.Max(1, furniture.width * furniture.length);
            int subcellsPerBig = subAxis * subAxis;
            int expectedArraySize = subcellsPerBig * totalBigCells;

            if (_takenSmallCellGridProperty.arraySize != expectedArraySize)
            {
                _takenSmallCellGridProperty.arraySize = expectedArraySize;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Taken Small Cell Grid ({subAxis} x {subAxis} per big cell)", EditorStyles.boldLabel);

            for (int big = 0; big < totalBigCells; big++)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Big Cell {big + 1}", EditorStyles.miniBoldLabel);
                for (int row = 0; row < subAxis; row++)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int col = 0; col < subAxis; col++)
                    {
                        int flatIndex = big * subcellsPerBig + row * subAxis + col;
                        SerializedProperty element = _takenSmallCellGridProperty.GetArrayElementAtIndex(flatIndex);
                        bool value = element.boolValue;
                        value = GUILayout.Toggle(value, GUIContent.none, GUILayout.Width(18));
                        element.boolValue = value;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear"))
            {
                for (int i = 0; i < _takenSmallCellGridProperty.arraySize; i++)
                {
                    _takenSmallCellGridProperty.GetArrayElementAtIndex(i).boolValue = false;
                }
            }
            if (GUILayout.Button("Fill"))
            {
                for (int i = 0; i < _takenSmallCellGridProperty.arraySize; i++)
                {
                    _takenSmallCellGridProperty.GetArrayElementAtIndex(i).boolValue = true;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }
}


