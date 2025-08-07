using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Direct WebSocket implementation using TCP sockets
/// This bypasses Unity's security restrictions
/// </summary>
public class DirectWebSocket : MonoBehaviour
{
    [Header("Connection")]
    public string serverAddress = "174.138.42.117";
    public int wsPort = 3002;
    
    private TcpClient tcpClient;
    private NetworkStream stream;
    private bool isConnected = false;
    
    // Events
    public static event Action<bool> OnConnectionChanged;
    public static event Action<string> OnMessageReceived;
    
    public static DirectWebSocket Instance { get; private set; }
    
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
        // Connect();
        Debug.Log("🔧 DirectWebSocket disabled - use SimpleDirectWebSocket instead");
    }
    
    public void Connect()
    {
        StartCoroutine(ConnectCoroutine());
    }
    
    IEnumerator ConnectCoroutine()
    {
        Debug.Log($"📡 Connecting to {serverAddress}:{wsPort} using direct TCP...");
        
        // Create TCP connection
        tcpClient = new TcpClient();
        
        // Connect asynchronously
        var connectTask = tcpClient.ConnectAsync(serverAddress, wsPort);
        
        // Wait for connection with timeout
        float timeout = 5f;
        float elapsed = 0f;
        bool connectionFailed = false;
        string errorMessage = "";
        
        while (!connectTask.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        try
        {
            if (!connectTask.IsCompleted)
            {
                connectionFailed = true;
                errorMessage = "Connection timeout";
            }
            else if (tcpClient.Connected)
            {
                stream = tcpClient.GetStream();
                isConnected = true;
                
                // Send WebSocket handshake
                SendWebSocketHandshake();
                
                // Start receiving messages
                StartCoroutine(ReceiveMessages());
                
                OnConnectionChanged?.Invoke(true);
                Debug.Log("✅ Connected via direct TCP!");
            }
            else
            {
                connectionFailed = true;
                errorMessage = "Failed to connect";
            }
        }
        catch (Exception e)
        {
            connectionFailed = true;
            errorMessage = e.Message;
        }
        
        if (connectionFailed)
        {
            Debug.LogError($"❌ Connection failed: {errorMessage}");
            Debug.Log("💡 Falling back to HTTP polling...");
            
            // Fallback to HTTP
            if (gameObject.GetComponent<SimpleWebSocketClient>() == null)
            {
                var httpClient = gameObject.AddComponent<SimpleWebSocketClient>();
                if (httpClient != null)
                {
                    httpClient.TryHTTPConnection();
                }
            }
        }
    }
    
    void SendWebSocketHandshake()
    {
        // Construct WebSocket handshake
        string handshake = "GET / HTTP/1.1\r\n" +
                          $"Host: {serverAddress}:{wsPort}\r\n" +
                          "Upgrade: websocket\r\n" +
                          "Connection: Upgrade\r\n" +
                          "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                          "Sec-WebSocket-Version: 13\r\n" +
                          "\r\n";
        
        byte[] handshakeBytes = Encoding.UTF8.GetBytes(handshake);
        stream.Write(handshakeBytes, 0, handshakeBytes.Length);
        
        Debug.Log("📤 Sent WebSocket handshake");
    }
    
    IEnumerator ReceiveMessages()
    {
        byte[] buffer = new byte[1024];
        
        while (isConnected && stream != null)
        {
            if (stream.DataAvailable)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ProcessMessage(message);
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void ProcessMessage(string message)
    {
        // Skip HTTP headers if present
        if (message.Contains("HTTP/1.1 101"))
        {
            Debug.Log("✅ WebSocket handshake accepted");
            return;
        }
        
        // Process actual WebSocket frames
        OnMessageReceived?.Invoke(message);
        Debug.Log($"📨 Received: {message}");
    }
    
    public void SendMessage(string type, object data)
    {
        if (!isConnected || stream == null)
        {
            Debug.LogError("Not connected");
            return;
        }
        
        var message = new
        {
            type = type,
            data = data,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        
        string json = JsonConvert.SerializeObject(message);
        byte[] messageBytes = Encoding.UTF8.GetBytes(json);
        
        // Simple WebSocket frame (text message)
        byte[] frame = new byte[messageBytes.Length + 2];
        frame[0] = 0x81; // FIN = 1, opcode = 1 (text)
        frame[1] = (byte)messageBytes.Length; // Payload length
        Array.Copy(messageBytes, 0, frame, 2, messageBytes.Length);
        
        stream.Write(frame, 0, frame.Length);
        Debug.Log($"📤 Sent: {type}");
    }
    
    void OnDestroy()
    {
        isConnected = false;
        stream?.Close();
        tcpClient?.Close();
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
}
