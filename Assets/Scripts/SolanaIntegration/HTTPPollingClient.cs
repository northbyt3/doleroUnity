using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// HTTP Polling fallback for when WebSockets don't work
/// This simulates real-time communication using HTTP polling
/// </summary>
public class HTTPPollingClient : MonoBehaviour
{
    [Header("Server Configuration")]
    public string serverAddress = "174.138.42.117";
    public int httpPort = 3001;
    public float pollInterval = 1f; // Poll every second
    
    [Header("Status")]
    public bool isConnected = false;
    public bool isPolling = false;
    
    private Coroutine pollingCoroutine;
    private string sessionId;
    private Queue<GameMessage> messageQueue = new Queue<GameMessage>();
    
    // Events
    public static event Action<bool> OnConnectionChanged;
    public static event Action<string> OnMessageReceived;
    public static event Action<GameMessage> OnGameMessageReceived;
    
    public static HTTPPollingClient Instance { get; private set; }
    
    [Serializable]
    public class GameMessage
    {
        public string type;
        public string data;
        public string timestamp;
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
        Connect();
    }
    
    public void Connect()
    {
        if (isConnected || isPolling)
        {
            Debug.Log("Already connected or connecting");
            return;
        }
        
        StartCoroutine(ConnectCoroutine());
    }
    
    IEnumerator ConnectCoroutine()
    {
        string connectUrl = $"http://{serverAddress}:{httpPort}/api/connect";
        Debug.Log($"📡 Connecting via HTTP to {connectUrl}");
        
        // Create connection request
        var connectData = new { 
            clientType = "Unity",
            version = "1.0.0"
        };
        
        string jsonData = JsonConvert.SerializeObject(connectData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(connectUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
                if (response.ContainsKey("sessionId"))
                {
                    sessionId = response["sessionId"].ToString();
                    isConnected = true;
                    OnConnectionChanged?.Invoke(true);
                    Debug.Log($"✅ Connected via HTTP! Session: {sessionId}");
                    
                    // Start polling
                    StartPolling();
                }
            }
            else
            {
                Debug.LogError($"❌ Connection failed: {request.error}");
                OnConnectionChanged?.Invoke(false);
            }
        }
    }
    
    void StartPolling()
    {
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
        }
        pollingCoroutine = StartCoroutine(PollForMessages());
    }
    
    IEnumerator PollForMessages()
    {
        isPolling = true;
        string pollUrl = $"http://{serverAddress}:{httpPort}/api/poll/{sessionId}";
        
        while (isConnected)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(pollUrl))
            {
                request.timeout = 30; // Long polling
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    ProcessMessages(request.downloadHandler.text);
                }
                else if (request.result != UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogWarning($"Poll error: {request.error}");
                }
            }
            
            yield return new WaitForSeconds(pollInterval);
        }
        
        isPolling = false;
    }
    
    void ProcessMessages(string json)
    {
        try
        {
            var messages = JsonConvert.DeserializeObject<List<GameMessage>>(json);
            if (messages != null && messages.Count > 0)
            {
                foreach (var msg in messages)
                {
                    Debug.Log($"📨 Received: {msg.type}");
                    OnMessageReceived?.Invoke(JsonConvert.SerializeObject(msg));
                    OnGameMessageReceived?.Invoke(msg);
                    messageQueue.Enqueue(msg);
                }
            }
        }
        catch (Exception e)
        {
            // Single message instead of array
            try
            {
                var msg = JsonConvert.DeserializeObject<GameMessage>(json);
                if (msg != null)
                {
                    OnGameMessageReceived?.Invoke(msg);
                }
            }
            catch
            {
                Debug.LogWarning($"Failed to parse message: {e.Message}");
            }
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
        string sendUrl = $"http://{serverAddress}:{httpPort}/api/send";
        
        var message = new
        {
            sessionId = sessionId,
            type = type,
            data = data,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        
        string jsonData = JsonConvert.SerializeObject(message);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(sendUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"📤 Sent: {type}");
            }
            else
            {
                Debug.LogError($"Failed to send message: {request.error}");
            }
        }
    }
    
    public void Disconnect()
    {
        isConnected = false;
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
            pollingCoroutine = null;
        }
        OnConnectionChanged?.Invoke(false);
        Debug.Log("Disconnected from HTTP polling");
    }
    
    void OnDestroy()
    {
        Disconnect();
    }
    
    // Public API matching WebSocket client
    public void JoinGame(string gameId = null)
    {
        SendMessage("joinGame", new { gameId });
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
}
