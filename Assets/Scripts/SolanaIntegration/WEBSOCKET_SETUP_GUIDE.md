# 🔗 WebSocket Connection Guide - DOLERO Server

## 🎯 Overview

This guide shows how to set up a proper WebSocket connection to your DOLERO server at `174.138.42.117:3002`.

## 🚀 Quick Setup

### Step 1: Add WebSocket Components
1. **Create a GameObject** named `DoleroWebSocket`
2. **Add these components**:
   - `DoleroWebSocketClient` - Core WebSocket functionality
   - `WebSocketTestUI` - User interface for testing
   - `WebSocketConnectionTest` - Automated testing

### Step 2: Configure WebSocket Settings
In the `DoleroWebSocketClient` component:
- **Server Address**: `174.138.42.117`
- **WebSocket Port**: `3002`
- **Auto Connect**: `true` (recommended)
- **Reconnect Delay**: `5` seconds
- **Max Reconnect Attempts**: `3`

### Step 3: Test the Connection
1. **Play the scene**
2. **Check Console** for connection status
3. **Use the UI** to test different functions

## 🔧 WebSocket Client Features

### Core Functions
```csharp
// Connect to server
DoleroWebSocketClient.Instance.Connect();

// Disconnect from server
DoleroWebSocketClient.Instance.Disconnect();

// Send a message
DoleroWebSocketClient.Instance.SendMessage("ping");
```

### Game Functions
```csharp
// Join a game
DoleroWebSocketClient.Instance.JoinGame("game-123");

// Request game state
DoleroWebSocketClient.Instance.RequestGameState();

// Select a relic
DoleroWebSocketClient.Instance.SelectRelic(1);

// Play a card
DoleroWebSocketClient.Instance.PlayCard(0, 1);

// Place a bet
DoleroWebSocketClient.Instance.PlaceBet("RAISE", 0.1);
```

## 🎮 Integration with Your Game

### 1. Connect to Existing Game Components
```csharp
// In your GameManager or main game script
public class GameManager : MonoBehaviour
{
    private DoleroWebSocketClient webSocketClient;
    
    void Start()
    {
        webSocketClient = FindObjectOfType<DoleroWebSocketClient>();
        
        // Subscribe to WebSocket events
        DoleroWebSocketClient.OnConnectionChanged += OnWebSocketConnectionChanged;
        DoleroWebSocketClient.OnGameStateUpdated += OnGameStateUpdated;
        DoleroWebSocketClient.OnMessageReceived += OnMessageReceived;
    }
    
    void OnWebSocketConnectionChanged(bool connected)
    {
        if (connected)
        {
            Debug.Log("✅ Connected to DOLERO server!");
            // Enable game features
            EnableGameFeatures();
        }
        else
        {
            Debug.Log("❌ Disconnected from server");
            // Disable game features
            DisableGameFeatures();
        }
    }
    
    void OnGameStateUpdated(DoleroWebSocketClient.GameStateUpdate state)
    {
        Debug.Log($"🎮 Game state: Phase={state.phase}, Round={state.round}, Pot={state.pot}");
        
        // Update your game UI based on state
        UpdateGameUI(state);
    }
    
    void OnMessageReceived(string message)
    {
        Debug.Log($"📨 Server message: {message}");
        // Handle server messages
    }
}
```

### 2. UI Integration
```csharp
// Add WebSocket UI to your existing UI
public class GameUI : MonoBehaviour
{
    public Button connectButton;
    public Button joinGameButton;
    public TextMeshProUGUI statusText;
    
    void Start()
    {
        // Setup WebSocket UI
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(() => {
                if (DoleroWebSocketClient.Instance.isConnected)
                {
                    DoleroWebSocketClient.Instance.Disconnect();
                }
                else
                {
                    DoleroWebSocketClient.Instance.Connect();
                }
            });
        }
        
        if (joinGameButton != null)
        {
            joinGameButton.onClick.AddListener(() => {
                if (DoleroWebSocketClient.Instance.isConnected)
                {
                    DoleroWebSocketClient.Instance.JoinGame();
                }
            });
        }
    }
}
```

## 🔐 Connection Security

### Best Practices
1. **Use `ws://` for development** (not `wss://`)
2. **Handle reconnection** automatically
3. **Queue messages** when disconnected
4. **Validate server responses**

