using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteMask : MonoBehaviour
{
    public int maskID = 1;
    [Range(0f, 1f)]
    public float alphaCutoff = 0.1f;
    public bool inverted = false;

    private SpriteRenderer sr;
    private Material maskMat;

    private void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();
        Shader shader = Shader.Find("Hidden/Sprite/StencilMask");
        if (shader != null)
        {
            // Створюємо екземпляр, щоб не псувати спільний матеріал
            maskMat = new Material(shader);
            sr.sharedMaterial = maskMat;
        }
        UpdateProperties();
    }
    private void OnDisable()
    {
        
    }
    private void OnValidate()
    {
        RefreshHierarchy();
    }
    private void Update()
    {
        RefreshHierarchy();
    }

    private void RefreshHierarchy()
    {
        UpdateProperties();

        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i].gameObject == gameObject) continue;

            SpriteMaskReceiver receiver = allRenderers[i].GetComponent<SpriteMaskReceiver>();
            if (receiver == null)
            {
                receiver = allRenderers[i].gameObject.AddComponent<SpriteMaskReceiver>();
                receiver.hideFlags = HideFlags.HideInInspector;
            }

            receiver.parentMask = this;
            receiver.RefreshFromParent();
        }
    }
    private void UpdateProperties()
    {
        if (maskMat != null)
        {
            maskMat.SetInt("_StencilRef", maskID);
            maskMat.SetFloat("_Cutoff", alphaCutoff);
            
            // ОСЬ ВОНО! Передаємо інверсію в шейдер самої маски
            // 0 = false, 1 = true
            maskMat.SetInt("_Inverted", inverted ? 1 : 0);
        }
    }
}