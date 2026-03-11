using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(RuleBasedTileSet))]
public class RuleBasedTileSetEditor : Editor
{
    private ReorderableList list;

    private void OnEnable()
    {
        list = new ReorderableList(serializedObject, serializedObject.FindProperty("rules"), true, true, true, true);
        list.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Rule Tile Rules (8 Neighbors)"); };
        list.elementHeight = 90; // Висота для комфортного розміщення 3х3

        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var spriteProp = element.FindPropertyRelative("sprite");
            rect.y += 5;

            float rowHeight = 18;
            float labelWidth = 80;
            float gridCellSize = 22;
            float previewSize = 70;

            // --- ЛІВА ЧАСТИНА (Параметри як GameObject/Collider) ---
            float leftPartWidth = 150;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, rowHeight), "GameObject");
            EditorGUI.TextField(new Rect(rect.x + labelWidth, rect.y, leftPartWidth - labelWidth, rowHeight), "Single");
            
            EditorGUI.LabelField(new Rect(rect.x, rect.y + rowHeight, labelWidth, rowHeight), "Collider");
            EditorGUI.Popup(new Rect(rect.x + labelWidth, rect.y + rowHeight, leftPartWidth - labelWidth, rowHeight), 0, new string[] { "None", "Sprite", "Grid" });

            EditorGUI.LabelField(new Rect(rect.x, rect.y + rowHeight * 2, labelWidth, rowHeight), "Output");
            EditorGUI.Popup(new Rect(rect.x + labelWidth, rect.y + rowHeight * 2, leftPartWidth - labelWidth, rowHeight), 0, new string[] { "Single", "Random", "Animated" });

            // --- ЦЕНТРАЛЬНА ЧАСТИНА (СІТКА 3x3) ---
            float gridStartX = rect.x + leftPartWidth + 20;
            float gridStartY = rect.y;

            // Малюємо лінії сітки
            DrawGridLines(new Rect(gridStartX, gridStartY, gridCellSize * 3, gridCellSize * 3), gridCellSize);

            // Ряд 1 (Top)
            DrawToggle(new Rect(gridStartX, gridStartY, gridCellSize, gridCellSize), element.FindPropertyRelative("upLeft"));
            DrawToggle(new Rect(gridStartX + gridCellSize, gridStartY, gridCellSize, gridCellSize), element.FindPropertyRelative("up"));
            DrawToggle(new Rect(gridStartX + gridCellSize * 2, gridStartY, gridCellSize, gridCellSize), element.FindPropertyRelative("upRight"));

            // Ряд 2 (Middle)
            DrawToggle(new Rect(gridStartX, gridStartY + gridCellSize, gridCellSize, gridCellSize), element.FindPropertyRelative("left"));
            GUI.Box(new Rect(gridStartX + gridCellSize, gridStartY + gridCellSize, gridCellSize, gridCellSize), "■", EditorStyles.centeredGreyMiniLabel);
            DrawToggle(new Rect(gridStartX + gridCellSize * 2, gridStartY + gridCellSize, gridCellSize, gridCellSize), element.FindPropertyRelative("right"));

            // Ряд 3 (Bottom)
            DrawToggle(new Rect(gridStartX, gridStartY + gridCellSize * 2, gridCellSize, gridCellSize), element.FindPropertyRelative("downLeft"));
            DrawToggle(new Rect(gridStartX + gridCellSize, gridStartY + gridCellSize * 2, gridCellSize, gridCellSize), element.FindPropertyRelative("down"));
            DrawToggle(new Rect(gridStartX + gridCellSize * 2, gridStartY + gridCellSize * 2, gridCellSize, gridCellSize), element.FindPropertyRelative("downRight"));

            // --- ПРАВА ЧАСТИНА (ПОСИЛАННЯ ТА ПРЕВ'Ю) ---
            float previewX = rect.x + rect.width - previewSize;
            
            // 1. Посилання на спрайт (вузьке поле зверху)
            EditorGUI.PropertyField(new Rect(previewX, rect.y, previewSize, rowHeight), spriteProp, GUIContent.none);
            // 2. Темно-сірий квадрат прев'ю
            Rect previewRect = new Rect(previewX, rect.y + rowHeight + 2, previewSize, previewSize - rowHeight - 2);
            EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f, 1f)); // Темно-сірий фон
            
            if (spriteProp.objectReferenceValue != null)
            {
                Sprite s = (Sprite)spriteProp.objectReferenceValue;
                // Малюємо текстуру спрайта всередині квадрата
                // AssetPreview працює швидше і виглядає краще в редакторі
                Texture2D previewTex = AssetPreview.GetAssetPreview(s);
                if (previewTex != null)
                {
                    GUI.DrawTexture(previewRect, previewTex, ScaleMode.ScaleToFit);
                }
            }
            else
            {
                EditorGUI.LabelField(previewRect, "None", EditorStyles.centeredGreyMiniLabel);
            }
        };
    }

    private void DrawGridLines(Rect rect, float size)
    {
        Handles.BeginGUI();
        Handles.color = new Color(0, 0, 0, 0.3f);
        for (int i = 0; i <= 3; i++) {
            Handles.DrawLine(new Vector2(rect.x + i * size, rect.y), new Vector2(rect.x + i * size, rect.y + rect.height));
            Handles.DrawLine(new Vector2(rect.x, rect.y + i * size), new Vector2(rect.x + rect.width, rect.y + i * size));
        }
        Handles.EndGUI();
    }

    private void DrawToggle(Rect rect, SerializedProperty prop)
    {
        Color oldColor = GUI.color;
        // Зелений - "Must Have", Червоний - "No Neighbor"
        GUI.color = prop.boolValue ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
        
        // Використовуємо символи стрілок/хрестиків залежно від стану
        string label = prop.boolValue ? "✔" : "✘";
        
        if (GUI.Button(rect, label, EditorStyles.miniButton)) {
            prop.boolValue = !prop.boolValue;
        }
        GUI.color = oldColor;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultSprite"));
        list.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}