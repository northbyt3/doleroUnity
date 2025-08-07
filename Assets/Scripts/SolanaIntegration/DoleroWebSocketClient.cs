using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

// The Solana SDK already includes WebSocketSharp
// We'll use it directly without additional imports

/// <summary>
/// Real WebSocket client for DOLERO using WebSocketSharp
/// Handles real-time bidirectional communication with the Web2 delegate server
/// </summary>
public class DoleroWebSocketClient : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string serverAddress = "174.138.42.117";
    [SerializeField] private int wsPort = 3002; // WebSocket port
    [SerializeField] private bool autoReconnect = true;
    [SerializeField] private float reconnectDelay = 3f;
    [SerializeField] private bool useSecureWebSocket = false; // KEEP THIS FALSE for ws://
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    // WebSocket instance
    private WebSocketSharp.WebSocket websocket;
    private bool isConnecting = false;
    private Coroutine reconnectCoroutine;
    private Queue<Action> mainThreadActions = new Queue<Action>();
    
    // Player info
    private string playerId;
    private string gameId;
    
    // Events
    public static event Action<bool> OnConnectionChanged;
    public static event Action<string> OnMessageReceived;
    public static event Action<string> OnError;
    public static event Action<GameStateUpdate> OnGameStateUpdated;
    public static event Action<string> OnPlayerJoined;
    public static event Action<RelicSelectionData> OnRelicSelected;
    public static event Action<CardPlayData> OnCardPlayed;
    public static event Action<BetData> OnBetPlaced;
    
    public static DoleroWebSocketClient Instance { get; private set; }
    
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
        // Auto-connect on start - DISABLED due to Unity WebSocket security issues
        // Connect();
        Debug.Log("🔧 DoleroWebSocketClient disabled - use SimpleDirectWebSocket instead");
    }
    
    void Update()
    {
        // Execute queued actions on main thread
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }
    }
    
    /// <summary>
    /// Connect to the WebSocket server
    /// </summary>
    public void Connect()
    {
        if (websocket != null && websocket.ReadyState == WebSocketSharp.WebSocketState.Open)
        {
            Debug.Log("Already connected to WebSocket server");
            return;
        }
        
        if (isConnecting)
        {
            Debug.Log("Connection already in progress");
            return;
        }
        
        StartCoroutine(ConnectCoroutine());
    }
    
    private IEnumerator ConnectCoroutine()
    {
        isConnecting = true;
        
        // Construct WebSocket URL
        string protocol = useSecureWebSocket ? "wss" : "ws";
        string wsUrl = $"{protocol}://{serverAddress}:{wsPort}";
        
        Debug.Log($"🔌 Connecting to WebSocket server at {wsUrl}");
        
        bool connectionFailed = false;
        string errorMessage = "";
        
        try
        {
            // Create WebSocket instance
            websocket = new WebSocketSharp.WebSocket(wsUrl);
            
            // Configure WebSocket for insecure connections if needed
            if (!useSecureWebSocket)
            {
                // Allow insecure connections for development
                if (websocket.SslConfiguration != null)
                {
                    websocket.SslConfiguration.ServerCertificateValidationCallback = 
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
            }
            
            // Configure WebSocket
            websocket.OnOpen += OnWebSocketOpen;
            websocket.OnMessage += OnWebSocketMessage;
            websocket.OnError += OnWebSocketError;
            websocket.OnClose += OnWebSocketClose;
            
            // Set additional properties
            websocket.WaitTime = System.TimeSpan.FromSeconds(5);
            websocket.EmitOnPing = true;
            
            // Connect asynchronously
            websocket.ConnectAsync();
        }
        catch (Exception e)
        {
            connectionFailed = true;
            errorMessage = e.Message;
        }
        
        if (!connectionFailed)
        {
            // Wait for connection (timeout after 5 seconds)
            float timeout = 5f;
            float elapsed = 0f;
            
            while (websocket != null && websocket.ReadyState == WebSocketSharp.WebSocketState.Connecting && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (websocket == null || websocket.ReadyState != WebSocketSharp.WebSocketState.Open)
            {
                connectionFailed = true;
                errorMessage = $"Connection timeout or failed. State: {websocket?.ReadyState}";
            }
            else
            {
                Debug.Log("✅ WebSocket connected successfully!");
            }
        }
        
        if (connectionFailed)
        {
            Debug.LogError($"❌ WebSocket connection failed: {errorMessage}");
            QueueMainThreadAction(() => OnError?.Invoke($"Connection failed: {errorMessage}"));
            
            if (autoReconnect && reconnectCoroutine == null)
            {
                reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
            }
        }
        
        isConnecting = false;
    }
    
    #region WebSocket Event Handlers
    
    private void OnWebSocketOpen(object sender, EventArgs e)
    {
        Debug.Log("✅ WebSocket connection opened");
        
        QueueMainThreadAction(() =>
        {
            OnConnectionChanged?.Invoke(true);
            
            // Send initial handshake/authentication
            SendHandshake();
        });
    }
    
    private void OnWebSocketMessage(object sender, WebSocketSharp.MessageEventArgs e)
    {
        if (debugMode)
        {
            Debug.Log($"📨 Received: {e.Data}");
        }
        
        QueueMainThreadAction(() =>
        {
            OnMessageReceived?.Invoke(e.Data);
            ProcessMessage(e.Data);
        });
    }
    
    private void OnWebSocketError(object sender, WebSocketSharp.ErrorEventArgs e)
    {
        Debug.LogError($"❌ WebSocket error: {e.Message}");
        
        QueueMainThreadAction(() =>
        {
            OnError?.Invoke(e.Message);
        });
    }
    
    private void OnWebSocketClose(object sender, WebSocketSharp.CloseEventArgs e)
    {
        Debug.Log($"🔌 WebSocket closed: {e.Reason} (Code: {e.Code})");
        
        QueueMainThreadAction(() =>
        {
            OnConnectionChanged?.Invoke(false);
            
            if (autoReconnect && reconnectCoroutine == null)
            {
                reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
            }
        });
    }
    
    #endregion
    
    #region Message Processing
    
    private void ProcessMessage(string message)
    {
        try
        {
            var msg = JsonConvert.DeserializeObject<WebSocketMessage>(message);
            if (msg == null) return;
            
            switch (msg.type)
            {
                case "connected":
                    HandleConnected(msg);
                    break;
                case "gameState":
                    HandleGameStateUpdate(msg);
                    break;
                case "playerJoined":
                    HandlePlayerJoined(msg);
                    break;
                case "relicSelected":
                    HandleRelicSelected(msg);
                    break;
                case "cardPlayed":
                    HandleCardPlayed(msg);
                    break;
                case "cardSwapped":
                    HandleCardSwapped(msg);
                    break;
                case "betPlaced":
                    HandleBetPlaced(msg);
                    break;
                case "cardsRevealed":
                    HandleCardsRevealed(msg);
                    break;
                case "roundEnd":
                    HandleRoundEnd(msg);
                    break;
                case "error":
                    HandleError(msg);
                    break;
                default:
                    Debug.LogWarning($"Unknown message type: {msg.type}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to process message: {e.Message}\nMessage: {message}");
        }
    }
    
    private void HandleConnected(WebSocketMessage msg)
    {
        Debug.Log("✅ Server acknowledged connection");
        if (msg.data != null)
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(msg.data.ToString());
            if (data.ContainsKey("playerId"))
            {
                playerId = data["playerId"].ToString();
                Debug.Log($"Assigned Player ID: {playerId}");
            }
        }
    }
    
    private void HandleGameStateUpdate(WebSocketMessage msg)
    {
        var gameState = JsonConvert.DeserializeObject<GameStateUpdate>(msg.data.ToString());
        OnGameStateUpdated?.Invoke(gameState);
    }
    
    private void HandlePlayerJoined(WebSocketMessage msg)
    {
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(msg.data.ToString());
        OnPlayerJoined?.Invoke(data["playerId"]);
    }
    
    private void HandleRelicSelected(WebSocketMessage msg)
    {
        var data = JsonConvert.DeserializeObject<RelicSelectionData>(msg.data.ToString());
        OnRelicSelected?.Invoke(data);
    }
    
    private void HandleCardPlayed(WebSocketMessage msg)
    {
        var data = JsonConvert.DeserializeObject<CardPlayData>(msg.data.ToString());
        OnCardPlayed?.Invoke(data);
    }
    
    private void HandleCardSwapped(WebSocketMessage msg)
    {
        Debug.Log("Cards swapped");
        // Handle card swap confirmation
    }
    
    private void HandleBetPlaced(WebSocketMessage msg)
    {
        var data = JsonConvert.DeserializeObject<BetData>(msg.data.ToString());
        OnBetPlaced?.Invoke(data);
    }
    
    private void HandleCardsRevealed(WebSocketMessage msg)
    {
        Debug.Log("Cards revealed");
        // Handle card reveal
    }
    
    private void HandleRoundEnd(WebSocketMessage msg)
    {
        Debug.Log("Round ended");
        // Handle round end
    }
    
    private void HandleError(WebSocketMessage msg)
    {
        string error = msg.data?.ToString() ?? "Unknown error";
        Debug.LogError($"Server error: {error}");
        OnError?.Invoke(error);
    }
    
    #endregion
    
    #region Message Sending
    
    /// <summary>
    /// Send a message to the server
    /// </summary>
    public void SendMessage(string type, object data = null)
    {
        if (websocket == null || websocket.ReadyState != WebSocketSharp.WebSocketState.Open)
        {
            Debug.LogError("WebSocket is not connected");
            return;
        }
        
        var message = new WebSocketMessage
        {
            type = type,
            data = data,
            timestamp = DateTime.UtcNow.ToString("o"),
            playerId = playerId
        };
        
        string json = JsonConvert.SerializeObject(message);
        
        if (debugMode)
        {
            Debug.Log($"📤 Sending: {json}");
        }
        
        websocket.Send(json);
    }
    
    private void SendHandshake()
    {
        SendMessage("handshake", new
        {
            clientVersion = "1.0.0",
            platform = "Unity",
            playerId = SystemInfo.deviceUniqueIdentifier
        });
    }
    
    #endregion
    
    #region Public API
    
    public bool IsConnected => websocket != null && websocket.ReadyState == WebSocketSharp.WebSocketState.Open;
    
    public void JoinGame(string gameId = null)
    {
        this.gameId = gameId;
        SendMessage("joinGame", new { gameId });
    }
    
    public void SelectRelic(int relicId)
    {
        SendMessage("selectRelic", new { relicId, gameId });
    }
    
    public void PlayCard(int cardIndex, int position)
    {
        SendMessage("playCard", new { cardIndex, position, gameId });
    }
    
    public void SwapCards(int position1, int position2)
    {
        SendMessage("swapCards", new { position1, position2, gameId });
    }
    
    public void PlaceBet(string action, int amount = 0)
    {
        SendMessage("placeBet", new { action, amount, gameId });
    }
    
    public void RequestGameState()
    {
        SendMessage("getGameState", new { gameId });
    }
    
    #endregion
    
    #region Utility
    
    private void QueueMainThreadAction(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }
    
    public void Disconnect()
    {
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }
        
        if (websocket != null)
        {
            if (websocket.ReadyState == WebSocketSharp.WebSocketState.Open)
            {
                websocket.Close();
            }
            websocket = null;
        }
        
        Debug.Log("Disconnected from WebSocket server");
    }
    
    private IEnumerator ReconnectCoroutine()
    {
        while (!IsConnected && autoReconnect)
        {
            yield return new WaitForSeconds(reconnectDelay);
            Debug.Log("Attempting to reconnect...");
            Connect();
        }
        reconnectCoroutine = null;
    }
    
    void OnDestroy()
    {
        Disconnect();
    }
    
    #endregion
    
    #region Data Classes
    
    [Serializable]
    public class WebSocketMessage
    {
        public string type;
        public object data;
        public string timestamp;
        public string playerId;
    }
    
    [Serializable]
    public class GameStateUpdate
    {
        public string gameId;
        public string phase;
        public int round;
        public List<PlayerState> players;
        public int pot;
        public int currentBet;
        public string currentPlayer;
    }
    
    [Serializable]
    public class PlayerState
    {
        public string playerId;
        public int hearts;
        public int chips;
        public string status;
        public int bet;
    }
    
    [Serializable]
    public class RelicSelectionData
    {
        public string playerId;
        public int relicId;
        public string relicName;
    }
    
    [Serializable]
    public class CardPlayData
    {
        public string playerId;
        public int cardIndex;
        public int position;
    }
    
    [Serializable]
    public class BetData
    {
        public string playerId;
        public string action;
        public int amount;
        public int newPot;
    }
    
    #endregion
}
