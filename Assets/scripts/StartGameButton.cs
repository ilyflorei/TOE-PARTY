using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class SceneLoadButton : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Name of the scene to load (e.g., 'playscene', 'arena', 'level2')")]
    public string targetSceneName = "playscene";

    [Header("Player Setup")]
    public GameObject playerPrefab;

    [Header("Lobby Manager (Optional)")]
    [Tooltip("Should this button spawn the LobbyManager? (Only needed for lobby scene)")]
    public bool shouldSpawnLobbyManager = false;

    private static int instanceCount = 0;
    private int myInstanceId;

    void Start()
    {
        myInstanceId = instanceCount++;
        Debug.Log($"✅ SceneLoadButton created: {gameObject.name} (ID: {myInstanceId})");

        if (shouldSpawnLobbyManager && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Debug.Log("🔍 HOST DETECTED - Will spawn LobbyManager in 0.2 seconds...");
            Invoke(nameof(SpawnLobbyManager), 0.2f);
        }
    }

    void OnDestroy()
    {
        Debug.Log($"🗑️ SceneLoadButton destroyed: {gameObject.name} (ID: {myInstanceId})");
    }

    private void SpawnLobbyManager()
    {
        Debug.Log("🔍 Looking for LobbyManager to spawn...");

        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();

        if (lobbyManager != null)
        {
            NetworkObject netObj = lobbyManager.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                if (!netObj.IsSpawned)
                {
                    netObj.Spawn();
                    Debug.Log("✅ LobbyManager NetworkObject spawned!");
                }
                else
                {
                    Debug.Log("ℹ️ LobbyManager already spawned");
                }
            }
            else
            {
                Debug.LogError("❌ LobbyManager has no NetworkObject component!");
            }
        }
        else
        {
            Debug.LogError("❌ No LobbyManager found in scene!");
        }
    }

    public void OnButtonClicked()
    {
        Debug.Log($"=== BUTTON CLICKED: Loading '{targetSceneName}' ===");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager is null!");
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Only host can start!");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned!");
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Target scene name is empty!");
            return;
        }

        // STEP 1: Despawn all lobby player objects
        Debug.Log("Despawning lobby players...");
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
            {
                var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                Debug.Log($"Despawning lobby player for client {clientId}");
                playerObject.Despawn();
                Destroy(playerObject.gameObject);
            }
        }

        // STEP 2: Re-enable player spawning for the play scene
        NetworkManager.Singleton.NetworkConfig.PlayerPrefab = playerPrefab;
        Debug.Log($"✅ Player spawning RE-ENABLED! Prefab: {playerPrefab.name}");

        // STEP 3: Subscribe to scene loaded event
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;

        // STEP 4: Load the target scene
        Debug.Log($"Loading scene: {targetSceneName}...");
        NetworkManager.Singleton.SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }

    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        Debug.Log($"✅ Scene '{sceneName}' loaded! Manually spawning players with spawn points...");

        // Unsubscribe
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;

        // MANUALLY spawn players for all connected clients using spawn manager positions
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                // Check if they already have a player object
                if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject == null)
                {
                    Debug.Log($"Spawning player for client {clientId}");

                    // ✅ GET SPAWN POSITION FROM SPAWN MANAGER
                    Vector3 spawnPosition = Vector3.zero;
                    Quaternion spawnRotation = Quaternion.identity;

                    if (PlayerSpawnManager.Instance != null)
                    {
                        PlayerSpawnManager.Instance.GetNextSpawnPoint(out spawnPosition, out spawnRotation);
                        Debug.Log($"✅ Got spawn position from manager: {spawnPosition}");
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ PlayerSpawnManager not found! Spawning at (0,0,0)");
                    }

                    // Instantiate the player prefab at the spawn position
                    GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);

                    // Get NetworkObject component
                    NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();

                    // Spawn as player object for this client
                    netObj.SpawnAsPlayerObject(clientId, true);

                    Debug.Log($"✅ Player spawned for client {clientId} at {spawnPosition}!");
                }
                else
                {
                    Debug.LogWarning($"Client {clientId} already has a player object!");
                }
            }
        }

        // Destroy this button now that we're in the new scene
        Destroy(gameObject);
    }
}