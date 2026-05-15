using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ArcadeBoostController))]
public sealed class ArcadeBoostVfx : MonoBehaviour
{
    private const string ScreenVfxName = "Arcade Screen Hyperdrive VFX";
    private const string NitroVfxName = "Arcade Car Nitro VFX";

    [Header("Catalog")]
    public string pickupCatalogResourcePath = "ArcadePickupCatalog";
    public ArcadePickupCatalog pickupCatalog;

    [Header("Screen Hyperdrive")]
    public bool useScreenHyperdrive = true;
    public GameObject screenHyperdrivePrefab;
    public Vector3 screenLocalPosition = new Vector3(0f, 0f, 3f);
    public Vector3 screenLocalEulerAngles;
    public Vector3 screenLocalScale = new Vector3(2.6f, 2.6f, 2.6f);

    [Header("Car Nitro")]
    public bool useCarNitro = true;
    public GameObject carNitroPrefab;
    public Vector3 nitroLocalPosition = new Vector3(0f, 0.45f, -2.05f);
    public Vector3 nitroLocalEulerAngles = new Vector3(0f, 180f, 0f);
    public Vector3 nitroLocalScale = Vector3.one;

    private ArcadeBoostController boostController;
    private GameObject screenInstance;
    private GameObject nitroInstance;
    private Camera cachedCamera;
    private bool vfxActive;
    private bool initialized;

    public void Initialize(ArcadeBoostController controller)
    {
        boostController = controller != null ? controller : GetComponent<ArcadeBoostController>();
        LoadCatalog();
        EnsureInstances();
        SetVfxActive(false, true);
        initialized = true;
    }

    private void Awake()
    {
        boostController = GetComponent<ArcadeBoostController>();
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            Initialize(boostController);
        }

        EnsureInstances();
        UpdateScreenParent();

        bool shouldPlay = boostController != null && boostController.IsBoosting;
        SetVfxActive(shouldPlay, false);
    }

    private void OnDisable()
    {
        SetVfxActive(false, true);
    }

    private void OnDestroy()
    {
        DestroyInstance(screenInstance);
        DestroyInstance(nitroInstance);
    }

    private void LoadCatalog()
    {
        if (pickupCatalog == null)
        {
            pickupCatalog = ArcadePickupCatalog.LoadDefault(pickupCatalogResourcePath);
        }

        if (pickupCatalog == null)
        {
            return;
        }

        if (screenHyperdrivePrefab == null)
        {
            screenHyperdrivePrefab = pickupCatalog.screenHyperdriveVfxPrefab;
        }

        if (carNitroPrefab == null)
        {
            carNitroPrefab = pickupCatalog.carNitroVfxPrefab;
        }
    }

    private void EnsureInstances()
    {
        if (useScreenHyperdrive && screenHyperdrivePrefab != null && screenInstance == null)
        {
            screenInstance = Instantiate(screenHyperdrivePrefab);
            screenInstance.name = ScreenVfxName;
            UpdateScreenParent();
        }

        if (useCarNitro && carNitroPrefab != null && nitroInstance == null)
        {
            nitroInstance = Instantiate(carNitroPrefab, transform);
            nitroInstance.name = NitroVfxName;
            nitroInstance.transform.localPosition = nitroLocalPosition;
            nitroInstance.transform.localRotation = Quaternion.Euler(nitroLocalEulerAngles);
            nitroInstance.transform.localScale = nitroLocalScale;
        }
    }

    private void UpdateScreenParent()
    {
        if (screenInstance == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera != cachedCamera)
        {
            cachedCamera = mainCamera;
            screenInstance.transform.SetParent(cachedCamera.transform, false);
        }
        else if (cachedCamera == null && screenInstance.transform.parent == null)
        {
            screenInstance.transform.SetParent(transform, false);
        }

        screenInstance.transform.localPosition = screenLocalPosition;
        screenInstance.transform.localRotation = Quaternion.Euler(screenLocalEulerAngles);
        screenInstance.transform.localScale = screenLocalScale;
    }

    private void SetVfxActive(bool active, bool force)
    {
        if (!force && vfxActive == active)
        {
            return;
        }

        vfxActive = active;
        SetInstanceActive(screenInstance, active);
        SetInstanceActive(nitroInstance, active);
    }

    private static void SetInstanceActive(GameObject instance, bool active)
    {
        if (instance == null)
        {
            return;
        }

        if (active && !instance.activeSelf)
        {
            instance.SetActive(true);
        }

        ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            if (active)
            {
                particles[i].Clear(true);
                particles[i].Play(true);
            }
            else
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (!active && instance.activeSelf)
        {
            instance.SetActive(false);
        }
    }

    private static void DestroyInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Destroy(instance);
    }
}
