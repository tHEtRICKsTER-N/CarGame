using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Graphic))]
public sealed class NeumorphicUIElement : UIBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler
{
    public enum SurfaceMode
    {
        Raised,
        Inset
    }

    public enum RadiusMode
    {
        Relative,
        Pixels
    }

    [Header("Material")]
    public Material sourceMaterial;
    public bool createMaterialInstance = true;

    [Header("Surface")]
    public SurfaceMode mode = SurfaceMode.Raised;
    public Color baseColor = new Color(0.075f, 0.082f, 0.095f, 1f);
    public Color lightColor = new Color(0.18f, 0.20f, 0.23f, 1f);
    public Color darkColor = new Color(0.01f, 0.012f, 0.016f, 1f);

    public RadiusMode radiusMode = RadiusMode.Pixels;
    [Range(0f, 0.5f)]
    public float cornerRadius = 0.16f;
    [Min(0f)]
    public float cornerRadiusPixels = 22f;
    [Range(0f, 0.25f)]
    public float shapePadding = 0.075f;
    [Min(0f)]
    public float shapePaddingPixels = 10f;
    [Range(0.001f, 0.08f)]
    public float edgeSoftness = 0.012f;
    [Min(0.25f)]
    public float edgeSoftnessPixels = 1.5f;
    [Range(0.001f, 0.25f)]
    public float bevelSize = 0.095f;
    [Min(0f)]
    public float bevelSizePixels = 14f;
    [Range(0f, 1f)]
    public float highlightStrength = 0.45f;
    [Range(0f, 1f)]
    public float surfaceShadowStrength = 0.72f;

    [Header("Outer Shadow")]
    public Vector2 shadowOffset = new Vector2(0.028f, -0.028f);
    [Range(0.001f, 0.18f)]
    public float shadowSoftness = 0.06f;
    [Range(-0.08f, 0.08f)]
    public float shadowSpread = 0.015f;
    [Range(0f, 1f)]
    public float darkShadowOpacity = 0.55f;
    [Range(0f, 1f)]
    public float lightShadowOpacity = 0.18f;
    public Vector2 lightDirection = new Vector2(-1f, 1f);

    [Header("Press Animation")]
    public bool enablePressAnimation = true;
    public bool onlyAnimateSelectables = true;
    public bool keepPressedWhenPointerExits;
    [Range(0f, 1f)]
    public float pressedAmount = 1f;
    [Range(0f, 1f)]
    public float previewPressAmount;
    [Range(1f, 40f)]
    public float pressInSpeed = 24f;
    [Range(1f, 40f)]
    public float pressOutSpeed = 16f;
    [Range(0f, 1f)]
    public float pressedShadowFade = 0.78f;
    [Range(0f, 0.25f)]
    public float pressedDarken = 0.07f;
    [Range(0.03f, 0.4f)]
    public float submitPulseSeconds = 0.12f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LightColorId = Shader.PropertyToID("_LightColor");
    private static readonly int DarkColorId = Shader.PropertyToID("_DarkColor");
    private static readonly int CornerRadiusId = Shader.PropertyToID("_CornerRadius");
    private static readonly int ShapePaddingId = Shader.PropertyToID("_ShapePadding");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int BevelSizeId = Shader.PropertyToID("_BevelSize");
    private static readonly int HighlightStrengthId = Shader.PropertyToID("_HighlightStrength");
    private static readonly int SurfaceShadowStrengthId = Shader.PropertyToID("_SurfaceShadowStrength");
    private static readonly int InsetId = Shader.PropertyToID("_Inset");
    private static readonly int PressAmountId = Shader.PropertyToID("_PressAmount");
    private static readonly int PressedShadowFadeId = Shader.PropertyToID("_PressedShadowFade");
    private static readonly int PressedDarkenId = Shader.PropertyToID("_PressedDarken");
    private static readonly int ShadowOffsetId = Shader.PropertyToID("_ShadowOffset");
    private static readonly int ShadowSoftnessId = Shader.PropertyToID("_ShadowSoftness");
    private static readonly int ShadowSpreadId = Shader.PropertyToID("_ShadowSpread");
    private static readonly int ShadowOpacityId = Shader.PropertyToID("_ShadowOpacity");
    private static readonly int LightShadowOpacityId = Shader.PropertyToID("_LightShadowOpacity");
    private static readonly int LightDirectionId = Shader.PropertyToID("_LightDirection");

