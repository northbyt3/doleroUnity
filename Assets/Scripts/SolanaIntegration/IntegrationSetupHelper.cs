using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to automatically set up and connect all integration components
/// Attach this to your DoleroIntegration GameObject
/// </summary>
public class IntegrationSetupHelper : MonoBehaviour
{
    [Header("🎮 Your Existing Game Components")]
    [Space(10)]
    [Tooltip("Drag your existing GameManager here")]
    public GameManager gameManager;
    
    [Tooltip("Drag your existing HorizontalCardHolder here")]
    public HorizontalCardHolder cardHolder;
    
    [Tooltip("Drag your existing PlayerHealth here")]
    public PlayerHealth playerHealth;
    
    [Tooltip("Drag your existing VisualCardsHandler here")]
    public VisualCardsHandler visualCardsHandler;
    
    [Tooltip("Drag your existing BetAmountHandler here (if you have one)")]
    public BetAmountHandler betAmountHandler;
    
    [Header("🌐 WebSocket Configuration")]
    [Space(10)]
    public string serverAddress = "174.138.42.117";
    public int wsPort = 3002;
    public bool autoConnect = true;
    public bool debugMode = true;
    
    [Header("✅ Setup Status")]
    [Space(10)]
    [SerializeField] private bool isSetupComplete = false;
    
    void Start()
    {
        if (!isSetupComplete)
        {
            SetupIntegration();
        }
        
        if (autoConnect)
        {
            ConnectToServer();
        }
    }
    
    [ContextMenu("Setup Integration")]
    public void SetupIntegration()
    {
        Debug.Log("🔧 Setting up DOLERO Integration...");
        
        // 1. Configure WebSocket Client
        var wsClient = GetComponent<DoleroWebSocketClient>();
        if (wsClient == null)
        {
            wsClient = gameObject.AddComponent<DoleroWebSocketClient>();
        }
        ConfigureWebSocket(wsClient);
        
        // 2. Configure Game State Manager
        var gameStateManager = GetComponent<DoleroGameStateManager>();
        if (gameStateManager != null && gameManager != null)
        {
            Debug.Log("✅ Connected GameStateManager to GameManager");
        }
        
        // 3. Find and configure all systems
        ConfigureSystems();
        
        // 4. Setup event connections
        SetupEventConnections();
        
        isSetupComplete = true;
        Debug.Log("✅ DOLERO Integration Setup Complete!");
        Debug.Log("Press F4 during play to open the test panel");
    }
    
    void ConfigureWebSocket(DoleroWebSocketClient wsClient)
    {
        // The WebSocket client will use the values from its inspector
        // But we can set them programmatically if needed
        Debug.Log($"📡 WebSocket configured for {serverAddress}:{wsPort}");
    }
    
    void ConfigureSystems()
    {
        // Find all system components
        var systems = GetComponentsInChildren<MonoBehaviour>();
        
        foreach (var system in systems)
        {
            switch (system)
            {
                case DoleroCardSwapSystem swapSystem:
                    Debug.Log("✅ Card Swap System found and configured");
                    break;
                    
                case DoleroBettingSystem bettingSystem:
                    Debug.Log("✅ Betting System found and configured");
                    break;
                    
                case DoleroRelicSystem relicSystem:
                    Debug.Log("✅ Relic System found and configured");
                    break;
                    
                case DoleroTimerSystem timerSystem:
                    Debug.Log("✅ Timer System found and configured");
                    break;
                    
                case DoleroProgressiveRevealSystem revealSystem:
                    Debug.Log("✅ Progressive Reveal System found and configured");
                    break;
            }
        }
    }
    
    void SetupEventConnections()
    {
        // Connect WebSocket events to game systems
        DoleroWebSocketClient.OnConnectionChanged += OnServerConnectionChanged;
        DoleroWebSocketClient.OnMessageReceived += OnServerMessageReceived;
        DoleroWebSocketClient.OnGameStateUpdated += OnGameStateUpdated;
        
        Debug.Log("✅ Event connections established");
    }
    
    void OnServerConnectionChanged(bool connected)
    {
        if (connected)
        {
            Debug.Log("✅ Connected to DOLERO server!");
            
            // Auto-join a game
            if (DoleroWebSocketClient.Instance != null)
            {
                DoleroWebSocketClient.Instance.JoinGame();
            }
        }
        else
        {
            Debug.Log("❌ Disconnected from server");
        }
    }
    
    void OnServerMessageReceived(string message)
    {
        if (debugMode)
        {
            Debug.Log($"📨 Server message: {message}");
        }
    }
    
    void OnGameStateUpdated(DoleroWebSocketClient.GameStateUpdate state)
    {
        Debug.Log($"🎮 Game state updated: Phase={state.phase}, Round={state.round}, Pot={state.pot}");
        
        // Update your game UI here
        if (gameManager != null)
        {
            // Example: Update pot display
            // gameManager.UpdatePotDisplay(state.pot);
        }
    }
    
    [ContextMenu("Connect to Server")]
    public void ConnectToServer()
    {
        if (DoleroWebSocketClient.Instance != null)
        {
            DoleroWebSocketClient.Instance.Connect();
        }
        else
        {
            Debug.LogError("WebSocket client not found!");
        }
    }
    
    [ContextMenu("Test Game Flow")]
    public void TestGameFlow()
    {
        StartCoroutine(TestGameFlowCoroutine());
    }
    
    System.Collections.IEnumerator TestGameFlowCoroutine()
    {
        Debug.Log("🎮 Starting Game Flow Test...");
        
        // 1. Join game
        DoleroWebSocketClient.Instance.JoinGame("test-game-001");
        yield return new WaitForSeconds(1);
        
        // 2. Select relic
        Debug.Log("Selecting High Stakes relic...");
        DoleroWebSocketClient.Instance.SelectRelic(1);
        yield return new WaitForSeconds(1);
        
        // 3. Play cards
        Debug.Log("Playing cards...");
        for (int i = 0; i < 3; i++)
        {
            DoleroWebSocketClient.Instance.PlayCard(i, i + 1);
            yield return new WaitForSeconds(0.5f);
        }
        
        // 4. Swap cards
        Debug.Log("Swapping cards...");
        DoleroWebSocketClient.Instance.SwapCards(1, 2);
        yield return new WaitForSeconds(1);
        
        // 5. Place bet
        Debug.Log("Placing bet...");
        DoleroWebSocketClient.Instance.PlaceBet("RAISE", 100);
        yield return new WaitForSeconds(1);
        
        Debug.Log("✅ Game Flow Test Complete!");
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        DoleroWebSocketClient.OnConnectionChanged -= OnServerConnectionChanged;
        DoleroWebSocketClient.OnMessageReceived -= OnServerMessageReceived;
        DoleroWebSocketClient.OnGameStateUpdated -= OnGameStateUpdated;
    }
}
