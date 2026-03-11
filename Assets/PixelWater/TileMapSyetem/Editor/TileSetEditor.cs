using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileSetData))]
public class TileSetEditor : Editor
{
    private TileSetData tileSet;

    private void OnEnable()
    {
        tileSet = (TileSetData)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        SerializedProperty typeProp = serializedObject.FindProperty("setType");
        EditorGUILayout.PropertyField(typeProp, new GUIContent("Tile Set Mode:"));
        EditorGUILayout.Space();

        TileSetData.TileSetType currentType = (TileSetData.TileSetType)typeProp.enumValueIndex;

        switch (currentType)
        {
            case TileSetData.TileSetType.Simple:
                DrawSimpleMode();
                break;
            case TileSetData.TileSetType.Standard16:
                DrawStandardMode();
                break;
            case TileSetData.TileSetType.T_Shape:
                DrawTMode();
                break;
        }

        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(tileSet);
        }
    }

    // РЕЖИМ 1: Тільки базові (для стін або поодиноких об'єктів)
    private void DrawSimpleMode()
    {
        EditorGUILayout.HelpBox("Простий режим: Центр та ізольований тайл.", MessageType.None);
        EditorGUILayout.BeginHorizontal();
        DrawSpriteField("center", "Center");
        DrawSpriteField("isolated", "Isolated");
        EditorGUILayout.EndHorizontal();
    }

    // РЕЖИМ 2: Повна сітка
    private void DrawStandardMode()
    {
        EditorGUILayout.LabelField("Standard 16-Tile Grid", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        DrawSpriteField("topLeftCorner", "┌");
        DrawSpriteField("topEdge", "Top");
        DrawSpriteField("topRightCorner", "┐");
        DrawSpriteField("verticalPass", "Vert");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawSpriteField("leftEdge", "Left");
        DrawSpriteField("center", "CENTER");
        DrawSpriteField("rightEdge", "Right");
        DrawSpriteField("horizontalPass", "Hor");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawSpriteField("bottomLeftCorner", "└");
        DrawSpriteField("bottomEdge", "Bottom");
        DrawSpriteField("bottomRightCorner", "┘");
        DrawSpriteField("isolated", "Single");
        EditorGUILayout.EndHorizontal();
    }

    // РЕЖИМ 3: Тільки Т-подібні переходи
    private void DrawTMode()
    {
        EditorGUILayout.HelpBox("Тільки складні Т-подібні з'єднання.", MessageType.None);
        EditorGUILayout.BeginHorizontal();
        DrawSpriteField("tUpShape", "T-Up");
        DrawSpriteField("tDownShape", "T-Down");
        DrawSpriteField("tLeftShape", "T-Left");
        DrawSpriteField("tRightShape", "T-Right");
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSpriteField(string propertyName, string label)
    {
        SerializedProperty prop = serializedObject.FindProperty(propertyName);
        EditorGUILayout.BeginVertical(GUILayout.Width(65));
        prop.objectReferenceValue = EditorGUILayout.ObjectField(prop.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(64), GUILayout.Height(64));
        EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(64));
        EditorGUILayout.EndVertical();
    }
}