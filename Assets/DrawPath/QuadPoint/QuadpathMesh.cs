using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuadPoint
{
    public Vector3 point;
    public Transform posit;

    public Vector3 GetPosition()
    {
        if (posit != null) return posit.position;
        return point;
    }
}

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class QuadpathMesh : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite startSprite;
    public Sprite bodySprite;
    public Sprite turnSprite;
    public Sprite endSprite;

    [Header("Visuals")]
    public float width = 0.5f;
    public float spacing = 0.6f;
    public Color color = Color.white;
    [Header("Offsets")]
    public float endOffset = 0f;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    [Header("Points")]
    public List<QuadPoint> linePoints = new List<QuadPoint>();

    private Mesh mesh;
    private MeshFilter mf;
    private MeshRenderer mr;
    private List<Vector3> lastFramePoints = new List<Vector3>();

    private void OnEnable()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        
        if (mf.sharedMesh == null || mf.sharedMesh.name != "PathMesh_Gen")
        {
            mesh = new Mesh();
            mesh.name = "PathMesh_Gen";
            mf.sharedMesh = mesh;
        }
        else mesh = mf.sharedMesh;
        
        UpdateSorting();
        UpdateMesh();
    }

    private void Update()
    {
        if (Application.isPlaying) TryAutoUpdate();
    }

#if UNITY_EDITOR
    private void LateUpdate()
    {
        if (!Application.isPlaying) TryAutoUpdate();
    }
    private void OnValidate()
    {
        UpdateSorting();
        if (!Application.isPlaying) UpdateMesh();
    }
