using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LinePoint
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
public class RibbonMesh : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite startSprite;
    public Sprite bodySprite;
    public Sprite endSprite;

    [Header("Visuals")]
    public float width = 0.5f;
    public Color color = Color.white;
    public float bodyTilingStep = 1.0f;
    public float capLength = 0.2f;
    [Tooltip("If true, subdivisions are spread evenly across the whole line. If false, they are per-segment.")]
    public bool uniformSubdivision = true;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    [Header("Points")]
    public List<LinePoint> linePoints = new List<LinePoint>();

    private Mesh mesh;
    private MeshFilter mf;
    private MeshRenderer mr;
    private List<Vector3> lastFramePoints = new List<Vector3>();

    private void OnEnable()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();

        if (mf.sharedMesh == null || mf.sharedMesh.name != "RibbonMesh_Gen")
        {
            mesh = new Mesh();
            mesh.name = "RibbonMesh_Gen";
            mf.sharedMesh = mesh;
        }
        else
        {
            mesh = mf.sharedMesh;
        }

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
        if (mr == null) mr = GetComponent<MeshRenderer>();
        UpdateSorting();
        if (!Application.isPlaying) UpdateMesh();
    }
#endif

    private void UpdateSorting()
    {
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
        {
            if (Vector3.SqrMagnitude(current[i] - lastFramePoints[i]) > 0.0001f) return true;
        }
        return false;
    }

    private List<Vector3> GetWorldPoints()
    {
        List<Vector3> pts = new List<Vector3>();
        for (int i = 0; i < linePoints.Count; i++)
        {
            pts.Add(linePoints[i].GetPosition());
        }
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

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        List<Color> cols = new List<Color>();

        float totalLength = 0;
        float[] accDist = new float[pts.Count];
        for (int i = 0; i < pts.Count - 1; i++)
        {
            accDist[i] = totalLength;
            totalLength += Vector3.Distance(pts[i], pts[i + 1]);
        }
        accDist[pts.Count - 1] = totalLength;

        // Виправляємо зазори, щоб стиковка була ідеальною
        float startGap = capLength;
        float endGap = capLength;
        float bodyDrawLength = totalLength - startGap - endGap;

        // --- START CAP ---
        if (startSprite != null)
        {
            AddCap(verts, uvs, cols, tris, pts[0], pts[1], startSprite, true);
        }

        // --- BODY ---
        if (bodyDrawLength > 0.001f)
        {
            if (uniformSubdivision)
            {
                int totalSteps = Mathf.Max(2, Mathf.CeilToInt(bodyDrawLength / (bodyTilingStep * 0.5f)));
                float stepDist = bodyDrawLength / totalSteps;
                for (int s = 0; s < totalSteps; s++)
                {
                    float d1 = startGap + (s * stepDist);
                    float d2 = startGap + ((s + 1) * stepDist);
                    // Передаємо чисту дистанцію від початку тіла для UV
                    AddGlobalSegment(verts, uvs, cols, tris, pts, accDist, d1, d2, d1 - startGap, d2 - startGap);
                }
            }
            else
            {
                float currentDist = 0f;
                float effectiveLength = totalLength - endGap;
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    float segDist = Vector3.Distance(pts[i], pts[i + 1]);
                    float segStart = currentDist;
                    float segEnd = currentDist + segDist;

                    if (segEnd <= startGap) { currentDist += segDist; continue; }
                    if (segStart >= effectiveLength) break;

                    float localS = Mathf.Max(0, startGap - segStart);
                    float localE = Mathf.Min(segDist, effectiveLength - segStart);
                    float drawL = localE - localS;

                    int subs = Mathf.Max(1, Mathf.CeilToInt(drawL / (bodyTilingStep * 0.5f)));
                    for (int s = 0; s < subs; s++)
                    {
                        float f1 = (float)s / subs;
                        float f2 = (float)(s + 1) / subs;
                        float t1 = (localS + f1 * drawL) / segDist;
                        float t2 = (localS + f2 * drawL) / segDist;
                        
                        // Глобальна дистанція тіла для тайлінгу без артефактів
                        float uvStart = (currentDist + localS + f1 * drawL) - startGap;
                        float uvEnd = (currentDist + localS + f2 * drawL) - startGap;
                        
                        AddSubdividedSegment(verts, uvs, cols, tris, pts, i, t1, t2, new Vector2(uvStart, uvEnd));
                    }
                    currentDist += segDist;
                }
            }
        }

        // --- HEAD CAP ---
        if (endSprite != null)
        {
            AddCap(verts, uvs, cols, tris, pts[pts.Count - 1], pts[pts.Count - 2], endSprite, false);
        }

        mesh.Clear();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = cols.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        SetupMaterial();
    }

    private void GetLineDataAtDistance(List<Vector3> pts, float[] accDist, float targetDist, out Vector3 pos, out Vector3 miter)
    {
        targetDist = Mathf.Clamp(targetDist, 0, accDist[accDist.Length - 1]);
        int idx = 0;
        for (int i = 0; i < accDist.Length - 1; i++)
        {
            if (targetDist <= accDist[i + 1]) { idx = i; break; }
        }
        float t = (targetDist - accDist[idx]) / (accDist[idx + 1] - accDist[idx]);
        pos = Vector3.Lerp(pts[idx], pts[idx + 1], t);
        Vector3 mS = GetMiterOffset(pts, idx);
        Vector3 mE = GetMiterOffset(pts, idx + 1);
        miter = Vector3.Lerp(mS, mE, t);
    }

    private void AddGlobalSegment(List<Vector3> verts, List<Vector2> uvs, List<Color> cols, List<int> tris,
                                 List<Vector3> pts, float[] accDist, float d1, float d2, float uvD1, float uvD2)
    {
        int baseIdx = verts.Count;
        Vector3 p1, m1, p2, m2;
        GetLineDataAtDistance(pts, accDist, d1, out p1, out m1);
        GetLineDataAtDistance(pts, accDist, d2, out p2, out m2);

        verts.Add(transform.InverseTransformPoint(p1 - m1));
        verts.Add(transform.InverseTransformPoint(p1 + m1));
        verts.Add(transform.InverseTransformPoint(p2 - m2));
        verts.Add(transform.InverseTransformPoint(p2 + m2));

        ApplyBodyUV(uvs, uvD1, uvD2);

        for (int i = 0; i < 4; i++) cols.Add(color);
        tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
        tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
    }

    private void ApplyBodyUV(List<Vector2> uvs, float distStart, float distEnd)
    {
        Rect r = GetSpriteRect(bodySprite);
        float spriteHeight = r.yMax - r.yMin;

        // Рахуємо чистий тайлінг (кількість повторів)
        float v1 = (distStart / bodyTilingStep) % 1.0f;
        float v2 = v1 + (distEnd - distStart) / bodyTilingStep;

        // Якщо v2 більше 1.0, значить ми перетнули межу тайлу.
        // У стандартному спрайт-шейдері ми не можемо просто вийти за 1.0,
        // бо він почне малювати сусідні спрайти з атласу.
        // ТОМУ: ми "затискаємо" v2 в межах одного циклу, але зберігаємо пропорцію.
        
        // v2 - v1 має ЗАВЖДИ дорівнювати (distEnd - distStart) / bodyTilingStep
        // Це і є запорука того, що ширина смуг не зміниться.
        
        float finalV1 = r.yMin + v1 * spriteHeight;
        float finalV2 = r.yMin + v2 * spriteHeight;

        uvs.Add(new Vector2(r.xMin, finalV1));
        uvs.Add(new Vector2(r.xMax, finalV1));
        uvs.Add(new Vector2(r.xMin, finalV2));
        uvs.Add(new Vector2(r.xMax, finalV2));
    }

    private void AddCap(List<Vector3> verts, List<Vector2> uvs, List<Color> cols, List<int> tris, Vector3 p, Vector3 pNext, Sprite s, bool isStart)
    {
        int baseIdx = verts.Count;
        Vector3 dir = (pNext - p).normalized;
        Vector3 norm = new Vector3(-dir.y, dir.x, 0) * (width * 0.5f);

        Vector3 pEdge = p;
        Vector3 pInner = p + dir * capLength;

        verts.Add(transform.InverseTransformPoint(pEdge - norm));
        verts.Add(transform.InverseTransformPoint(pEdge + norm));
        verts.Add(transform.InverseTransformPoint(pInner - norm));
        verts.Add(transform.InverseTransformPoint(pInner + norm));

        Rect r = GetSpriteRect(s);

        if (isStart)
        {
            uvs.Add(new Vector2(r.xMin, r.yMin));
            uvs.Add(new Vector2(r.xMax, r.yMin));
            uvs.Add(new Vector2(r.xMin, r.yMax));
            uvs.Add(new Vector2(r.xMax, r.yMax));
        }
        else
        {
            uvs.Add(new Vector2(r.xMin, r.yMax));
            uvs.Add(new Vector2(r.xMax, r.yMax));
            uvs.Add(new Vector2(r.xMin, r.yMin));
            uvs.Add(new Vector2(r.xMax, r.yMin));
        }

        for (int i = 0; i < 4; i++) cols.Add(color);

        tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
        tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
    }

    private void AddSubdividedSegment(List<Vector3> verts, List<Vector2> uvs, List<Color> cols, List<int> tris,
                                        List<Vector3> pts, int idx, float t1, float t2, Vector2 uvDistRange)
    {
        int baseIdx = verts.Count;
        Vector3 p1 = Vector3.Lerp(pts[idx], pts[idx + 1], t1);
        Vector3 p2 = Vector3.Lerp(pts[idx], pts[idx + 1], t2);
        Vector3 offStart = GetMiterOffset(pts, idx);
        Vector3 offEnd = GetMiterOffset(pts, idx + 1);
        Vector3 offA = Vector3.Lerp(offStart, offEnd, t1);
        Vector3 offB = Vector3.Lerp(offStart, offEnd, t2);

        verts.Add(transform.InverseTransformPoint(p1 - offA));
        verts.Add(transform.InverseTransformPoint(p1 + offA));
        verts.Add(transform.InverseTransformPoint(p2 - offB));
        verts.Add(transform.InverseTransformPoint(p2 + offB));

        ApplyBodyUV(uvs, uvDistRange.x, uvDistRange.y);

        for (int i = 0; i < 4; i++) cols.Add(color);
        tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
        tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
    }

    private Vector3 GetMiterOffset(List<Vector3> pts, int i)
    {
        float hw = width * 0.5f;
        if (i <= 0)
        {
            Vector3 d = (pts[1] - pts[0]).normalized;
            return new Vector3(-d.y, d.x, 0f) * hw;
        }
        if (i >= pts.Count - 1)
        {
            Vector3 d = (pts[i] - pts[i - 1]).normalized;
            return new Vector3(-d.y, d.x, 0f) * hw;
        }
        Vector3 dA = (pts[i] - pts[i - 1]).normalized;
        Vector3 dB = (pts[i + 1] - pts[i]).normalized;
        Vector3 nA = new Vector3(-dA.y, dA.x, 0f);
        Vector3 nB = new Vector3(-dB.y, dB.x, 0f);
        Vector3 miter = (nA + nB);
        if (miter.sqrMagnitude < 0.0001f) return nA * hw;
        miter.Normalize();
        float dot = Vector3.Dot(miter, nA);
        if (Mathf.Abs(dot) < 0.2f) return nA * hw;
        float mL = Mathf.Clamp(hw / dot, -hw * 2f, hw * 2f);
        return miter * mL;
    }

    private Rect GetSpriteRect(Sprite s)
    {
        if (s == null) return new Rect(0, 0, 1, 1);
        return new Rect(s.textureRect.x / s.texture.width, s.textureRect.y / s.texture.height,
                        s.textureRect.width / s.texture.width, s.textureRect.height / s.texture.height);
    }

    private void SetupMaterial()
    {
        if (mr == null) mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null) mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        if (bodySprite != null) mr.sharedMaterial.mainTexture = bodySprite.texture;
    }
}