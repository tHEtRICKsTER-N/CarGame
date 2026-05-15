using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class StartFinishTrigger : MonoBehaviour
{
    public ArcadeRaceManager raceManager;

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (raceManager == null)
        {
            return;
        }

        ArcadeRaceParticipant participant = other.GetComponentInParent<ArcadeRaceParticipant>();
        if (participant == null && other.attachedRigidbody != null)
        {
            participant = other.attachedRigidbody.GetComponent<ArcadeRaceParticipant>();
        }

        if (participant != null)
        {
            raceManager.HandleStartFinishCrossing(participant);
        }
    }

    private void EnsureTriggerCollider()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }
    }
}
