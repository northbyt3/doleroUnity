using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using WebSocketSharp;

/// <summary>
/// WebSocket Test Client using the same clean approach as DOLEROMatchmaking.cs
/// Simple test client for connecting to DOLERO server
/// </summary>
public class WebSocketTEST : MonoBehaviour
{
    [Header("Server Configuration")]
    public string serverAddress = "174.138.42.117";
    public int serverPort = 3002;
    public bool autoConnect = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // WebSocket connection
    private WebSocket webSocket;
    private bool isConnected = false;
    private bool isConnectionEstablished = false;
    
    // Test state
    private string lastMessage = "";
    private string lastError = "";
    private int messageCount = 0;
    
    // Events
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnMessageReceived;
    public event Action<string> OnError;
    
    // Message classes
    [System.Serializable]
    public class WebSocketMessage
    {
        public string type;
        public long timestamp;
    }
    
    [System.Serializable]
    public class TestMessage : WebSocketMessage
    {
        public string message;
        public object data;
    }
    
    void Start()
    {
        LogDebug("🚀 WebSocketTEST component started");
        LogDebug("🚀 AutoConnect setting: " + autoConnect);
        LogDebug("🚀 Server Address: " + serverAddress);
        LogDebug("🚀 Server Port: " + serverPort);
        
        if (autoConnect)
        {
            LogDebug("🚀 Auto-connecting to server...");
            Connect();
        }
        else
        {
            LogDebug("🚀 Auto-connect disabled, waiting for manual connection");
        }
    }
    
    void OnDestroy()
    {
        Disconnect();
    }
    
    /// <summary>
    /// Initialize WebSocket connection
    /// </summary>
    private void InitializeWebSocket()
    {
        try
        {
            // Apply security fixes first
            ApplySecurityFixes();
            
            string targetUrl = $"ws://{serverAddress}:{serverPort}";
            
            LogDebug("🔧 Initializing WebSocket connection to: " + targetUrl);
            LogDebug("🔧 Server Address: " + serverAddress);
            LogDebug("🔧 Server Port: " + serverPort);
            
            webSocket = new WebSocket(targetUrl);
            
            // Configure WebSocket for insecure connections
            LogDebug("🔧 Configuring WebSocket settings...");
            // Note: SslConfiguration is not available for ws:// connections
            // Only configure logging for insecure connections
            webSocket.Log.Level = WebSocketSharp.LogLevel.Debug; // Enable detailed logging
            
            LogDebug("🔧 WebSocket settings configured successfully");
            
            // Subscribe to events
            LogDebug("🔧 Subscribing to WebSocket events...");
            webSocket.OnOpen += OnWebSocketOpen;
            webSocket.OnMessage += OnWebSocketMessage;
            webSocket.OnClose += OnWebSocketClose;
            webSocket.OnError += OnWebSocketError;
            
            LogDebug("🔧 Events subscribed successfully");
            
            // Attempt connection
            LogDebug("🔧 Attempting to connect...");
            webSocket.Connect();
            
            LogDebug("🔧 Connect() called successfully");
        }
        catch (Exception e)
        {
            LogError("❌ Failed to initialize WebSocket: " + e.Message);
            LogError("❌ Exception Type: " + e.GetType().Name);
            LogError("❌ Stack Trace: " + e.StackTrace);
        }
    }
    
    /// <summary>
    /// Apply security fixes to allow ws:// connections
    /// </summary>
    private void ApplySecurityFixes()
    {
        try
        {
            LogDebug("🔧 Applying security fixes...");
            
            // Method 1: Set Unity's HTTP settings
            #if UNITY_EDITOR
            UnityEditor.PlayerSettings.insecureHttpOption = UnityEditor.InsecureHttpOption.AlwaysAllowed;
            LogDebug("✅ Unity HTTP settings updated");
            #endif
            
            // Method 2: Set environment variables
            System.Environment.SetEnvironmentVariable("UNITY_DISABLE_GRAPHICS_API_VALIDATION", "1");
            System.Environment.SetEnvironmentVariable("UNITY_DISABLE_GRAPHICS_JOBS", "1");
            LogDebug("✅ Environment variables set");
            
            // Method 3: Configure .NET security
            ConfigureDotNetSecurity();
            
            LogDebug("✅ Security fixes applied successfully!");
        }
        catch (Exception e)
        {
            LogError("❌ Failed to apply security fixes: " + e.Message);
        }
    }
    
    /// <summary>
    /// Configure .NET security settings
    /// </summary>
    private void ConfigureDotNetSecurity()
    {
        try
        {
            // Allow all SSL/TLS protocols
            System.Net.ServicePointManager.SecurityProtocol = 
                System.Net.SecurityProtocolType.Tls | 
                System.Net.SecurityProtocolType.Tls11 | 
                System.Net.SecurityProtocolType.Tls12;
            
            // Disable certificate validation for development
            System.Net.ServicePointManager.ServerCertificateValidationCallback = 
                (sender, certificate, chain, sslPolicyErrors) => true;
            
            LogDebug("✅ .NET security configured");
        }
        catch (Exception e)
        {
            LogWarning("⚠️ .NET security configuration failed: " + e.Message);
        }
    }
    
