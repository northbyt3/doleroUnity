using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Simple standalone WebSocket test client for connecting to DOLERO server
/// Add this to any GameObject in your scene to test the connection
/// </summary>
public class WebSocketTestClient : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverAddress = "174.138.42.117";
    public int serverPort = 3002;
    public bool autoConnect = true;
    
    [Header("Test Settings")]
    public bool testPing = true;
    public bool testJoinGame = true;
    public bool testGameState = true;
    
    [Header("Status")]
    public bool isConnected = false;
    public string connectionStatus = "Disconnected";
    public string lastMessage = "";
    public string lastError = "";
    
    // WebSocket instance
    private WebSocketSharp.WebSocket websocket;
    private Coroutine connectionCoroutine;
    
    // Events
    public static event Action<bool> OnConnectionChanged;
    public static event Action<string> OnMessageReceived;
    public static event Action<string> OnError;
    
    void Start()
    {
        if (autoConnect)
        {
            Connect();
        }
    }
    
    /// <summary>
    /// Connect to the WebSocket server
    /// </summary>
    [ContextMenu("Connect to Server")]
    public void Connect()
    {
        if (isConnected)
        {
            Debug.Log("Already connected!");
            return;
        }
        
        if (connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
        }
        
        connectionCoroutine = StartCoroutine(ConnectCoroutine());
    }
    
    /// <summary>
    /// Disconnect from the server
    /// </summary>
    [ContextMenu("Disconnect from Server")]
    public void Disconnect()
    {
        if (websocket != null)
        {
            websocket.Close();
            websocket = null;
        }
        
        isConnected = false;
        connectionStatus = "Disconnected";
        OnConnectionChanged?.Invoke(false);
        
        Debug.Log("Disconnected from server");
    }
    
    /// <summary>
    /// Send a ping message
    /// </summary>
    [ContextMenu("Send Ping")]
    public void SendPing()
    {
        SendMessage("ping");
    }
    
    /// <summary>
    /// Join a test game
    /// </summary>
    [ContextMenu("Join Test Game")]
    public void JoinTestGame()
    {
        SendMessage("joinGame", new { gameId = "test-game-001" });
    }
    
    /// <summary>
    /// Request game state
    /// </summary>
    [ContextMenu("Request Game State")]
    public void RequestGameState()
    {
        SendMessage("getGameState");
    }
    
    /// <summary>
    /// Run all tests
    /// </summary>
    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        StartCoroutine(RunTestsCoroutine());
    }
    
    IEnumerator ConnectCoroutine()
    {
        Debug.Log($"🔗 Connecting to WebSocket server at ws://{serverAddress}:{serverPort}");
        connectionStatus = "Connecting...";
        
        bool connectionFailed = false;
        string errorMessage = "";
        
        try
        {
            // Create WebSocket connection
            string wsUrl = $"ws://{serverAddress}:{serverPort}";
            websocket = new WebSocketSharp.WebSocket(wsUrl);
            
            // Configure for insecure connections
            websocket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            websocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.None;
            websocket.SslConfiguration.CheckCertificateRevocation = false;
            websocket.Log.Level = WebSocketSharp.LogLevel.Error;
            
            // Subscribe to events
            websocket.OnOpen += OnWebSocketOpen;
            websocket.OnMessage += OnWebSocketMessage;
            websocket.OnClose += OnWebSocketClose;
            websocket.OnError += OnWebSocketError;
            
            // Connect
            websocket.Connect();
        }
        catch (Exception e)
        {
            connectionFailed = true;
            errorMessage = e.Message;
        }
        
        // Wait for connection (outside try-catch)
        float timeout = 10f;
        float elapsed = 0f;
        
        while (!isConnected && elapsed < timeout && !connectionFailed)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (connectionFailed)
        {
            Debug.LogError($"❌ WebSocket connection failed: {errorMessage}");
            connectionStatus = $"Connection failed: {errorMessage}";
            lastError = errorMessage;
            OnConnectionChanged?.Invoke(false);
            OnError?.Invoke(errorMessage);
        }
        else if (!isConnected)
        {
            Debug.LogError("❌ WebSocket connection timeout");
            connectionStatus = "Connection timeout";
            lastError = "Connection timeout";
            OnConnectionChanged?.Invoke(false);
        }
    }
    
    void OnWebSocketOpen(object sender, EventArgs e)
    {
        Debug.Log("✅ WebSocket connected!");
        isConnected = true;
        connectionStatus = "Connected";
        lastError = "";
        OnConnectionChanged?.Invoke(true);
        
        // Send initial connection message
        SendMessage("connect", new
        {
            clientType = "UnityTestClient",
            platform = Application.platform.ToString(),
            version = Application.version,
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
    
    void OnWebSocketMessage(object sender, WebSocketSharp.MessageEventArgs e)
    {
        string message = e.Data;
        Debug.Log($"📨 Received: {message}");
        lastMessage = message;
        OnMessageReceived?.Invoke(message);
    }
    
    void OnWebSocketClose(object sender, WebSocketSharp.CloseEventArgs e)
    {
        Debug.Log($"🔌 WebSocket closed: {e.Reason}");
        isConnected = false;
        connectionStatus = "Disconnected";
        OnConnectionChanged?.Invoke(false);
    }
    
    void OnWebSocketError(object sender, WebSocketSharp.ErrorEventArgs e)
    {
        Debug.LogError($"❌ WebSocket error: {e.Message}");
        lastError = e.Message;
        OnError?.Invoke(e.Message);
    }
    
    /// <summary>
    /// Send a message to the server
    /// </summary>
    public void SendMessage(string type, object data = null)
    {
        if (!isConnected)
        {
            Debug.LogError("❌ Not connected to server!");
            return;
        }
        
        try
        {
            var message = new
            {
                type = type,
                data = data,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            
            string jsonMessage = JsonConvert.SerializeObject(message);
            websocket.Send(jsonMessage);
            
            Debug.Log($"📤 Sent: {jsonMessage}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to send message: {e.Message}");
            lastError = e.Message;
        }
    }
    
    /// <summary>
    /// Send a simple text message
    /// </summary>
    public void SendMessage(string message)
    {
        if (!isConnected)
        {
            Debug.LogError("❌ Not connected to server!");
            return;
        }
        
        try
        {
            websocket.Send(message);
            Debug.Log($"📤 Sent: {message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to send message: {e.Message}");
            lastError = e.Message;
        }
    }
    
    IEnumerator RunTestsCoroutine()
    {
        Debug.Log("🧪 Running WebSocket tests...");
        
        // Wait for connection
        if (!isConnected)
        {
            Debug.Log("Waiting for connection...");
            yield return new WaitForSeconds(2f);
            
            if (!isConnected)
            {
                Debug.LogError("❌ Not connected! Cannot run tests.");
                yield break;
            }
        }
        
        // Test 1: Ping
        if (testPing)
        {
            Debug.Log("🏓 Testing ping...");
            SendPing();
            yield return new WaitForSeconds(1f);
        }
        
        // Test 2: Join Game
        if (testJoinGame)
        {
            Debug.Log("🎮 Testing join game...");
            JoinTestGame();
            yield return new WaitForSeconds(1f);
        }
        
        // Test 3: Request Game State
        if (testGameState)
        {
            Debug.Log("📊 Testing game state request...");
            RequestGameState();
            yield return new WaitForSeconds(1f);
        }
        
        Debug.Log("✅ WebSocket tests completed!");
    }
    
    void OnDestroy()
    {
        if (websocket != null)
        {
            websocket.Close();
        }
    }
    
    void OnApplicationQuit()
    {
        Disconnect();
    }
}
