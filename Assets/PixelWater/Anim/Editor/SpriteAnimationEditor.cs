using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(SpriteAnimation))]
public class SpriteAnimationEditor : Editor 
{
    private static float lastFrameTime;
    private static int currentFrameIndex;
    private static bool isGlobalPreviewPlaying = false; 

    private void OnEnable() 
    {
        EditorApplication.update += UpdatePreview;
    }

    private void OnDisable() 
    {
        EditorApplication.update -= UpdatePreview;
    }

    private void UpdatePreview() 
    {
        SpriteAnimation anim = (SpriteAnimation)target;
        if (anim != null && isGlobalPreviewPlaying) 
        {
            float currentTime = (float)EditorApplication.timeSinceStartup;
            
            if (anim.framesArray != null && currentFrameIndex >= anim.framesArray.Length)
                currentFrameIndex = 0;

            if (currentTime - lastFrameTime > anim.frameRate) 
            {
                if (anim.framesArray != null && anim.framesArray.Length > 0)
                {
                    currentFrameIndex = (currentFrameIndex + 1) % anim.framesArray.Length;
                }
                lastFrameTime = currentTime;
                Repaint(); 
            }
        }
    }

    public override void OnInspectorGUI() 
    {
        SpriteAnimation anim = (SpriteAnimation)target;

        anim.frameRate = EditorGUILayout.FloatField("Frame Rate (sec)", anim.frameRate);

        // 1. ПОЛЯ СПРАЙТІВ ТА ШВИДКОСТІ
        SerializedProperty framesProp = serializedObject.FindProperty("framesArray");
        EditorGUILayout.PropertyField(framesProp, new GUIContent("Animation Frames"), true);

        //EditorGUILayout.Space();

        // 2. ПОЛЯ ПОДІЙ (Ось звідки з'явиться список з "+")
        SerializedProperty eventsProp = serializedObject.FindProperty("events");
        EditorGUILayout.PropertyField(eventsProp, new GUIContent("Animation Events"), true);

        serializedObject.ApplyModifiedProperties();

        if (anim.framesArray == null || anim.framesArray.Length == 0)
        {
            EditorGUILayout.HelpBox("Додайте спрайти у список.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        // --- ПЛЕЄР ---
        float previewSize = 235f;
        float padding = 10f;
        
        Rect layoutRect = EditorGUILayout.GetControlRect(GUILayout.Height(previewSize + 90));
        float centerX = layoutRect.x + (layoutRect.width - previewSize) / 2;

        Rect fullBoxRect = new Rect(centerX - padding, layoutRect.y, previewSize + (padding * 2), previewSize + 85);
        EditorGUI.HelpBox(fullBoxRect, "", MessageType.None);

        Rect spriteRect = new Rect(centerX, layoutRect.y + padding, previewSize, previewSize);
        EditorGUI.DrawRect(spriteRect, new Color(0.12f, 0.12f, 0.12f, 1f));

        if (currentFrameIndex >= anim.framesArray.Length) currentFrameIndex = 0;

        Texture2D tex = AssetPreview.GetAssetPreview(anim.framesArray[currentFrameIndex]);
        if (tex != null) 
        {
            tex.filterMode = FilterMode.Point;
            GUI.DrawTexture(spriteRect, tex, ScaleMode.ScaleToFit);
        }
        else
        {
            GUIStyle loadingStyle = new GUIStyle();
            loadingStyle.alignment = TextAnchor.MiddleCenter;
            loadingStyle.normal.textColor = Color.gray;
            GUI.Label(spriteRect, "Loading...", loadingStyle);
        } 

        // --- ТАЙМЛАЙН З ПОЗНАЧКАМИ ПОДІЙ ---
        Rect timelineRect = new Rect(centerX, spriteRect.yMax + 10, previewSize, 20);
        
        // Малюємо підкладку під івенти (маленькі позначки)
        if (anim.events != null && anim.framesArray.Length > 1)
        {
            GUIStyle eventIdStyle = new GUIStyle(EditorStyles.miniLabel);
            eventIdStyle.normal.textColor = new Color(1f, 0.5f, 0.5f, 1f); // Світло-червоний колір
            eventIdStyle.fontSize = 9;
            eventIdStyle.alignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < anim.events.Length; i++)
            {
                var evt = anim.events[i];
                
                // Розраховуємо позицію позначки на слайдері
                float t = (float)evt.frameIndex / (anim.framesArray.Length - 1);
                float markX = timelineRect.x + (t * timelineRect.width);
                
                // 1. Малюємо червону лінію (івент)
                Rect markRect = new Rect(markX - 1, timelineRect.y - 4, 3, 8);
                EditorGUI.DrawRect(markRect, new Color(1f, 0.3f, 0.3f, 1f));

                // 2. Малюємо ID події (індекс масиву) над рискою
                Rect idRect = new Rect(markX - 10, timelineRect.y - 16, 20, 12);
                GUI.Label(idRect, i.ToString(), eventIdStyle);
            }
        }

        EditorGUI.BeginChangeCheck();
        int newFrame = Mathf.RoundToInt(GUI.HorizontalSlider(timelineRect, currentFrameIndex, 0, anim.framesArray.Length - 1));
        if (EditorGUI.EndChangeCheck())
        {
            currentFrameIndex = newFrame;
            isGlobalPreviewPlaying = false; 
            Repaint();
        }

        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(centerX, timelineRect.yMax - 5, previewSize, 15), 
            "Frame: " + currentFrameIndex +  "/" + (anim.framesArray.Length - 1), labelStyle);

        // Кнопки
        float btnWidth = (previewSize / 2) - 2;
        Rect playBtnRect = new Rect(centerX, timelineRect.yMax + 15, btnWidth, 22);
        Rect stopBtnRect = new Rect(centerX + btnWidth + 4, timelineRect.yMax + 15, btnWidth, 22);

        if (GUI.Button(playBtnRect, isGlobalPreviewPlaying ? "Pause" : "Play")) 
        {
            isGlobalPreviewPlaying = !isGlobalPreviewPlaying;
            if (isGlobalPreviewPlaying) lastFrameTime = (float)EditorApplication.timeSinceStartup;
        }

        if (GUI.Button(stopBtnRect, "Stop")) 
        {
            isGlobalPreviewPlaying = false;
            currentFrameIndex = 0;
            Repaint();
        }
    }
}