using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class SpriteLayout : MonoBehaviour
{
    [Header("Layout")]
    public LayoutType layoutType = LayoutType.Horizontal;
    public enum LayoutType { Horizontal, Vertical, Grid }
    public float spacing = 0.1f;
    public float cellWidth = 1f;
    public float cellHeight = 1f;
    public int columns = 3;

    public enum Order { LeftToRight, RightToLeft, Center }
    public Order order = Order.LeftToRight;

    public enum ControlChildSize { None, Width, Height, Both }
    public ControlChildSize controlChildSize = ControlChildSize.None;

    public enum SortingMode { None, SameForAll, Incremental }
    [Header("Sorting")]
    public SortingMode sortingMode = SortingMode.None;
    public int baseSortingOrder = 0;

    private void Start()
    {
        ApplyLayout();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyLayout();
    }
#endif

    private void OnTransformChildrenChanged()
    {
        ApplyLayout();
    }

    // =========================
    // APPLY
    // =========================

    public void ApplyLayout()
    {
        List<Transform> childs = new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
            childs.Add(transform.GetChild(i));

        if (order == Order.RightToLeft)
            childs.Reverse();

        if (layoutType == LayoutType.Horizontal)
            ApplyHorizontal(childs);
        else if (layoutType == LayoutType.Vertical)
            ApplyVertical(childs);
        else
            ApplyGrid(childs);

        ApplySorting(childs);
    }

    // =========================
    // SIZE CONTROL
    // =========================

    private void ControlSize(Transform child)
    {
        if (controlChildSize == ControlChildSize.None)
            return;

        SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return;

        Vector3 scale = child.localScale;
        Vector3 size = sr.sprite.bounds.size;

        if (size.x == 0f || size.y == 0f)
            return;

        if (controlChildSize == ControlChildSize.Width || controlChildSize == ControlChildSize.Both)
            scale.x = cellWidth / size.x;

        if (controlChildSize == ControlChildSize.Height || controlChildSize == ControlChildSize.Both)
            scale.y = cellHeight / size.y;

#if UNITY_EDITOR
        Undo.RecordObject(child, "Sprite Layout Resize");
#endif
        child.localScale = scale;
    }

    // =========================
    // SORTING
    // =========================

    private void ApplySorting(List<Transform> childs)
    {
        if (sortingMode == SortingMode.None)
            return;

        for (int i = 0; i < childs.Count; i++)
        {
            SpriteRenderer sr = childs[i].GetComponent<SpriteRenderer>();
            if (sr == null)
                continue;

#if UNITY_EDITOR
            Undo.RecordObject(sr, "Sprite Layout Sorting");
#endif

            if (sortingMode == SortingMode.SameForAll)
                sr.sortingOrder = baseSortingOrder;
            else if (sortingMode == SortingMode.Incremental)
                sr.sortingOrder = baseSortingOrder + i;
        }
    }

    // =========================
    // LAYOUT TYPES
    // =========================

    private void ApplyHorizontal(List<Transform> childs)
    {
        float step = cellWidth + spacing;
        float x = 0f;

        if (order == Order.Center)
            x = -((childs.Count - 1) * step) * 0.5f;

        for (int i = 0; i < childs.Count; i++)
        {
            ControlSize(childs[i]);
#if UNITY_EDITOR
            Undo.RecordObject(childs[i], "Sprite Layout Move");
#endif
            childs[i].localPosition = new Vector3(x, 0f, 0f);
            x += step;
        }
    }

    private void ApplyVertical(List<Transform> childs)
    {
        float step = cellHeight + spacing;
        float y = 0f;

        if (order == Order.Center)
            y = ((childs.Count - 1) * step) * 0.5f;

        for (int i = 0; i < childs.Count; i++)
        {
            ControlSize(childs[i]);
#if UNITY_EDITOR
            Undo.RecordObject(childs[i], "Sprite Layout Move");
#endif
            childs[i].localPosition = new Vector3(0f, y, 0f);
            y -= step;
        }
    }

private void ApplyGrid(List<Transform> childs)
{
    int safeColumns = Mathf.Max(1, columns);
    int rows = Mathf.CeilToInt((float)childs.Count / safeColumns);

    float stepX = cellWidth + spacing;
    float stepY = cellHeight + spacing;

    int index = 0;

    for (int row = 0; row < rows; row++)
    {
        // Кількість елементів у цьому ряду
        int elementsInRow = Mathf.Min(safeColumns, childs.Count - row * safeColumns);

        // Визначаємо totalWidth для цього ряду
        float totalRowWidth = elementsInRow * cellWidth + (elementsInRow - 1) * spacing;

        // Позиція першої клітинки в ряду по X (центр рядка)
        float startX = -totalRowWidth * 0.5f + cellWidth * 0.5f;

        // Позиція ряду по Y
        float y = (rows * stepY * 0.5f) - stepY * row - cellHeight * 0.5f;

        for (int col = 0; col < elementsInRow; col++)
        {
            Transform child = childs[index++];
            ControlSize(child);

            float x = startX + col * stepX;

#if UNITY_EDITOR
            Undo.RecordObject(child, "Sprite Layout Move");
#endif
            child.localPosition = new Vector3(x, y, 0f);
        }
    }
}
#if UNITY_EDITOR
private void OnDrawGizmos()
{
    int childCount = transform.childCount;
    int countForGizmo = Mathf.Max(1, childCount);

    Vector3 center = transform.position;

    if (layoutType == LayoutType.Horizontal)
    {
        float totalWidth = countForGizmo * cellWidth + (countForGizmo - 1) * spacing;
        float half = totalWidth * 0.5f;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(center, new Vector3(totalWidth, cellHeight, 0f));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center + new Vector3(-half, 0f, 0f),
                        center + new Vector3(half, 0f, 0f));
    }
    else if (layoutType == LayoutType.Vertical)
    {
        float totalHeight = countForGizmo * cellHeight + (countForGizmo - 1) * spacing;
        float half = totalHeight * 0.5f;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(center, new Vector3(cellWidth, totalHeight, 0f));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center + new Vector3(0f, half, 0f),
                        center + new Vector3(0f, -half, 0f));
    }
    else if (layoutType == LayoutType.Grid)
    {
        int safeColumns = Mathf.Max(1, columns);
        int rows = Mathf.CeilToInt((float)countForGizmo / safeColumns);

        float stepX = cellWidth + spacing;
        float stepY = cellHeight + spacing;

        // Визначаємо повну ширину та висоту гріду
        float totalWidth = Mathf.Min(countForGizmo, safeColumns) * cellWidth + (Mathf.Min(countForGizmo, safeColumns) - 1) * spacing;
        float totalHeight = rows * cellHeight + (rows - 1) * spacing;

        float startX = -totalWidth * 0.5f + cellWidth * 0.5f;
        float startY = totalHeight * 0.5f - cellHeight * 0.5f;

        Gizmos.color = Color.white;

        // Малюємо рамку навколо всього гріду
        Gizmos.DrawWireCube(center, new Vector3(totalWidth, totalHeight, 0f));

        // Малюємо вертикальні лінії по центрах клітинок
        for (int c = 1; c < safeColumns; c++)
        {
            float x = startX + c * stepX - cellWidth * 0.5f;
            Gizmos.DrawLine(center + new Vector3(x, startY + cellHeight * 0.5f, 0f),
                            center + new Vector3(x, startY - totalHeight + cellHeight * 0.5f, 0f));
        }

        // Малюємо горизонтальні лінії по центрах клітинок
        for (int r = 1; r < rows; r++)
        {
            float y = startY - r * stepY + cellHeight * 0.5f;
            Gizmos.DrawLine(center + new Vector3(startX - cellWidth * 0.5f, y, 0f),
                            center + new Vector3(startX + totalWidth - cellWidth * 0.5f, y, 0f));
        }

        // Центральні осі
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center + new Vector3(-totalWidth * 0.5f, 0f, 0f), center + new Vector3(totalWidth * 0.5f, 0f, 0f));
        Gizmos.DrawLine(center + new Vector3(0f, totalHeight * 0.5f, 0f), center + new Vector3(0f, -totalHeight * 0.5f, 0f));
    }
}
#endif
}