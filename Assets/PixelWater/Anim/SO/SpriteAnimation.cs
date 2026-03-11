using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AnimationEvent
{
    public int frameIndex; // На якому кадрі спрацює
    public string eventName; // Назва методу або тег події
}

[CreateAssetMenu(fileName = "NewAnimation", menuName = "Sprite/Sprite Animation")]
public class SpriteAnimation : ScriptableObject 
{
    public Sprite[] framesArray;
    public float frameRate = 0.1f;
    // Список наших подій
    public AnimationEvent[] events;
}