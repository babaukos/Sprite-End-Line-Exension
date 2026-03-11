using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SpriteMaskReceiver : MonoBehaviour
{
    public SpriteMask parentMask;
    private SpriteRenderer sr;
    private Material internalMat;
    private Shader originalShader;

    void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sharedMaterial != null)
        {
            originalShader = sr.sharedMaterial.shader;
        }
    }

    public void RefreshFromParent()
    {
        if (parentMask == null || sr == null) return;

        if (!transform.IsChildOf(parentMask.transform))
        {
            CleanUp();
            return;
        }

        if (internalMat == null)
        {
            Shader s = Shader.Find("Hidden/Sprite/MaskedSprite");
            if (s != null)
            {
                // Створюємо екземпляр, щоб не міняти шейдер у всіх об'єктів відразу
                internalMat = new Material(s); 
                sr.sharedMaterial = internalMat;
            }
        }

        if (internalMat != null)
        {
            internalMat.SetInt("_StencilRef", parentMask.maskID);
            CompareFunction comp = parentMask.inverted ? CompareFunction.NotEqual : CompareFunction.Equal;
            internalMat.SetInt("_StencilComp", (int)comp);
            internalMat.SetColor("_Color", sr.color);
        }
    }

    void LateUpdate()
    {
        // Перевірка на "виліт" з ієрархії
        if (parentMask == null || !transform.IsChildOf(parentMask.transform))
        {
            CleanUp();
            return;
        }

        if (internalMat != null && sr != null)
        {
            internalMat.SetColor("_Color", sr.color);
            if (sr.sprite != null) internalMat.mainTexture = sr.sprite.texture;
        }
    }

    void CleanUp()
    {
        if (sr != null)
        {
            // Повертаємо дефолтний шейдер (або той, що був)
            sr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        
        // Самознищення компонента (оскільки ми в ExecuteInEditMode, використовуємо DestroyImmediate)
        if (Application.isPlaying) Destroy(this);
        else DestroyImmediate(this);
    }
}