using UnityEngine;
using Unity.Netcode;

public class PlayerMovementManager : NetworkBehaviour
{
    [Header("Movement Control")]
    [Tooltip("Tag that disables movement when touched")]
    public string freezeZoneTag = "FreezeZone";

    [Tooltip("Alternative: Layer that disables movement when touched")]
    public LayerMask freezeZoneLayer;

    [Tooltip("Use tag or layer? (true = tag, false = layer)")]
    public bool useTag = true;

    private CharacterController characterController;
    private FirstPersonMovement movementScript;
    private bool isGuiltyPlayer = false;
    private bool isInFreezeZone = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"🚨 [SPAWN] Player spawned at: {transform.position} (Client {NetworkManager.Singleton.LocalClientId})");

        // Only run for local player
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        // Get components immediately
        characterController = GetComponent<CharacterController>();
        movementScript = GetComponent<FirstPersonMovement>();

        // Subscribe to voting events
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.OnPlayerVotedOut += OnPlayerVotedOut;
        }

        // MOVEMENT STARTS ENABLED BY DEFAULT
        EnableMovement();
        Debug.Log($"✅ PlayerMovementManager: Movement enabled at spawn!");
    }

    void EnableMovement()
    {
        if (movementScript != null)
        {
            movementScript.enabled = true;
            Debug.Log("✅ Movement ENABLED - Player can move!");
        }
    }

    void DisableMovement()
    {
        if (movementScript != null)
        {
            movementScript.enabled = false;
            Debug.Log("🔒 Movement DISABLED - Player is frozen!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if we entered a freeze zone
        bool isFreezeZone = false;

        if (useTag)
        {
            // Check by tag
            isFreezeZone = other.CompareTag(freezeZoneTag);
        }
        else
        {
            // Check by layer
            isFreezeZone = ((1 << other.gameObject.layer) & freezeZoneLayer) != 0;
        }

        if (isFreezeZone)
        {
            isInFreezeZone = true;
            DisableMovement();
            Debug.Log($"❄️ Entered freeze zone: {other.gameObject.name}");
        }

        // Guilty zone logic (if you want to keep this)
        if (other.CompareTag("GuiltyZone") && isGuiltyPlayer)
        {
            Debug.Log("🎭 Guilty player entered the guilty zone!");
            EnableMovement();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if we exited a freeze zone
        bool isFreezeZone = false;

        if (useTag)
        {
            // Check by tag
            isFreezeZone = other.CompareTag(freezeZoneTag);
        }
        else
        {
            // Check by layer
            isFreezeZone = ((1 << other.gameObject.layer) & freezeZoneLayer) != 0;
        }

        if (isFreezeZone)
        {
            isInFreezeZone = false;
            EnableMovement();
            Debug.Log($"✅ Exited freeze zone: {other.gameObject.name}");
        }
    }

    void OnPlayerVotedOut(ulong votedOutClientId, bool wasImposter)
    {
        // Check if this is the local player who was voted out and was guilty
        ulong myClientId = NetworkManager.Singleton.LocalClientId;

        if (votedOutClientId == myClientId && wasImposter)
        {
            Debug.Log("🎭 I was voted out and I was GUILTY! Movement stays enabled...");
            isGuiltyPlayer = true;

            // Enable movement if not in freeze zone
            if (!isInFreezeZone)
            {
                EnableMovement();
            }
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.OnPlayerVotedOut -= OnPlayerVotedOut;
        }
    }
}