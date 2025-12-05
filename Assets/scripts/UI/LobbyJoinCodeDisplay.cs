using UnityEngine;
using TMPro;
using System.Collections;

public class LobbyJoinCodeDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI joinCodeText;

    [Header("Settings")]
    public string defaultText = "Join Code: Waiting...";
    public string codePrefix = "Join Code: ";

    [Header("Auto-Update")]
    public bool autoRefresh = true;
    public float refreshInterval = 0.5f;

    int maxRetries = 20;
    int retryCount = 0;

    private void Start()
    {
        Debug.Log("🟢 LobbyJoinCodeDisplay START called");
        Debug.Log($"🟢 GameObject: {gameObject.name}");
        Debug.Log($"🟢 GameObject active: {gameObject.activeInHierarchy}");
        Debug.Log($"🟢 joinCodeText assigned: {joinCodeText != null}");

        if (joinCodeText == null)
        {
            Debug.LogError("❌ joinCodeText is NULL! Assign it in Inspector!");
            return;
        }

        // Set default text
        joinCodeText.text = defaultText;
        Debug.Log($"✅ Set default text: {defaultText}");

        // Check for existing join code immediately
        CheckForJoinCode();

        // Start auto-refresh coroutine if enabled
        if (autoRefresh)
        {
            StartCoroutine(AutoRefreshJoinCode());
        }

        // Subscribe to join code generation event
        if (RelayManager.Instance != null)
        {
            RelayManager.Instance.OnJoinCodeGenerated += OnJoinCodeReceived;
            Debug.Log("✅ Subscribed to OnJoinCodeGenerated event");
        }
        else
        {
            Debug.LogWarning("⚠️ RelayManager.Instance is NULL at Start!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from event to prevent memory leaks
        if (RelayManager.Instance != null)
        {
            RelayManager.Instance.OnJoinCodeGenerated -= OnJoinCodeReceived;
        }
    }

    private void CheckForJoinCode()
    {
        Debug.Log("🔍 CheckForJoinCode called");

        if (RelayManager.Instance == null)
        {
            Debug.LogWarning("⚠️ RelayManager.Instance is NULL!");
            return;
        }

        Debug.Log($"✅ RelayManager found!");

        string joinCode = RelayManager.Instance.JoinCode;
        Debug.Log($"🔑 Retrieved join code: '{joinCode}'");
        Debug.Log($"🔑 Is null or empty: {string.IsNullOrEmpty(joinCode)}");

        if (!string.IsNullOrEmpty(joinCode))
        {
            DisplayJoinCode(joinCode);
        }
        else
        {
            Debug.Log("⏳ Join code not ready yet, will keep checking...");
        }
    }

    private IEnumerator AutoRefreshJoinCode()
    {
        if (RelayManager.Instance != null)
        {
            string joinCode = RelayManager.Instance.JoinCode;
            if (!string.IsNullOrEmpty(joinCode))
            {
                DisplayJoinCode(joinCode);
                Debug.Log($"🔄 Auto-refreshed join code: {joinCode}");
                yield break; // Stop checking once we have the code
            }
        }
        
        if(retryCount >= maxRetries)
        {
            Debug.LogError("Failed to get instance after maximum attempts");
            yield break;
        }
        ++retryCount;
        Debug.Log("Failed to get join code or RelayManager instance, retrying " + retryCount + "/" + maxRetries);
        yield return new WaitForSeconds(refreshInterval);
    }

    private void OnJoinCodeReceived(string joinCode)
    {
        Debug.Log($"🎉 OnJoinCodeReceived event fired! Code: {joinCode}");
        DisplayJoinCode(joinCode);
    }

    private void DisplayJoinCode(string code)
    {
        if (joinCodeText == null)
        {
            Debug.LogError("❌ Cannot display - joinCodeText is NULL!");
            return;
        }

        string displayText = codePrefix + code;
        joinCodeText.text = displayText;
        Debug.Log($"✅ Join code displayed in UI: {displayText}");
        Debug.Log($"✅ Text component text is now: '{joinCodeText.text}'");
    }

    // Manual refresh button for testing
    public void ManualRefresh()
    {
        Debug.Log("🔄 Manual refresh triggered");
        CheckForJoinCode();
    }
}