using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// WebSocket client for DOLERO server communication
/// Connects to 174.138.42.117:3002
/// </summary>
public class DoleroWebSocketClient : MonoBehaviour
{
    [Header("Connection Settings")]
    public string serverAddress = "174.138.42.117";
    public int wsPort = 3002;
    public bool autoConnect = true;
    public float reconnectDelay = 5f;
    public int maxReconnectAttempts = 3;
    
    [Header("Status")]
    public bool isConnected = false;
    public string connectionStatus = "Disconnected";
    public int reconnectAttempts = 0;
    
    // Events
    public static event Action<bool> OnConnectionChanged;
    public static event Action<string> OnMessageReceived;
    public static event Action<string> OnError;
    public static event Action<GameStateUpdate> OnGameStateUpdated;
    
    // WebSocket instance
    private WebSocketSharp.WebSocket websocket;
    private Coroutine reconnectCoroutine;
    private Queue<string> messageQueue = new Queue<string>();
    private bool isReconnecting = false;
    
    public static DoleroWebSocketClient Instance { get; private set; }
    
    // Game state data structure
    [Serializable]
    public class GameStateUpdate
    {
        public string phase;
        public int round;
        public double pot;
        public string[] players;
        public string currentPlayer;
        public double timeRemaining;
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
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
    public void Connect()
    {
        if (isConnected || isReconnecting)
        {
            Debug.Log("Already connected or connecting...");
            return;
        }
        
        StartCoroutine(ConnectCoroutine());
    }
    
    IEnumerator ConnectCoroutine()
    {
        Debug.Log($"🔗 Connecting to WebSocket server at ws://{serverAddress}:{wsPort}");
        connectionStatus = "Connecting...";
        isReconnecting = true;
        
        bool connectionFailed = false;
        string errorMessage = "";
        
        try
        {
            // Create WebSocket connection
            string wsUrl = $"ws://{serverAddress}:{wsPort}";
            websocket = new WebSocketSharp.WebSocket(wsUrl);
            
            // Configure WebSocket settings for insecure connections
            websocket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            websocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.None;
            websocket.Log.Level = WebSocketSharp.LogLevel.Error;
            
            // Additional security bypass for Unity
            websocket.SslConfiguration.CheckCertificateRevocation = false;
            websocket.SslConfiguration.ClientCertificates = null;
            
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
            OnConnectionChanged?.Invoke(false);
            OnError?.Invoke(errorMessage);
        }
        else if (!isConnected)
        {
            Debug.LogError("❌ WebSocket connection timeout");
            connectionStatus = "Connection timeout";
            OnConnectionChanged?.Invoke(false);
        }
        
        isReconnecting = false;
    }
    
