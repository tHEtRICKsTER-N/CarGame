using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class ArcadeBoostPickup : MonoBehaviour
{
    public float boostAmount = 35f;
    public float respawnSeconds = 8f;
    public float pickupRadius = 2.2f;
    public float rotationSpeed = 95f;
    public float bobHeight = 0.28f;
    public float bobSpeed = 2.6f;

    private Transform visualRoot;
    private Renderer[] renderers;
    private Collider triggerCollider;
    private Vector3 baseLocalPosition;
    private bool available = true;

    private void Awake()
    {
        EnsureTrigger();
        EnsureVisual();
    }

    private void Update()
    {
        if (!available || visualRoot == null)
        {
            return;
        }

        visualRoot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        visualRoot.localPosition = baseLocalPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!available)
        {
            return;
        }

        ArcadeBoostController boostController = other.GetComponentInParent<ArcadeBoostController>();
        if (boostController == null && other.attachedRigidbody != null)
        {
            boostController = other.attachedRigidbody.GetComponent<ArcadeBoostController>();
        }

        if (boostController == null)
        {
            return;
        }

        if (boostController.AddBoost(boostAmount) <= 0f)
        {
            return;
        }

        StartCoroutine(RespawnRoutine());
    }

    private void EnsureTrigger()
    {
        triggerCollider = GetComponent<SphereCollider>();
        SphereCollider sphereCollider = (SphereCollider)triggerCollider;
        sphereCollider.isTrigger = true;
        sphereCollider.radius = pickupRadius;
    }

    private void EnsureVisual()
    {
        if (visualRoot != null)
        {
            return;
        }

        GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualObject.name = "Boost Visual";
        visualObject.transform.SetParent(transform, false);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localScale = new Vector3(1.6f, 0.55f, 1.6f);

        Collider visualCollider = visualObject.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        Renderer renderer = visualObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreatePickupMaterial();
        }

        visualRoot = visualObject.transform;
        baseLocalPosition = visualRoot.localPosition;
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private IEnumerator RespawnRoutine()
    {
        SetAvailable(false);
        yield return new WaitForSeconds(respawnSeconds);
        SetAvailable(true);
    }

    private void SetAvailable(bool value)
    {
        available = value;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = value;
        }

        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = value;
            }
        }
    }

    private static Material CreatePickupMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = "Arcade Boost Pickup Material";
        material.color = new Color(0.05f, 0.85f, 1f, 0.95f);
        return material;
    }
}