### Error Handling
```csharp
// Subscribe to error events
DoleroWebSocketClient.OnError += OnWebSocketError;

void OnWebSocketError(string error)
{
    Debug.LogError($"WebSocket error: {error}");
    // Show error to user
    ShowErrorMessage($"Connection error: {error}");
}
```

## 🧪 Testing Setup

### Automated Testing
The `WebSocketConnectionTest` component will automatically:
- ✅ Test connection to server
- ✅ Test message sending
- ✅ Test game functions
- ✅ Log all results

### Manual Testing
Use the `WebSocketTestUI` component to:
- 🔗 Connect/Disconnect manually
- 🎮 Join games
- 📤 Send test messages
- 🏓 Send ping messages
- 📊 Request game state

## 📱 UI Components

### Required UI Elements
- **Connect Button** - Connect/Disconnect to server
- **Status Text** - Show connection status
- **Messages Text** - Display received messages
- **Join Game Button** - Join a game
- **Test Message Button** - Send test messages
- **Ping Button** - Send ping message

### UI Features
- ✅ **Real-time status updates**
- ✅ **Message history**
- ✅ **Button state management**
- ✅ **Error display**

## 🔄 Event System

### WebSocket Events
```csharp
// Subscribe to WebSocket events
DoleroWebSocketClient.OnConnectionChanged += OnConnectionChanged;
DoleroWebSocketClient.OnMessageReceived += OnMessageReceived;
DoleroWebSocketClient.OnError += OnError;
DoleroWebSocketClient.OnGameStateUpdated += OnGameStateUpdated;
```

### Event Parameters
- `OnConnectionChanged(bool connected)` - Connection status
- `OnMessageReceived(string message)` - Raw server message
- `OnError(string error)` - Error message
- `OnGameStateUpdated(GameStateUpdate state)` - Game state update

## 🎯 Game Integration Examples

### 1. Betting System Integration
```csharp
public class BettingSystem : MonoBehaviour
{
    public void PlaceBet(string action, double amount)
    {
        if (DoleroWebSocketClient.Instance.isConnected)
        {
            DoleroWebSocketClient.Instance.PlaceBet(action, amount);
        }
        else
        {
            Debug.LogError("Not connected to server!");
        }
    }
}
```

### 2. Relic System Integration
```csharp
public class RelicSystem : MonoBehaviour
{
    public void SelectRelic(int relicId)
    {
        if (DoleroWebSocketClient.Instance.isConnected)
        {
            DoleroWebSocketClient.Instance.SelectRelic(relicId);
        }
        else
        {
            Debug.LogError("Not connected to server!");
        }
    }
}
```

### 3. Game State Integration
```csharp
public class GameStateManager : MonoBehaviour
{
    void Start()
    {
        // Subscribe to game state updates
        DoleroWebSocketClient.OnGameStateUpdated += OnGameStateUpdated;
    }
    
    void OnGameStateUpdated(DoleroWebSocketClient.GameStateUpdate state)
    {
        switch (state.phase)
        {
            case "waiting":
                ShowWaitingScreen();
                break;
            case "relicSelection":
                ShowRelicSelection();
                break;
            case "cardPlaying":
                ShowCardPlaying();
                break;
            case "betting":
                ShowBettingUI();
                break;
            case "reveal":
                ShowCardReveal();
                break;
        }
    }
}
```

## 🚀 Next Steps

1. **Test the connection** - Verify it connects to your server
2. **Add UI elements** - Connect buttons and text fields
3. **Integrate with your game** - Connect to existing systems
4. **Test game functions** - Verify betting and relic selection
5. **Handle server responses** - Process game state updates

## 🐛 Troubleshooting

### Common Issues
1. **"Connection failed"** - Check server is running on port 3002
2. **"WebSocket error"** - Check firewall settings
3. **"Message not sent"** - Check connection status
4. **"Reconnection failed"** - Check network connectivity

### Debug Tips
- Enable detailed logging in WebSocket client
- Check Console for connection messages
- Verify server address and port
- Test with ping messages first

### Server Requirements
Your server at `174.138.42.117:3002` should:
- ✅ Accept WebSocket connections
- ✅ Handle JSON messages
- ✅ Send proper responses
- ✅ Support reconnection

The WebSocket integration is now ready to use! 🎉