    void OnWebSocketOpen(object sender, EventArgs e)
    {
        Debug.Log("✅ WebSocket connected!");
        isConnected = true;
        connectionStatus = "Connected";
        reconnectAttempts = 0;
        OnConnectionChanged?.Invoke(true);
        
        // Send initial connection message
        SendMessage("connect", new
        {
            clientType = "Unity",
            platform = Application.platform.ToString(),
            version = Application.version,
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
    
    void OnWebSocketMessage(object sender, WebSocketSharp.MessageEventArgs e)
    {
        try
        {
            string message = e.Data;
            Debug.Log($"📨 Received: {message}");
            
            // Parse message
            var messageData = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
            
            if (messageData.ContainsKey("type"))
            {
                string messageType = messageData["type"].ToString();
                
                switch (messageType)
                {
                    case "gameState":
                        HandleGameStateUpdate(messageData);
                        break;
                    case "error":
                        HandleErrorMessage(messageData);
                        break;
                    case "pong":
                        Debug.Log("🏓 Pong received");
                        break;
                    default:
                        OnMessageReceived?.Invoke(message);
                        break;
                }
            }
            else
            {
                OnMessageReceived?.Invoke(message);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Failed to parse message: {ex.Message}");
            OnError?.Invoke($"Message parsing error: {ex.Message}");
        }
    }
    
    void OnWebSocketClose(object sender, WebSocketSharp.CloseEventArgs e)
    {
        Debug.Log($"🔌 WebSocket closed: {e.Reason}");
        isConnected = false;
        connectionStatus = "Disconnected";
        OnConnectionChanged?.Invoke(false);
        
        // Attempt to reconnect
        if (reconnectAttempts < maxReconnectAttempts)
        {
            StartCoroutine(ReconnectCoroutine());
        }
        else
        {
            Debug.LogError("❌ Max reconnection attempts reached");
            OnError?.Invoke("Max reconnection attempts reached");
        }
    }
    
    void OnWebSocketError(object sender, WebSocketSharp.ErrorEventArgs e)
    {
        Debug.LogError($"❌ WebSocket error: {e.Message}");
        OnError?.Invoke(e.Message);
    }
    
    IEnumerator ReconnectCoroutine()
    {
        reconnectAttempts++;
        Debug.Log($"🔄 Attempting to reconnect... ({reconnectAttempts}/{maxReconnectAttempts})");
        
        yield return new WaitForSeconds(reconnectDelay);
        
        if (!isConnected)
        {
            Connect();
        }
    }
    
    /// <summary>
    /// Send a message to the server
    /// </summary>
    public void SendMessage(string type, object data = null)
    {
        if (!isConnected)
        {
            Debug.LogWarning("❌ Not connected, queuing message");
            messageQueue.Enqueue(JsonConvert.SerializeObject(new { type, data }));
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
            Debug.Log($"📤 Sent: {type}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to send message: {e.Message}");
            OnError?.Invoke($"Send error: {e.Message}");
        }
    }
    
    /// <summary>
    /// Join a game
    /// </summary>
    public void JoinGame(string gameId = null)
    {
        SendMessage("joinGame", new
        {
            gameId = gameId ?? Guid.NewGuid().ToString(),
            playerName = "UnityPlayer",
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
    
    /// <summary>
    /// Request current game state
    /// </summary>
    public void RequestGameState()
    {
        SendMessage("getGameState");
    }
    
    /// <summary>
    /// Select a relic
    /// </summary>
    public void SelectRelic(int relicId)
    {
        SendMessage("selectRelic", new
        {
            relicId = relicId,
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
    
    /// <summary>
    /// Play a card
    /// </summary>
    public void PlayCard(int cardIndex, int position)
    {
        SendMessage("playCard", new
        {
            cardIndex = cardIndex,
            position = position,
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
    
    /// <summary>
    /// Swap cards
    /// </summary>
    public void SwapCards(int card1Index, int card2Index)
    {
        SendMessage("swapCards", new
        {
            card1Index = card1Index,
            card2Index = card2Index,
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
    
    /// <summary>
    /// Place a bet
    /// </summary>
    public void PlaceBet(string action, double amount = 0)
    {
        SendMessage("placeBet", new
        {
            action = action, // RAISE, CALL, FOLD, REVEAL
            amount = amount,
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
    
    /// <summary>
    /// Disconnect from server
    /// </summary>
    public void Disconnect()
    {
        if (websocket != null && isConnected)
        {
            websocket.Close();
        }
        
        isConnected = false;
        connectionStatus = "Disconnected";
        reconnectAttempts = 0;
        
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }
        
        OnConnectionChanged?.Invoke(false);
    }
    
    void HandleGameStateUpdate(Dictionary<string, object> messageData)
    {
        try
        {
            if (messageData.ContainsKey("data"))
            {
                var gameStateJson = JsonConvert.SerializeObject(messageData["data"]);
                var gameState = JsonConvert.DeserializeObject<GameStateUpdate>(gameStateJson);
                
                Debug.Log($"🎮 Game state updated: Phase={gameState.phase}, Round={gameState.round}, Pot={gameState.pot}");
                OnGameStateUpdated?.Invoke(gameState);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to parse game state: {e.Message}");
        }
    }
    
    void HandleErrorMessage(Dictionary<string, object> messageData)
    {
        if (messageData.ContainsKey("data"))
        {
            string errorMessage = messageData["data"].ToString();
            Debug.LogError($"❌ Server error: {errorMessage}");
            OnError?.Invoke(errorMessage);
        }
    }
    
    void Update()
    {
        // Process queued messages when connected
        if (isConnected && messageQueue.Count > 0)
        {
            while (messageQueue.Count > 0)
            {
                string queuedMessage = messageQueue.Dequeue();
                try
                {
                    websocket.Send(queuedMessage);
                    Debug.Log("📤 Sent queued message");
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Failed to send queued message: {e.Message}");
                }
            }
        }
    }
    
    void OnDestroy()
    {
        Disconnect();
        
        if (websocket != null)
        {
            websocket.OnOpen -= OnWebSocketOpen;
            websocket.OnMessage -= OnWebSocketMessage;
            websocket.OnClose -= OnWebSocketClose;
            websocket.OnError -= OnWebSocketError;
        }
    }
}