    private Graphic graphic;
    private Selectable selectable;
    private Material materialInstance;
    private float currentPressAmount;
    private float submitPulseRemaining;
    private bool pointerPressed;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!Application.isPlaying)
        {
            currentPressAmount = previewPressAmount;
        }

        Apply();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        pointerPressed = false;
        currentPressAmount = 0f;
        ReleaseMaterialInstance();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        Apply();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        if (!Application.isPlaying)
        {
            currentPressAmount = previewPressAmount;
        }

        Apply();
    }

    private void Update()
    {
        UpdatePressAnimation();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimatePress())
        {
            return;
        }

        pointerPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerPressed = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!keepPressedWhenPointerExits)
        {
            pointerPressed = false;
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!CanAnimatePress())
        {
            return;
        }

        submitPulseRemaining = submitPulseSeconds;
    }

    public void Apply()
    {
        CacheGraphic();
        Material material = GetTargetMaterial();
        if (material == null)
        {
            return;
        }

        RectTransform rectTransform = transform as RectTransform;
        Vector2 size = rectTransform != null ? rectTransform.rect.size : Vector2.one;
        float width = Mathf.Max(1f, Mathf.Abs(size.x));
        float height = Mathf.Max(1f, Mathf.Abs(size.y));
        float minSize = Mathf.Min(width, height);
        float aspect = width / height;
        float resolvedPadding = Mathf.Clamp(ResolveRelativeValue(shapePadding, shapePaddingPixels, minSize), 0f, 0.25f);
        float innerMinSize = Mathf.Max(1f, minSize * Mathf.Max(0.001f, 1f - resolvedPadding * 2f));
        float resolvedCornerRadius = Mathf.Clamp(ResolveRelativeValue(cornerRadius, cornerRadiusPixels, innerMinSize), 0f, 0.5f);
        float resolvedEdgeSoftness = Mathf.Clamp(ResolveRelativeValue(edgeSoftness, edgeSoftnessPixels, innerMinSize), 0.001f, 0.08f);
        float resolvedBevelSize = Mathf.Clamp(ResolveRelativeValue(bevelSize, bevelSizePixels, innerMinSize), 0.001f, 0.25f);

        material.SetColor(BaseColorId, baseColor);
        material.SetColor(LightColorId, lightColor);
        material.SetColor(DarkColorId, darkColor);
        material.SetFloat(CornerRadiusId, resolvedCornerRadius);
        material.SetFloat(ShapePaddingId, resolvedPadding);
        material.SetFloat(AspectId, aspect);
        material.SetFloat(EdgeSoftnessId, resolvedEdgeSoftness);
        material.SetFloat(BevelSizeId, resolvedBevelSize);
        material.SetFloat(HighlightStrengthId, highlightStrength);
        material.SetFloat(SurfaceShadowStrengthId, surfaceShadowStrength);
        material.SetFloat(InsetId, mode == SurfaceMode.Inset ? 1f : 0f);
        material.SetFloat(PressAmountId, currentPressAmount);
        material.SetFloat(PressedShadowFadeId, pressedShadowFade);
        material.SetFloat(PressedDarkenId, pressedDarken);
        material.SetVector(ShadowOffsetId, new Vector4(shadowOffset.x, shadowOffset.y, 0f, 0f));
        material.SetFloat(ShadowSoftnessId, shadowSoftness);
        material.SetFloat(ShadowSpreadId, shadowSpread);
        material.SetFloat(ShadowOpacityId, darkShadowOpacity);
        material.SetFloat(LightShadowOpacityId, lightShadowOpacity);
        material.SetVector(LightDirectionId, new Vector4(lightDirection.x, lightDirection.y, 0f, 0f));

        if (graphic != null)
        {
            graphic.material = material;
            graphic.SetMaterialDirty();
        }
    }

    public void SetPillCornerRadius()
    {
        radiusMode = RadiusMode.Relative;
        cornerRadius = 0.5f;
        Apply();
    }

    private void CacheGraphic()
    {
        if (graphic == null)
        {
            graphic = GetComponent<Graphic>();
        }

        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }

        if (sourceMaterial == null && graphic != null)
        {
            sourceMaterial = graphic.material;
        }
    }

    private void UpdatePressAnimation()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        float target = GetTargetPressAmount();
        float speed = target > currentPressAmount ? pressInSpeed : pressOutSpeed;
        float deltaTime = Application.isPlaying ? Time.unscaledDeltaTime : 1f;
        float nextPressAmount = Mathf.MoveTowards(currentPressAmount, target, speed * deltaTime);

        if (Mathf.Approximately(nextPressAmount, currentPressAmount))
        {
            return;
        }

        currentPressAmount = nextPressAmount;
        Apply();
    }

    private float GetTargetPressAmount()
    {
        if (!Application.isPlaying)
        {
            return previewPressAmount;
        }

        if (!CanAnimatePress())
        {
            return 0f;
        }

        if (submitPulseRemaining > 0f)
        {
            submitPulseRemaining = Mathf.Max(0f, submitPulseRemaining - Time.unscaledDeltaTime);
            return pressedAmount;
        }

        return pointerPressed ? pressedAmount : 0f;
    }

    private bool CanAnimatePress()
    {
        if (!enablePressAnimation)
        {
            return false;
        }

        CacheGraphic();
        if (!onlyAnimateSelectables)
        {
            return true;
        }

        return selectable != null && selectable.interactable;
    }

    private float ResolveRelativeValue(float relativeValue, float pixelValue, float referenceSize)
    {
        if (radiusMode == RadiusMode.Relative)
        {
            return relativeValue;
        }

        return Mathf.Clamp(pixelValue / Mathf.Max(1f, referenceSize), 0f, 0.5f);
    }

    private Material GetTargetMaterial()
    {
        if (sourceMaterial == null)
        {
            return null;
        }

        if (!createMaterialInstance)
        {
            ReleaseMaterialInstance();
            return sourceMaterial;
        }

        if (materialInstance == null || materialInstance.shader != sourceMaterial.shader)
        {
            ReleaseMaterialInstance();
            materialInstance = new Material(sourceMaterial)
            {
                name = sourceMaterial.name + " (Instance)"
            };
        }

        return materialInstance;
    }

    private void ReleaseMaterialInstance()
    {
        if (materialInstance == null)
        {
            return;
        }

        if (graphic != null && graphic.material == materialInstance)
        {
            graphic.material = sourceMaterial;
        }

        if (Application.isPlaying)
        {
            Destroy(materialInstance);
        }
        else
        {
            DestroyImmediate(materialInstance);
        }

        materialInstance = null;
    }
}
