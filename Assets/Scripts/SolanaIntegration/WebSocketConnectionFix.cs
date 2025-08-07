using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// Fixes WebSocket connection issues in Unity
/// Add this to your DoleroIntegration GameObject
/// </summary>
public class WebSocketConnectionFix : MonoBehaviour
{
    void Awake()
    {
        // Allow insecure WebSocket connections in Unity
        // This is needed for ws:// connections (non-SSL)
        #if UNITY_EDITOR || UNITY_STANDALONE
        System.Environment.SetEnvironmentVariable("MONO_TLS_PROVIDER", "legacy");
        #endif
        
        Debug.Log("WebSocket connection settings configured");
    }
    
    [ContextMenu("Test HTTP Connection First")]
    public void TestHTTPConnection()
    {
        StartCoroutine(TestHTTPConnectionCoroutine());
    }
    
    IEnumerator TestHTTPConnectionCoroutine()
    {
        string testUrl = "http://174.138.42.117:3001/health";
        Debug.Log($"Testing HTTP connection to {testUrl}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ HTTP connection successful: {request.downloadHandler.text}");
                Debug.Log("Server is reachable. WebSocket should work too.");
            }
            else
            {
                Debug.LogError($"❌ HTTP connection failed: {request.error}");
            }
        }
    }
}