#endif

    private void UpdateSorting()
    {
        if (mr == null) mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = sortingLayerName;
            mr.sortingOrder = sortingOrder;
        }
    }

    private void TryAutoUpdate()
    {
        List<Vector3> pts = GetWorldPoints();
        if (ArePointsChanged(pts))
        {
            lastFramePoints = pts;
            UpdateMesh();
        }
    }

    private bool ArePointsChanged(List<Vector3> current)
    {
        if (lastFramePoints == null || current.Count != lastFramePoints.Count) return true;
        for (int i = 0; i < current.Count; i++)
            if (Vector3.SqrMagnitude(current[i] - lastFramePoints[i]) > 0.0001f) return true;
        return false;
    }

    private List<Vector3> GetWorldPoints()
    {
        List<Vector3> pts = new List<Vector3>();
        for (int i = 0; i < linePoints.Count; i++) pts.Add(linePoints[i].GetPosition());
        return pts;
    }

    public void UpdateMesh()
    {
        List<Vector3> pts = GetWorldPoints();
        if (pts.Count < 2)
        {
            if (mesh != null) mesh.Clear();
            return;
        }

        // ===== APPLY END OFFSET =====
        if (endOffset > 0f)
        {
            int last = pts.Count - 1;
            Vector3 lastDir = (pts[last] - pts[last - 1]).normalized;
            pts[last] = pts[last] - lastDir * endOffset;
        }

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        List<Color> cols = new List<Color>();

        // 1. Спочатку розраховуємо загальну довжину шляху та позиції точок поворотів
        float totalPathLength = 0f;
        float[] distanceAtPoint = new float[pts.Count];

        for (int i = 0; i < pts.Count - 1; i++)
        {
            distanceAtPoint[i] = totalPathLength;
            totalPathLength += Vector3.Distance(pts[i], pts[i + 1]);
        }

        distanceAtPoint[pts.Count - 1] = totalPathLength;

        // ===== START =====
        if (startSprite != null)
        {
            Vector3 dir = (pts[1] - pts[0]).normalized;
            AddQuad(verts, uvs, cols, tris, pts[0], dir, startSprite);
        }

        // ===== BODY (Рівномірне заповнення) =====
        // Починаємо від першого spacing і йдемо до кінця шляху
        float currentDist = spacing;
        while (currentDist < totalPathLength - spacing * 0.5f)
        {
            // Знаходимо, на якому сегменті ми зараз знаходимось
            int segmentIndex = 0;
            for (int j = 0; j < pts.Count - 1; j++)
            {
                if (currentDist >= distanceAtPoint[j] && currentDist <= distanceAtPoint[j + 1])
                {
                    segmentIndex = j;
                    break;
                }
            }

            Vector3 a = pts[segmentIndex];
            Vector3 b = pts[segmentIndex + 1];
            float segmentDist = currentDist - distanceAtPoint[segmentIndex];
            float t = segmentDist / Vector3.Distance(a, b);
            
            Vector3 pos = Vector3.Lerp(a, b, t);
            Vector3 dir = (b - a).normalized;

            AddQuad(verts, uvs, cols, tris, pos, dir, bodySprite);
            
            currentDist += spacing;
        }

        // ===== TURNS (Тільки в точках зламу) =====
        if (turnSprite != null)
        {
            for (int i = 1; i < pts.Count - 1; i++)
            {
                Vector3 prevDir = (pts[i] - pts[i - 1]).normalized;
                Vector3 nextDir = (pts[i + 1] - pts[i]).normalized;
                float angle = GetSignedAngle(prevDir, nextDir);

                if (Mathf.Abs(angle) > 5f)
                {
                    Vector3 bisector = (prevDir + nextDir).normalized;
                    AddQuad(verts, uvs, cols, tris, pts[i], bisector, turnSprite);
                }
            }
        }

        // ===== END =====
        if (endSprite != null)
        {
            Vector3 dir = (pts[pts.Count - 1] - pts[pts.Count - 2]).normalized;
            AddQuad(verts, uvs, cols, tris, pts[pts.Count - 1], dir, endSprite);
        }

        // Оновлення мешу
        mesh.Clear();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = cols.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        SetupMaterial();
    }
    
    private void AddQuad(List<Vector3> verts, List<Vector2> uvs, List<Color> cols, List<int> tris, Vector3 pos, Vector3 forward, Sprite s)
    {
        if (s == null) return;

        int baseIdx = verts.Count;
        float hW = width * 0.5f;
        float hH = (width * (s.rect.height / s.rect.width)) * 0.5f; // Зберігаємо пропорції спрайту

        Vector3 right = new Vector3(-forward.y, forward.x, 0).normalized;
        Vector3 up = forward.normalized;

        // Обчислюємо 4 кути квада в локальних координатах об'єкта
        verts.Add(transform.InverseTransformPoint(pos - right * hW - up * hH));
        verts.Add(transform.InverseTransformPoint(pos + right * hW - up * hH));
        verts.Add(transform.InverseTransformPoint(pos - right * hW + up * hH));
        verts.Add(transform.InverseTransformPoint(pos + right * hW + up * hH));

        Rect r = GetSpriteRect(s);
        uvs.Add(new Vector2(r.xMin, r.yMin));
        uvs.Add(new Vector2(r.xMax, r.yMin));
        uvs.Add(new Vector2(r.xMin, r.yMax));
        uvs.Add(new Vector2(r.xMax, r.yMax));

        for (int i = 0; i < 4; i++) cols.Add(color);

        tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
        tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
    }

    private float GetSignedAngle(Vector3 from, Vector3 to)
    {
        float angle = Vector3.Angle(from, to);
        float cross = from.x * to.y - from.y * to.x;
        return cross < 0f ? -angle : angle;
    }

    private Rect GetSpriteRect(Sprite s)
    {
        if (s == null) return new Rect(0, 0, 1, 1);
        return new Rect(s.textureRect.x / s.texture.width, s.textureRect.y / s.texture.height, 
                        s.textureRect.width / s.texture.width, s.textureRect.height / s.texture.height);
    }

    private void SetupMaterial()
    {
        if (mr.sharedMaterial == null) mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        // Якщо використовуєш атлас, всі спрайти мають бути на одній текстурі
        if (bodySprite != null) mr.sharedMaterial.mainTexture = bodySprite.texture;
    }
}