    /// <summary>
    /// Handle WebSocket connection opened
    /// </summary>
    private void OnWebSocketOpen(object sender, EventArgs e)
    {
        LogDebug("🎉 WebSocket connection opened!");
        LogDebug("🎉 Sender: " + (sender?.GetType().Name ?? "null"));
        LogDebug("🎉 EventArgs: " + (e?.GetType().Name ?? "null"));
        
        isConnected = true;
        isConnectionEstablished = true;
        lastError = "";
        
        LogDebug("✅ Connection state updated: isConnected=true, isConnectionEstablished=true");
        
        // Send initial connection message
        LogDebug("📤 Sending initial connection message...");
        SendTestMessage("connect", new
        {
            clientType = "UnityTestClient",
            platform = Application.platform.ToString(),
            version = Application.version,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
        
        LogDebug("✅ Initial connection message sent");
        OnConnected?.Invoke();
        LogDebug("✅ OnConnected event invoked");
    }
    
    /// <summary>
    /// Handle WebSocket messages
    /// </summary>
    private void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        try
        {
            messageCount++;
            lastMessage = e.Data;
            
            LogDebug($"Received message [{messageCount}]: {e.Data}");
            
            var message = JsonConvert.DeserializeObject<WebSocketMessage>(e.Data);
            HandleMessage(message, e.Data);
            
            OnMessageReceived?.Invoke(e.Data);
        }
        catch (Exception ex)
        {
            LogError("Failed to parse WebSocket message: " + ex.Message);
        }
    }
    
    /// <summary>
    /// Handle WebSocket connection closed
    /// </summary>
    private void OnWebSocketClose(object sender, CloseEventArgs e)
    {
        LogDebug("🔌 WebSocket connection closed!");
        LogDebug("🔌 Sender: " + (sender?.GetType().Name ?? "null"));
        LogDebug("🔌 CloseEventArgs: " + (e?.GetType().Name ?? "null"));
        LogDebug("🔌 Reason: " + (e?.Reason ?? "null"));
        LogDebug("🔌 Code: " + (e?.Code.ToString() ?? "null"));
        LogDebug("🔌 WasClean: " + (e?.WasClean.ToString() ?? "null"));
        
        isConnected = false;
        isConnectionEstablished = false;
        
        LogDebug("❌ Connection state updated: isConnected=false, isConnectionEstablished=false");
        
        OnDisconnected?.Invoke();
        LogDebug("✅ OnDisconnected event invoked");
    }
    
    /// <summary>
    /// Handle WebSocket errors
    /// </summary>
    private void OnWebSocketError(object sender, ErrorEventArgs e)
    {
        LogError("❌ WebSocket error occurred!");
        LogError("❌ Sender: " + (sender?.GetType().Name ?? "null"));
        LogError("❌ ErrorEventArgs: " + (e?.GetType().Name ?? "null"));
        LogError("❌ Error Message: " + (e?.Message ?? "null"));
        LogError("❌ Exception: " + (e?.Exception?.GetType().Name ?? "null"));
        
        if (e?.Exception != null)
        {
            LogError("❌ Exception Message: " + e.Exception.Message);
            LogError("❌ Exception Stack Trace: " + e.Exception.StackTrace);
        }
        
        lastError = e?.Message ?? "Unknown error";
        OnError?.Invoke(lastError);
        LogError("✅ OnError event invoked with: " + lastError);
    }
    
    /// <summary>
    /// Handle incoming messages based on type
    /// </summary>
    private void HandleMessage(WebSocketMessage message, string rawData)
    {
        LogDebug("Processing message type: " + message.type);
        
        switch (message.type)
        {
            case "pong":
                LogDebug("🏓 Pong received from server");
                break;
                
            case "gameState":
                LogDebug("📊 Game state update received");
                break;
                
            case "error":
                LogDebug("❌ Error message received");
                break;
                
            default:
                LogDebug("📨 Message received: " + message.type);
                break;
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
            LogDebug("Already connected to server");
            return;
        }
        
        InitializeWebSocket();
    }
    
    /// <summary>
    /// Disconnect from server
    /// </summary>
    [ContextMenu("Disconnect from Server")]
    public void Disconnect()
    {
        if (webSocket != null)
        {
            webSocket.Close();
            webSocket = null;
        }
        
        isConnected = false;
        isConnectionEstablished = false;
        
        LogDebug("Disconnected from server");
    }
    
    /// <summary>
    /// Send a ping message
    /// </summary>
    [ContextMenu("Send Ping")]
    public void SendPing()
    {
        SendTestMessage("ping");
    }
    
    /// <summary>
    /// Join a test game
    /// </summary>
    [ContextMenu("Join Test Game")]
    public void JoinTestGame()
    {
        SendTestMessage("joinGame", new { gameId = "test-game-001" });
    }
    
    /// <summary>
    /// Request game state
    /// </summary>
    [ContextMenu("Request Game State")]
    public void RequestGameState()
    {
        SendTestMessage("getGameState");
    }
    
    /// <summary>
    /// Send a test message
    /// </summary>
    public void SendTestMessage(string type, object data = null)
    {
        if (!isConnected)
        {
            LogError("Cannot send message: not connected to server");
            return;
        }
        
        try
        {
            var message = new TestMessage
            {
                type = type,
                message = type,
                data = data,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            SendMessage(message);
            LogDebug($"Sent test message: {type}");
        }
        catch (Exception e)
        {
            LogError("Failed to send test message: " + e.Message);
        }
    }
    
    /// <summary>
    /// Send a simple text message
    /// </summary>
    public void SendTestMessage(string message)
    {
        if (!isConnected)
        {
            LogError("Cannot send message: not connected to server");
            return;
        }
        
        try
        {
            webSocket.Send(message);
            LogDebug($"Sent simple message: {message}");
        }
        catch (Exception e)
        {
            LogError("Failed to send simple message: " + e.Message);
        }
    }
    
    /// <summary>
    /// Run all tests
    /// </summary>
    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        StartCoroutine(RunTestsCoroutine());
    }
    
    /// <summary>
    /// Test sequence: Connect, wait for connection established, then run tests
    /// </summary>
    public void TestConnectionSequence()
    {
        LogDebug("Starting test connection sequence...");
        Connect();
        
        // Wait for connection to establish, then run tests
        StartCoroutine(TestSequenceCoroutine());
    }
    
    private IEnumerator TestSequenceCoroutine()
    {
        // Wait for connection to be established
        float timeout = 10f;
        float elapsed = 0f;
        
        while (!isConnectionEstablished && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (isConnectionEstablished)
        {
            LogDebug("Connection established, running tests...");
            
            // Test 1: Ping
            LogDebug("🏓 Testing ping...");
            SendPing();
            yield return new WaitForSeconds(1f);
            
            // Test 2: Join Game
            LogDebug("🎮 Testing join game...");
            JoinTestGame();
            yield return new WaitForSeconds(1f);
            
            // Test 3: Request Game State
            LogDebug("📊 Testing game state request...");
            RequestGameState();
            yield return new WaitForSeconds(1f);
            
            LogDebug("✅ All tests completed!");
        }
        else
        {
            LogError("Connection not established within timeout period");
        }
    }
    
    IEnumerator RunTestsCoroutine()
    {
        LogDebug("🧪 Running WebSocket tests...");
        
        // Wait for connection
        if (!isConnected)
        {
            LogDebug("Waiting for connection...");
            yield return new WaitForSeconds(2f);
            
            if (!isConnected)
            {
                LogError("❌ Not connected! Cannot run tests.");
                yield break;
            }
        }
        
        // Test 1: Ping
        LogDebug("🏓 Testing ping...");
        SendPing();
        yield return new WaitForSeconds(1f);
        
        // Test 2: Join Game
        LogDebug("🎮 Testing join game...");
        JoinTestGame();
        yield return new WaitForSeconds(1f);
        
        // Test 3: Request Game State
        LogDebug("📊 Testing game state request...");
        RequestGameState();
        yield return new WaitForSeconds(1f);
        
        LogDebug("✅ WebSocket tests completed!");
    }
    
    /// <summary>
    /// Send message to server
    /// </summary>
    private void SendMessage(object message)
    {
        if (!isConnected)
        {
            LogError("Cannot send message: not connected to server");
            return;
        }
        
        try
        {
            string jsonMessage = JsonConvert.SerializeObject(message);
            webSocket.Send(jsonMessage);
            LogDebug("Sent message: " + jsonMessage);
        }
        catch (Exception e)
        {
            LogError("Failed to send message: " + e.Message);
        }
    }
    
    // Public properties for status checking
    public bool IsConnected => isConnected;
    public bool IsConnectionEstablished => isConnectionEstablished;
    public string LastMessage => lastMessage;
    public string LastError => lastError;
    public int MessageCount => messageCount;
    
    /// <summary>
    /// Get detailed connection status for debugging
    /// </summary>
    public string GetConnectionStatus()
    {
        return $"Connected: {isConnected}, ConnectionEstablished: {isConnectionEstablished}, Messages: {messageCount}, LastError: {lastError ?? "None"}";
    }
    
    /// <summary>
    /// Debug logging
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WebSocketTEST] {message}");
        }
    }
    
    /// <summary>
    /// Error logging
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[WebSocketTEST] {message}");
    }
    
    /// <summary>
    /// Warning logging
    /// </summary>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[WebSocketTEST] WARNING: {message}");
    }
}
