using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple UI to test WebSocket connection
/// </summary>
public class WebSocketTestUI : MonoBehaviour
{
    [Header("UI References")]
    public Button connectButton;
    public Button disconnectButton;
    public Button joinGameButton;
    public Button testMessageButton;
    public Button pingButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI messagesText;
    
    private int messageCount = 0;
    
    void Start()
    {
        // Subscribe to WebSocket events
        DoleroWebSocketClient.OnConnectionChanged += OnConnectionChanged;
        DoleroWebSocketClient.OnMessageReceived += OnMessageReceived;
        DoleroWebSocketClient.OnError += OnError;
        DoleroWebSocketClient.OnGameStateUpdated += OnGameStateUpdated;
        
        // Setup buttons
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectClicked);
        }
        
        if (disconnectButton != null)
        {
            disconnectButton.onClick.AddListener(OnDisconnectClicked);
        }
        
        if (joinGameButton != null)
        {
            joinGameButton.onClick.AddListener(OnJoinGameClicked);
            joinGameButton.interactable = false;
        }
        
        if (testMessageButton != null)
        {
            testMessageButton.onClick.AddListener(OnTestMessageClicked);
            testMessageButton.interactable = false;
        }
        
        if (pingButton != null)
        {
            pingButton.onClick.AddListener(OnPingClicked);
            pingButton.interactable = false;
        }
        
        UpdateStatus("Not Connected", Color.red);
    }
    
    void OnConnectClicked()
    {
        if (DoleroWebSocketClient.Instance != null)
        {
            if (!DoleroWebSocketClient.Instance.isConnected)
            {
                DoleroWebSocketClient.Instance.Connect();
                UpdateStatus("Connecting...", Color.yellow);
            }
            else
            {
                DoleroWebSocketClient.Instance.Disconnect();
            }
        }
        else
        {
            Debug.LogError("WebSocket client not found!");
        }
    }
    
    void OnDisconnectClicked()
    {
        if (DoleroWebSocketClient.Instance != null)
        {
            DoleroWebSocketClient.Instance.Disconnect();
        }
    }
    
    void OnJoinGameClicked()
    {
        if (DoleroWebSocketClient.Instance != null && DoleroWebSocketClient.Instance.isConnected)
        {
            DoleroWebSocketClient.Instance.JoinGame("test-game-001");
            AddMessage("Sent: Join Game");
        }
    }
    
    void OnTestMessageClicked()
    {
        if (DoleroWebSocketClient.Instance != null && DoleroWebSocketClient.Instance.isConnected)
        {
            // Test various messages
            DoleroWebSocketClient.Instance.RequestGameState();
            AddMessage("Sent: Request Game State");
            
            // Test relic selection
            DoleroWebSocketClient.Instance.SelectRelic(1);
            AddMessage("Sent: Select Relic (High Stakes)");
            
            // Test card play
            DoleroWebSocketClient.Instance.PlayCard(0, 1);
            AddMessage("Sent: Play Card");
            
            // Test bet
            DoleroWebSocketClient.Instance.PlaceBet("CALL");
            AddMessage("Sent: Place Bet (CALL)");
        }
    }
    
    void OnPingClicked()
    {
        if (DoleroWebSocketClient.Instance != null && DoleroWebSocketClient.Instance.isConnected)
        {
            DoleroWebSocketClient.Instance.SendMessage("ping");
            AddMessage("Sent: Ping");
        }
    }
    
    void OnConnectionChanged(bool connected)
    {
        if (connected)
        {
            UpdateStatus("Connected!", Color.green);
            if (connectButton != null)
            {
                connectButton.GetComponentInChildren<TextMeshProUGUI>().text = "Disconnect";
            }
            if (joinGameButton != null) joinGameButton.interactable = true;
            if (testMessageButton != null) testMessageButton.interactable = true;
            if (pingButton != null) pingButton.interactable = true;
        }
        else
        {
            UpdateStatus("Disconnected", Color.red);
            if (connectButton != null)
            {
                connectButton.GetComponentInChildren<TextMeshProUGUI>().text = "Connect";
            }
            if (joinGameButton != null) joinGameButton.interactable = false;
            if (testMessageButton != null) testMessageButton.interactable = false;
            if (pingButton != null) pingButton.interactable = false;
        }
    }
    
    void OnMessageReceived(string message)
    {
        messageCount++;
        AddMessage($"Received [{messageCount}]: {message}");
    }
    
    void OnError(string error)
    {
        AddMessage($"ERROR: {error}");
        UpdateStatus($"Error: {error}", Color.red);
    }
    
    void OnGameStateUpdated(DoleroWebSocketClient.GameStateUpdate state)
    {
        AddMessage($"Game State: Phase={state.phase}, Round={state.round}, Pot={state.pot}");
    }
    
    void UpdateStatus(string text, Color color)
    {
        if (statusText != null)
        {
            statusText.text = $"WebSocket Status: {text}";
            statusText.color = color;
        }
    }
    
    void AddMessage(string message)
    {
        if (messagesText != null)
        {
            // Keep only last 10 messages
            string[] lines = messagesText.text.Split('\n');
            if (lines.Length > 10)
            {
                messagesText.text = string.Join("\n", lines, lines.Length - 9, 9) + "\n" + message;
            }
            else
            {
                messagesText.text += (string.IsNullOrEmpty(messagesText.text) ? "" : "\n") + message;
            }
        }
        
        Debug.Log($"WebSocket: {message}");
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        DoleroWebSocketClient.OnConnectionChanged -= OnConnectionChanged;
        DoleroWebSocketClient.OnMessageReceived -= OnMessageReceived;
        DoleroWebSocketClient.OnError -= OnError;
        DoleroWebSocketClient.OnGameStateUpdated -= OnGameStateUpdated;
    }
}
