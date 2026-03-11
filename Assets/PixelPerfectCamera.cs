using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PixelPerfectCamera : MonoBehaviour
{
    public int targetPPU = 16;       // Pixels Per Unit вашого спрайта
    public int assetsPPU = 16;       // PPU для ваших арт-асетів (якщо відрізняється)
    public bool upscaleRT = false;   // Чи масштабувати RenderTexture для екрану
    public int refResolutionX = 320; // Базова ширина для pixel perfect
    public int refResolutionY = 180; // Базова висота для pixel perfect

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        UpdateCamera();
    }

    void LateUpdate()
    {
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        if (cam == null) return;

        // 1. Пікселі по висоті
        float screenHeight = (float)Screen.height;

        // 2. Орієнтоване на Assets PPU
        float unitsTall = screenHeight / (float)assetsPPU;

        // 3. Орто-камера розмір
        cam.orthographicSize = unitsTall / 2.0f;

        // 4. Оптимізація для ширини: підганяємо, щоб ціле число пікселів поміщалось
        float unitsWide = (float)Screen.width / (float)assetsPPU;
        float orthoSizeByWidth = unitsWide / cam.aspect / 2.0f;

        // Вибираємо менший розмір, щоб нічого не обрізалося
        cam.orthographicSize = Mathf.Min(cam.orthographicSize, orthoSizeByWidth);

        // 5. За бажанням: масштабування RenderTexture (для upscale)
        if (upscaleRT)
        {
            cam.pixelRect = new Rect(0, 0, Screen.width, Screen.height);
        }
    }
}

// [ExecuteInEditMode]
// [RequireComponent(typeof(Camera))]
// public class PixelPerfectCamera : MonoBehaviour
// {
//     public int targetPPU = 16; // Pixels Per Unit

//     private Camera cam;

//     private void Awake()
//     {
//         cam = GetComponent<Camera>();
//         if (cam == null)
//             cam = Camera.main; // на всякий випадок
//         UpdateCamera();
//     }
//     private void LateUpdate()
//     {
//         UpdateCamera();
//     }
//     private void UpdateCamera()
//     {
//         if (cam == null) return;

//         float screenHeight = (float)Screen.height;
//         cam.orthographicSize = screenHeight / (targetPPU * 2.0f);
//     }
// }