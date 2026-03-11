using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode] // щоб працювало в редакторі
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteYSorter : MonoBehaviour
{
    public int precision = 100; // Точність сортування
    public int minSortingOrder = -1000; // Мінімальний Order
    public int maxSortingOrder = 1000;  // Максимальний Order

    private SpriteRenderer spriteRenderer;
    private Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSorting();
    }

    private void LateUpdate()
    {
        UpdateSorting();
    }

    private void UpdateSorting()
    {
        if (spriteRenderer == null) return;

        int order = -(int)(cachedTransform.position.y * precision);

        // Обмежуємо діапазон
        if (order < minSortingOrder) order = minSortingOrder;
        if (order > maxSortingOrder) order = maxSortingOrder;

        spriteRenderer.sortingOrder = order;
    }
}