using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// WebSocket manager for DOLERO server communication
/// Handles real-time communication with the Web2 delegate server
/// </summary>
public class DoleroWebSocketManager : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string serverAddress = "174.138.42.117";
    [SerializeField] private int wsPort = 3002; // WebSocket port
    [SerializeField] private int httpPort = 3001; // HTTP port for fallback
    [SerializeField] private bool autoReconnect = true;
    [SerializeField] private float reconnectDelay = 3f;
    
    // Connection state
    private bool isConnected = false;
    private bool isConnecting = false;
    private Coroutine reconnectCoroutine;
    
    // Events
    public static event Action<bool> OnConnectionChanged;
    public static event Action<string> OnMessageReceived;
    public static event Action<string> OnError;
    
    // Message types
    public enum MessageType
    {
        Connect,
        JoinGame,
        SelectRelic,
        PlayCard,
        SwapCards,
        PlaceBet,
        Reveal,
        GameState,
        Error
    }
    
    public static DoleroWebSocketManager Instance { get; private set; }
    
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
        // Auto-connect on start
        Connect();
    }
    
    /// <summary>
    /// Connect to the WebSocket server
    /// </summary>
    public void Connect()
    {
        if (isConnected || isConnecting)
        {
            Debug.Log("Already connected or connecting to WebSocket server");
            return;
        }
        
        StartCoroutine(ConnectCoroutine());
    }
    
    private IEnumerator ConnectCoroutine()
    {
        isConnecting = true;
        
        // First, check if server is reachable via HTTP
        string healthUrl = $"http://{serverAddress}:{httpPort}/health";
        Debug.Log($"Checking server health at {healthUrl}");
        
        using (UnityWebRequest healthRequest = UnityWebRequest.Get(healthUrl))
        {
            healthRequest.timeout = 5;
            yield return healthRequest.SendWebRequest();
            
            if (healthRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Server health check failed: {healthRequest.error}");
                OnError?.Invoke($"Cannot reach server at {serverAddress}:{httpPort}");
                isConnecting = false;
                
                // Try to reconnect if enabled
                if (autoReconnect && reconnectCoroutine == null)
                {
                    reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
                }
                yield break;
            }
            
            Debug.Log($"✅ Server is reachable: {healthRequest.downloadHandler.text}");
        }
        
        // WebSocket connection would go here
        // For Unity, you need to install a WebSocket package
        // Recommended: NativeWebSocket from Package Manager
        
        // For now, we'll simulate WebSocket with HTTP polling
        StartCoroutine(SimulateWebSocketWithPolling());
        
        isConnected = true;
        isConnecting = false;
        OnConnectionChanged?.Invoke(true);
        
        Debug.Log($"✅ Connected to DOLERO server at {serverAddress}");
        Debug.Log("⚠️ Note: Using HTTP polling to simulate WebSocket. Install NativeWebSocket package for real WebSocket support.");
    }
    
    /// <summary>
    /// Simulate WebSocket with HTTP polling (temporary solution)
    /// </summary>
    private IEnumerator SimulateWebSocketWithPolling()
    {
        while (isConnected)
        {
            // Poll for game state updates
            string stateUrl = $"http://{serverAddress}:{httpPort}/api/game/state";
            
            using (UnityWebRequest request = UnityWebRequest.Get(stateUrl))
            {
                request.timeout = 30; // Long polling timeout
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    if (!string.IsNullOrEmpty(response))
                    {
                        OnMessageReceived?.Invoke(response);
                        ProcessMessage(response);
                    }
                }
                else if (request.result != UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogWarning($"Polling error: {request.error}");
                }
            }
            
            // Wait before next poll
            yield return new WaitForSeconds(1f);
        }
    }
    
    /// <summary>
    /// Send a message to the server
    /// </summary>
    public void SendMessage(MessageType type, object data)
    {
        if (!isConnected)
        {
            Debug.LogError("Not connected to server");
            return;
        }
        
        StartCoroutine(SendMessageCoroutine(type, data));
    }
    
    private IEnumerator SendMessageCoroutine(MessageType type, object data)
    {
        var message = new
        {
            type = type.ToString(),
            data = data,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        
        string json = JsonConvert.SerializeObject(message);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        
        string url = $"http://{serverAddress}:{httpPort}/api/game/message";
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Message sent: {type}");
                string response = request.downloadHandler.text;
                if (!string.IsNullOrEmpty(response))
                {
                    ProcessMessage(response);
                }
            }
            else
            {
                Debug.LogError($"Failed to send message: {request.error}");
                OnError?.Invoke($"Failed to send {type}: {request.error}");
            }
        }
    }
    
    /// <summary>
    /// Process incoming messages
    /// </summary>
    private void ProcessMessage(string message)
    {
        try
        {
            var msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
            if (msg != null && msg.ContainsKey("type"))
            {
                string msgType = msg["type"].ToString();
                Debug.Log($"Received message type: {msgType}");
                
                // Handle different message types
                switch (msgType)
                {
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
                    case "betPlaced":
                        HandleBetPlaced(msg);
                        break;
                    case "error":
                        HandleError(msg);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to process message: {e.Message}");
        }
    }
    
    private void HandleGameStateUpdate(Dictionary<string, object> msg)
    {
        Debug.Log("Game state updated");
        // Forward to game state manager
        if (DoleroGameStateManager.Instance != null)
        {
            // DoleroGameStateManager.Instance.UpdateFromServer(msg);
        }
    }
    
    private void HandlePlayerJoined(Dictionary<string, object> msg)
    {
        Debug.Log("Player joined game");
    }
    
    private void HandleRelicSelected(Dictionary<string, object> msg)
    {
        Debug.Log("Relic selected by player");
    }
    
    private void HandleCardPlayed(Dictionary<string, object> msg)
    {
        Debug.Log("Card played");
    }
    
    private void HandleBetPlaced(Dictionary<string, object> msg)
    {
        Debug.Log("Bet placed");
    }
    
    private void HandleError(Dictionary<string, object> msg)
    {
        string error = msg.ContainsKey("message") ? msg["message"].ToString() : "Unknown error";
        Debug.LogError($"Server error: {error}");
        OnError?.Invoke(error);
    }
    
    /// <summary>
    /// Disconnect from server
    /// </summary>
    public void Disconnect()
    {
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }
        
        isConnected = false;
        OnConnectionChanged?.Invoke(false);
        Debug.Log("Disconnected from server");
    }
    
    private IEnumerator ReconnectCoroutine()
    {
        while (!isConnected && autoReconnect)
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
    
    #region Public API
    
    public bool IsConnected => isConnected;
    
    public void JoinGame(string playerId)
    {
        SendMessage(MessageType.JoinGame, new { playerId });
    }
    
    public void SelectRelic(int relicId)
    {
        SendMessage(MessageType.SelectRelic, new { relicId });
    }
    
    public void PlayCard(int cardIndex)
    {
        SendMessage(MessageType.PlayCard, new { cardIndex });
    }
    
    public void SwapCards(int card1, int card2)
    {
        SendMessage(MessageType.SwapCards, new { card1, card2 });
    }
    
    public void PlaceBet(string action, int amount = 0)
    {
        SendMessage(MessageType.PlaceBet, new { action, amount });
    }
    
    #endregion
}
