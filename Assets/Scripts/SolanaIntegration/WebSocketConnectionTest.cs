using UnityEngine;

/// <summary>
/// Simple test to verify WebSocket connection to the server
/// </summary>
public class WebSocketConnectionTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool autoTestOnStart = true;
    public bool testConnection = true;
    public bool testMessages = true;
    
    private DoleroWebSocketClient webSocketClient;
    
    void Start()
    {
        if (autoTestOnStart)
        {
            StartCoroutine(RunWebSocketTests());
        }
    }
    
    System.Collections.IEnumerator RunWebSocketTests()
    {
        Debug.Log("🧪 Starting WebSocket connection tests...");
        
        // Wait for WebSocket client to be available
        yield return new WaitForSeconds(1f);
        
        webSocketClient = FindObjectOfType<DoleroWebSocketClient>();
        if (webSocketClient == null)
        {
            Debug.LogError("❌ DoleroWebSocketClient not found!");
            yield break;
        }
        
        Debug.Log("✅ WebSocket client found");
        
        if (testConnection)
        {
            yield return StartCoroutine(TestWebSocketConnection());
        }
        
        if (testMessages && webSocketClient.isConnected)
        {
            yield return StartCoroutine(TestWebSocketMessages());
        }
        
        Debug.Log("🧪 WebSocket connection tests completed!");
    }
    
    System.Collections.IEnumerator TestWebSocketConnection()
    {
        Debug.Log("🔗 Testing WebSocket connection...");
        
        // Subscribe to WebSocket events
        DoleroWebSocketClient.OnConnectionChanged += OnWebSocketConnectionChanged;
        DoleroWebSocketClient.OnMessageReceived += OnWebSocketMessageReceived;
        DoleroWebSocketClient.OnError += OnWebSocketError;
        
        // Connect to server
        webSocketClient.Connect();
        
        // Wait for connection
        yield return new WaitForSeconds(5f);
        
        if (webSocketClient.isConnected)
        {
            Debug.Log("✅ WebSocket connection test passed!");
        }
        else
        {
            Debug.LogError("❌ WebSocket connection test failed!");
        }
    }
    
    System.Collections.IEnumerator TestWebSocketMessages()
    {
        Debug.Log("📤 Testing WebSocket messages...");
        
        // Test 1: Ping
        Debug.Log("🏓 Testing ping...");
        webSocketClient.SendMessage("ping");
        yield return new WaitForSeconds(1f);
        
        // Test 2: Join Game
        Debug.Log("🎮 Testing join game...");
        webSocketClient.JoinGame("test-game-001");
        yield return new WaitForSeconds(1f);
        
        // Test 3: Request Game State
        Debug.Log("📊 Testing game state request...");
        webSocketClient.RequestGameState();
        yield return new WaitForSeconds(1f);
        
        // Test 4: Select Relic
        Debug.Log("🔮 Testing relic selection...");
        webSocketClient.SelectRelic(1);
        yield return new WaitForSeconds(1f);
        
        // Test 5: Place Bet
        Debug.Log("💰 Testing bet placement...");
        webSocketClient.PlaceBet("CALL");
        yield return new WaitForSeconds(1f);
        
        Debug.Log("✅ WebSocket message tests completed!");
    }
    
    void OnWebSocketConnectionChanged(bool connected)
    {
        if (connected)
        {
            Debug.Log("✅ WebSocket connected successfully!");
        }
        else
        {
            Debug.Log("❌ WebSocket disconnected");
        }
    }
    
    void OnWebSocketMessageReceived(string message)
    {
        Debug.Log($"📨 WebSocket message received: {message}");
    }
    
    void OnWebSocketError(string error)
    {
        Debug.LogError($"❌ WebSocket error: {error}");
    }
    
    [ContextMenu("Test WebSocket Connection")]
    public void TestWebSocketConnectionManual()
    {
        StartCoroutine(TestWebSocketConnection());
    }
    
    [ContextMenu("Test WebSocket Messages")]
    public void TestWebSocketMessagesManual()
    {
        if (webSocketClient != null && webSocketClient.isConnected)
        {
            StartCoroutine(TestWebSocketMessages());
        }
        else
        {
            Debug.LogError("❌ WebSocket not connected! Connect first.");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        DoleroWebSocketClient.OnConnectionChanged -= OnWebSocketConnectionChanged;
        DoleroWebSocketClient.OnMessageReceived -= OnWebSocketMessageReceived;
        DoleroWebSocketClient.OnError -= OnWebSocketError;
    }
}
