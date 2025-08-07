using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Simple WebSocket client using System.Net.WebSockets
/// This works better with Unity's security requirements
/// </summary>
public class SimpleWebSocketClient : MonoBehaviour
{
    [Header("Connection Settings")]
    public string serverAddress = "174.138.42.117";
    public int wsPort = 3002;
    
    [Header("Status")]
    public bool isConnected = false;
    public string connectionStatus = "Disconnected";
    
    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationToken;
    private Queue<string> messageQueue = new Queue<string>();
    
    // Events
    public static event Action<bool> OnConnectionChanged;
    public static event Action<string> OnMessageReceived;
    
    public static SimpleWebSocketClient Instance { get; private set; }
    
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
        // Try HTTP polling instead since WebSocket is having issues
        // TryHTTPConnection();
        Debug.Log("🔧 SimpleWebSocketClient disabled - use SimpleDirectWebSocket instead");
    }
    
    void Update()
    {
        // Process queued messages on main thread
        while (messageQueue.Count > 0)
        {
            string message = messageQueue.Dequeue();
            OnMessageReceived?.Invoke(message);
            ProcessGameMessage(message);
        }
    }
    
    /// <summary>
    /// Use HTTP as fallback since WebSocket has security issues
    /// </summary>
    public void TryHTTPConnection()
    {
        StartCoroutine(HTTPConnectionCoroutine());
    }
    
    IEnumerator HTTPConnectionCoroutine()
    {
        connectionStatus = "Connecting via HTTP...";
        string url = $"http://{serverAddress}:3001/api/connect";
        
        Debug.Log($"📡 Attempting HTTP connection to {url}");
        
        // Create connection data
        var connectData = new
        {
            clientType = "Unity",
            platform = Application.platform.ToString(),
            version = Application.version
        };
        
        string jsonData = JsonConvert.SerializeObject(connectData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        using (var request = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                isConnected = true;
                connectionStatus = "Connected via HTTP";
                OnConnectionChanged?.Invoke(true);
                Debug.Log($"✅ Connected via HTTP: {request.downloadHandler.text}");
                
                // Start polling for messages
                StartCoroutine(PollForMessages());
            }
            else
            {
                connectionStatus = $"Connection failed: {request.error}";
                Debug.LogError($"❌ HTTP connection failed: {request.error}");
                OnConnectionChanged?.Invoke(false);
                
                // Retry in a few seconds
                yield return new WaitForSeconds(3);
                TryHTTPConnection();
            }
        }
    }
    
    IEnumerator PollForMessages()
    {
        string pollUrl = $"http://{serverAddress}:3001/api/poll";
        
        while (isConnected)
        {
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(pollUrl))
            {
                request.timeout = 30; // Long polling
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    if (!string.IsNullOrEmpty(response) && response != "{}" && response != "[]")
                    {
                        messageQueue.Enqueue(response);
                    }
                }
            }
            
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    void ProcessGameMessage(string json)
    {
        try
        {
            var message = JsonConvert.DeserializeObject<GameMessage>(json);
            if (message != null)
            {
                Debug.Log($"📨 Game message: {message.type}");
                
                switch (message.type)
                {
                    case "gameState":
                        Debug.Log("Game state updated");
                        break;
                    case "playerJoined":
                        Debug.Log("Player joined");
                        break;
                    case "cardPlayed":
                        Debug.Log("Card played");
                        break;
                    case "betPlaced":
                        Debug.Log("Bet placed");
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse message: {e.Message}");
        }
    }
    
    public void SendMessage(string type, object data)
    {
        if (!isConnected)
        {
            Debug.LogError("Not connected");
            return;
        }
        
        StartCoroutine(SendMessageCoroutine(type, data));
    }
    
    IEnumerator SendMessageCoroutine(string type, object data)
    {
        string url = $"http://{serverAddress}:3001/api/message";
        
        var message = new
        {
            type = type,
            data = data,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        
        string jsonData = JsonConvert.SerializeObject(message);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        using (var request = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"📤 Sent: {type}");
            }
            else
            {
                Debug.LogError($"Failed to send: {request.error}");
            }
        }
    }
    
    void OnDestroy()
    {
        isConnected = false;
        cancellationToken?.Cancel();
        webSocket?.Dispose();
    }
    
    // Game API
    public void JoinGame(string gameId = null)
    {
        SendMessage("joinGame", new { gameId = gameId ?? Guid.NewGuid().ToString() });
    }
    
    public void SelectRelic(int relicId)
    {
        SendMessage("selectRelic", new { relicId });
    }
    
    public void PlayCard(int cardIndex, int position)
    {
        SendMessage("playCard", new { cardIndex, position });
    }
    
    public void PlaceBet(string action, int amount = 0)
    {
        SendMessage("placeBet", new { action, amount });
    }
    
    [Serializable]
    public class GameMessage
    {
        public string type;
        public object data;
        public string timestamp;
    }
}
