using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Simple WebSocket-like client using HTTP polling
/// This completely bypasses Unity's WebSocket restrictions
/// </summary>
public class SimpleDirectWebSocket : MonoBehaviour
{
    [Header("Connection")]
    public string serverAddress = "174.138.42.117";
    public int httpPort = 3001; // HTTP port for polling
    
    private bool isConnected = false;
    private string playerId;
    private Coroutine pollingCoroutine;
    
    // Events
    public static event Action<bool> OnConnectionChanged;
    public static event Action<string> OnMessageReceived;
    
    public static SimpleDirectWebSocket Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            playerId = System.Guid.NewGuid().ToString();
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
        StartCoroutine(ConnectCoroutine());
    }
    
    IEnumerator ConnectCoroutine()
    {
        Debug.Log($"📡 Connecting to {serverAddress}:{httpPort} using HTTP polling...");
        
        string url = $"http://{serverAddress}:{httpPort}/api/connect";
        
        var connectData = new
        {
            playerId = playerId,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        
        string jsonData = JsonConvert.SerializeObject(connectData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                isConnected = true;
                OnConnectionChanged?.Invoke(true);
                Debug.Log("✅ Connected via HTTP polling!");
                
                // Start polling for messages
                pollingCoroutine = StartCoroutine(PollForMessages());
            }
            else
            {
                Debug.LogError($"❌ Connection failed: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                OnConnectionChanged?.Invoke(false);
            }
        }
    }
    
    IEnumerator PollForMessages()
    {
        while (isConnected)
        {
            yield return new WaitForSeconds(1f); // Poll every second
            
            string url = $"http://{serverAddress}:{httpPort}/api/poll?playerId={playerId}";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    if (!string.IsNullOrEmpty(response) && response != "[]")
                    {
                        OnMessageReceived?.Invoke(response);
                        Debug.Log($"📨 Polled messages: {response}");
                    }
                }
                else if (request.responseCode != 404) // 404 is normal when no messages
                {
                    Debug.LogError($"Polling error: {request.error}");
                }
            }
        }
    }
    
    public void SendMessage(string type, object data)
    {
        if (!isConnected)
        {
            Debug.LogError("Not connected - cannot send message");
            return;
        }
        
        StartCoroutine(SendMessageCoroutine(type, data));
    }
    
    IEnumerator SendMessageCoroutine(string type, object data)
    {
        string url = $"http://{serverAddress}:{httpPort}/api/message";
        
        var message = new
        {
            playerId = playerId,
            type = type,
            data = data,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        
        string jsonData = JsonConvert.SerializeObject(message);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"📤 Sent message: {type}");
            }
            else
            {
                Debug.LogError($"❌ Failed to send message: {request.error}");
            }
        }
    }
    
    void OnDestroy()
    {
        isConnected = false;
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
        }
    }
    
    // Game API
    public void JoinGame(string gameId = null)
    {
        SendMessage("joinGame", new { gameId });
    }
    
    public void SelectRelic(int relicId)
    {
        SendMessage("selectRelic", new { relicId });
    }
    
    public void SwapCards(int card1Index, int card2Index)
    {
        SendMessage("swapCards", new { card1Index, card2Index });
    }
}
