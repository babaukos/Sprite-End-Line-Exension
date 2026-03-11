using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour 
{
    public SpriteAnimation currentAnimation;
    public bool playOnStart = true;
    public bool loop = true;

    private SpriteRenderer spriteRenderer;
    private int currentFrame;
    private float timer;
    private bool isPlaying;

    private void Awake() 
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start() 
    {
        // Якщо ми вручну призначили анімацію в інспекторі ще до запуску гри
        if (currentAnimation != null)
        {
            // Встановлюємо перший кадр відразу, щоб об'єкт не був порожнім
            SetFrame(0);

            if (playOnStart)
            {
                Play(currentAnimation);
            }
        }
    }

    private void Update() 
    {
        // Якщо не грає, або немає анімації, або в масиві порожньо — виходимо
        if (!isPlaying || currentAnimation == null || currentAnimation.framesArray.Length == 0) 
            return;

        timer += Time.deltaTime;

        if (timer >= currentAnimation.frameRate) 
        {
            timer -= currentAnimation.frameRate;
            int nextFrame = currentFrame + 1;

            if (nextFrame >= currentAnimation.framesArray.Length) 
            {
                if (loop) 
                {
                    nextFrame = 0;
                }
                else 
                {
                    isPlaying = false;
                    return;
                }
            }

            SetFrame(nextFrame);
        }
    }

    // Окремий метод для встановлення кадру та перевірки івентів
    private void SetFrame(int index)
    {
        if (currentAnimation == null || index < 0 || index >= currentAnimation.framesArray.Length) 
            return;

        currentFrame = index;
        spriteRenderer.sprite = currentAnimation.framesArray[currentFrame];
        
        // Перевіряємо події для цього конкретного кадру
        CheckEvents(currentFrame);
    }

    private void CheckEvents(int frame)
    {
        if (currentAnimation.events == null) return;

        for (int i = 0; i < currentAnimation.events.Length; i++)
        {
            if (currentAnimation.events[i].frameIndex == frame)
            {
                OnAnimationEventTriggered(currentAnimation.events[i].eventName);
            }
        }
    }

    private void OnAnimationEventTriggered(string eventName)
    {
        Debug.Log("Подія анімації [" + gameObject.name + "]: " + eventName);
        // Тут буде твоя логіка звуків/ефектів
    }

    public void Play(SpriteAnimation anim) 
    {
        if (anim == null) return;

        currentAnimation = anim;
        currentFrame = 0;
        timer = 0;
        isPlaying = true;

        SetFrame(0);
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void Resume()
    {
        if (currentAnimation != null) isPlaying = true;
    }
}