using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Simple connection test to verify server communication
/// </summary>
public class ConnectionTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool autoTestOnStart = true;
    public string serverAddress = "174.138.42.117";
    public int httpPort = 3001;
    
    void Start()
    {
        if (autoTestOnStart)
        {
            StartCoroutine(TestConnectionCoroutine());
        }
    }
    
    [ContextMenu("Test Connection")]
    public void TestConnection()
    {
        StartCoroutine(TestConnectionCoroutine());
    }
    
    IEnumerator TestConnectionCoroutine()
    {
        Debug.Log("🧪 Testing connection to DOLERO server...");
        
        // Test 1: Basic HTTP connection
        string healthUrl = $"http://{serverAddress}:{httpPort}/health";
        Debug.Log($"📡 Testing HTTP connection to {healthUrl}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(healthUrl))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ HTTP connection successful!");
                Debug.Log($"📄 Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"❌ HTTP connection failed: {request.error}");
                Debug.LogError($"📄 Response: {request.downloadHandler.text}");
            }
        }
        
        // Test 2: API endpoint
        string apiUrl = $"http://{serverAddress}:{httpPort}/api/connect";
        Debug.Log($"📡 Testing API endpoint: {apiUrl}");
        
        var connectData = new
        {
            clientType = "Unity",
            platform = Application.platform.ToString(),
            version = Application.version
        };
        
        string jsonData = JsonUtility.ToJson(connectData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ API connection successful!");
                Debug.Log($"📄 Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"❌ API connection failed: {request.error}");
                Debug.LogError($"📄 Response: {request.downloadHandler.text}");
            }
        }
        
        Debug.Log("🧪 Connection test completed!");
    }
}
