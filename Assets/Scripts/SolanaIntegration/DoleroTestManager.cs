using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Simple testing manager for DOLERO integration without heavy dependencies
/// This allows testing basic functionality while SDK dependencies are being installed
/// </summary>
public class DoleroTestManager : MonoBehaviour
{
    [Header("Server Testing")]
    [SerializeField] private string serverUrl = "http://174.138.42.117";
    [SerializeField] private Button testServerButton;
    [SerializeField] private TextMeshProUGUI serverStatusText;
    
    [Header("Wallet Testing")]
    [SerializeField] private Button simulateWalletButton;
    [SerializeField] private TextMeshProUGUI walletStatusText;
    [SerializeField] private string testWalletAddress = "DemoWallet123456789";
    
    [Header("Game Flow Testing")]
    [SerializeField] private Button startGameFlowButton;
    [SerializeField] private Button testSwapButton;
    [SerializeField] private Button testBettingButton;
    [SerializeField] private Button testRevealButton;
    [SerializeField] private TextMeshProUGUI gameFlowStatusText;
    
    [Header("Integration Testing")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HorizontalCardHolder playerDeck;
    [SerializeField] private PlayerHealth playerHealth;
    
    // Test state
    private bool serverConnected = false;
    private bool walletConnected = false;
    private bool gameActive = false;
    private TestGamePhase currentPhase = TestGamePhase.Waiting;
    
    // Test data
    private List<TestCardData> testCards = new List<TestCardData>();
    private int testSwapsRemaining = 3;
    private decimal testBetAmount = 0.01m;
    
    public static DoleroTestManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        SetupTestUI();
        InitializeTestData();
    }
    
    #region UI Setup
    
    private void SetupTestUI()
    {
        // Setup server test button
        if (testServerButton != null)
        {
            testServerButton.onClick.AddListener(TestServerConnection);
        }
        
        // Setup wallet test button
        if (simulateWalletButton != null)
        {
            simulateWalletButton.onClick.AddListener(SimulateWalletConnection);
        }
        
        // Setup game flow buttons
        if (startGameFlowButton != null)
        {
            startGameFlowButton.onClick.AddListener(StartTestGameFlow);
        }
        
        if (testSwapButton != null)
        {
            testSwapButton.onClick.AddListener(TestSwapSystem);
        }
        
        if (testBettingButton != null)
        {
            testBettingButton.onClick.AddListener(TestBettingSystem);
        }
        
        if (testRevealButton != null)
        {
            testRevealButton.onClick.AddListener(TestRevealSystem);
        }
        
        UpdateTestUI();
    }
    
    private void UpdateTestUI()
    {
        // Update server status
        if (serverStatusText != null)
        {
            serverStatusText.text = serverConnected ? 
                $"✅ Server Connected: {serverUrl}" : 
                $"❌ Server Disconnected: {serverUrl}";
            serverStatusText.color = serverConnected ? Color.green : Color.red;
        }
        
        // Update wallet status
        if (walletStatusText != null)
        {
            walletStatusText.text = walletConnected ? 
                $"✅ Wallet Connected: {testWalletAddress}" : 
                "❌ Wallet Not Connected";
            walletStatusText.color = walletConnected ? Color.green : Color.red;
        }
        
        // Update game flow status
        if (gameFlowStatusText != null)
        {
            gameFlowStatusText.text = $"Phase: {currentPhase}\nSwaps: {testSwapsRemaining}\nBet: {testBetAmount:F3} SOL";
        }
        
        // Update button states
        if (startGameFlowButton != null)
        {
            startGameFlowButton.interactable = serverConnected && walletConnected && !gameActive;
        }
        
        if (testSwapButton != null)
        {
            testSwapButton.interactable = gameActive && currentPhase == TestGamePhase.CardPlaying;
        }
        
        if (testBettingButton != null)
        {
            testBettingButton.interactable = gameActive && currentPhase == TestGamePhase.Betting;
        }
        
        if (testRevealButton != null)
        {
            testRevealButton.interactable = gameActive && currentPhase == TestGamePhase.FinalReveal;
        }
    }
    
    #endregion
    
    #region Test Data Initialization
    
    private void InitializeTestData()
    {
        // Initialize test cards
        testCards = new List<TestCardData>
        {
            new TestCardData { value = 10, suit = 0, position = 1 },
            new TestCardData { value = 5, suit = 1, position = 2 },
            new TestCardData { value = 8, suit = 2, position = 3 }
        };
    }
    
    #endregion
    
    #region Server Connection Testing
    
    public void TestServerConnection()
    {
        StartCoroutine(TestServerConnectionCoroutine());
    }
    
    private IEnumerator TestServerConnectionCoroutine()
    {
        if (serverStatusText != null)
        {
            serverStatusText.text = "Testing server connection...";
            serverStatusText.color = Color.yellow;
        }
        
        // Test health endpoint
        UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/health");
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            serverConnected = true;
            Debug.Log($"✅ Server connection successful: {request.downloadHandler.text}");
            
            // Test API endpoint
            yield return StartCoroutine(TestAPIEndpoint());
        }
        else
        {
            serverConnected = false;
            Debug.LogError($"❌ Server connection failed: {request.error}");
        }
        
        UpdateTestUI();
    }
    
    private IEnumerator TestAPIEndpoint()
    {
        // Test a simple API call
        UnityWebRequest apiRequest = UnityWebRequest.Get($"{serverUrl}/api/test");
        yield return apiRequest.SendWebRequest();
        
        if (apiRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ API endpoint accessible");
        }
        else
        {
            Debug.Log("⚠️ API endpoint not accessible (expected for now)");
        }
    }
    
    #endregion
    
    #region Wallet Connection Testing
    
    public void SimulateWalletConnection()
    {
        StartCoroutine(SimulateWalletConnectionCoroutine());
    }
    
    private IEnumerator SimulateWalletConnectionCoroutine()
    {
        if (walletStatusText != null)
        {
            walletStatusText.text = "Connecting wallet...";
            walletStatusText.color = Color.yellow;
        }
        
        yield return new WaitForSeconds(2f); // Simulate connection time
        
        walletConnected = true;
        Debug.Log($"✅ Wallet connected (simulated): {testWalletAddress}");
        
        UpdateTestUI();
    }
    
    #endregion
    
    #region Game Flow Testing
    
    public void StartTestGameFlow()
    {
        if (!serverConnected || !walletConnected)
        {
            Debug.LogWarning("Cannot start game flow - server or wallet not connected");
            return;
        }
        
        StartCoroutine(TestGameFlowCoroutine());
    }
    
    private IEnumerator TestGameFlowCoroutine()
    {
        gameActive = true;
        Debug.Log("🎮 Starting test game flow...");
        
        // Phase 1: Relic Selection
        currentPhase = TestGamePhase.RelicSelection;
        UpdateTestUI();
        Debug.Log("Phase 1: Relic Selection (15s)");
        yield return new WaitForSeconds(3f); // Shortened for testing
        
        // Phase 2: Card Playing
        currentPhase = TestGamePhase.CardPlaying;
        UpdateTestUI();
        Debug.Log("Phase 2: Card Playing - Test your existing card system!");
        TestExistingCardSystem();
        yield return new WaitForSeconds(3f);
        
        // Phase 3: Card Positioning
        currentPhase = TestGamePhase.CardPositioning;
        UpdateTestUI();
        Debug.Log("Phase 3: Card Positioning");
        yield return new WaitForSeconds(2f);
        
        // Phase 4: Initial Reveal
        currentPhase = TestGamePhase.InitialReveal;
        UpdateTestUI();
        Debug.Log("Phase 4: Initial Reveal (Cards 3 & 2)");
        TestCardReveal(false);
        yield return new WaitForSeconds(2f);
        
        // Phase 5: Betting
        currentPhase = TestGamePhase.Betting;
        UpdateTestUI();
        Debug.Log("Phase 5: Betting - Test betting system!");
        yield return new WaitForSeconds(3f);
        
        // Phase 6: Final Reveal
        currentPhase = TestGamePhase.FinalReveal;
        UpdateTestUI();
        Debug.Log("Phase 6: Final Reveal (Card 1)");
        TestCardReveal(true);
        yield return new WaitForSeconds(2f);
        
        // Game Complete
        currentPhase = TestGamePhase.Completed;
        gameActive = false;
        UpdateTestUI();
        Debug.Log("✅ Test game flow completed!");
    }
    
    #endregion
    
    #region System Testing
    
    public void TestSwapSystem()
    {
        if (testSwapsRemaining <= 0)
        {
            Debug.LogWarning("No swaps remaining");
            return;
        }
        
        Debug.Log("🔄 Testing swap system...");
        
        // Test card swap logic
        if (playerDeck != null && playerDeck.cards.Count > 0)
        {
            // Simulate swapping first selected card
            foreach (var card in playerDeck.cards)
            {
                if (card.selected)
                {
                    TestSwapCard(card);
                    break;
                }
            }
        }
        
        testSwapsRemaining--;
        UpdateTestUI();
    }
    
    private void TestSwapCard(Card card)
    {
        // Simulate new card data from blockchain
        var newCardData = new TestCardData
        {
            value = UnityEngine.Random.Range(1, 14),
            suit = UnityEngine.Random.Range(0, 4),
            position = card.ParentIndex()
        };
        
        // Update card with test data
        card.cardValue = newCardData.value;
        card.cardRank = newCardData.suit;
        
        // Update visual if available
        if (card.cardVisual != null)
        {
            card.cardVisual.SetCardVisual(newCardData.value, newCardData.suit);
            card.cardVisual.TurnCardDown();
            StartCoroutine(DelayedCardReveal(card, 0.5f));
        }
        
        Debug.Log($"✅ Swapped card: {newCardData.value} of suit {newCardData.suit}");
    }
    
    private IEnumerator DelayedCardReveal(Card card, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (card.cardVisual != null)
        {
            card.cardVisual.TurnCardUp();
        }
    }
    
    public void TestBettingSystem()
    {
        Debug.Log("💰 Testing betting system...");
        
        // Simulate betting actions
        string[] bettingActions = { "RAISE", "CALL", "FOLD", "REVEAL" };
        string selectedAction = bettingActions[UnityEngine.Random.Range(0, bettingActions.Length)];
        
        switch (selectedAction)
        {
            case "RAISE":
                testBetAmount += 0.005m;
                Debug.Log($"🔼 RAISE to {testBetAmount:F3} SOL");
                break;
            case "CALL":
                Debug.Log($"📞 CALL {testBetAmount:F3} SOL");
                break;
            case "FOLD":
                Debug.Log("🏳️ FOLD - Player loses 1 heart");
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage();
                }
                break;
            case "REVEAL":
                Debug.Log("🎭 REVEAL - End betting phase");
                currentPhase = TestGamePhase.FinalReveal;
                break;
        }
        
        UpdateTestUI();
    }
    
    public void TestRevealSystem()
    {
        Debug.Log("🎭 Testing reveal system...");
        TestCardReveal(true);
    }
    
    private void TestCardReveal(bool isFinalReveal)
    {
        if (playerDeck == null) return;
        
        foreach (var card in playerDeck.cards)
        {
            if (card.isPlayed && card.cardVisual != null)
            {
                // Simulate reveal animation
                card.cardVisual.TurnCardUp();
                
                if (isFinalReveal)
                {
                    Debug.Log($"🎭 Final reveal: {card.cardValue} of suit {card.cardRank}");
                }
                else
                {
                    Debug.Log($"👁️ Initial reveal: {card.cardValue} of suit {card.cardRank}");
                }
            }
        }
    }
    
    private void TestExistingCardSystem()
    {
        Debug.Log("🃏 Testing integration with existing card system...");
        
        if (gameManager != null)
        {
            Debug.Log("✅ GameManager found and accessible");
            
            if (gameManager.playerDeck != null)
            {
                Debug.Log($"✅ Player deck found with {gameManager.playerDeck.cards.Count} cards");
                
                // Test existing functionality
                if (gameManager.playerDeck.cards.Count > 0)
                {
                    var firstCard = gameManager.playerDeck.cards[0];
                    Debug.Log($"✅ First card: {firstCard.cardValue} of suit {firstCard.cardRank}");
                    
                    if (firstCard.cardVisual != null)
                    {
                        Debug.Log("✅ Card visual system accessible");
                    }
                }
            }
        }
        
        if (playerHealth != null)
        {
            Debug.Log($"✅ Player health system found: {playerHealth.currentHealth} hearts");
        }
    }
    
    #endregion
    
    #region Relic Testing
    
    public void TestRelicEffects()
    {
        Debug.Log("🔮 Testing relic effects...");
        
        // Test High Stakes
        Debug.Log("⚡ High Stakes: 2x heart damage");
        
        // Test Plan B
        Debug.Log("📋 Plan B: 6 swaps, 2-card play");
        testSwapsRemaining = 6;
        
        // Test Third Eye
        Debug.Log("👁️ Third Eye: First swap revealed");
        
        // Test Fair Game
        Debug.Log("⚖️ Fair Game: All relics negated");
        testSwapsRemaining = 3; // Reset to base
        
        // Test The Closer
        Debug.Log("🎯 The Closer: Exact 21 bonus");
        
        // Test Fast Hand
        Debug.Log("⚡ Fast Hand: +1 swap, 15s timer");
        testSwapsRemaining = 4;
        
        UpdateTestUI();
    }
    
    #endregion
    
    #region Public Testing Methods
    
    /// <summary>
    /// Test the complete integration without SDK dependencies
    /// </summary>
    public void RunCompleteIntegrationTest()
    {
        StartCoroutine(CompleteIntegrationTestCoroutine());
    }
    
    private IEnumerator CompleteIntegrationTestCoroutine()
    {
        Debug.Log("🚀 Starting Complete Integration Test...");
        
        // 1. Test server connection
        yield return StartCoroutine(TestServerConnectionCoroutine());
        
        if (!serverConnected)
        {
            Debug.LogError("❌ Integration test failed: Server not accessible");
            yield break;
        }
        
        // 2. Simulate wallet connection
        yield return StartCoroutine(SimulateWalletConnectionCoroutine());
        
        // 3. Test existing systems
        TestExistingCardSystem();
        yield return new WaitForSeconds(1f);
        
        // 4. Test relic effects
        TestRelicEffects();
        yield return new WaitForSeconds(1f);
        
        // 5. Test game flow
        yield return StartCoroutine(TestGameFlowCoroutine());
        
        Debug.Log("✅ Complete Integration Test Finished!");
        Debug.Log("📋 Next Steps:");
        Debug.Log("1. Install Solana Unity SDK (see SETUP_AND_TESTING_GUIDE.md)");
        Debug.Log("2. Configure Unity Inspector references");
        Debug.Log("3. Test with real Solana wallet");
        Debug.Log("4. Test with actual smart contract");
    }
    
    #endregion
    
    #region Data Structures
    
    public enum TestGamePhase
    {
        Waiting,
        RelicSelection,
        CardPlaying,
        CardPositioning,
        InitialReveal,
        Betting,
        FinalReveal,
        Completed
    }
    
    [System.Serializable]
    public class TestCardData
    {
        public int value;
        public int suit;
        public int position;
    }
    
    #endregion
    
    #region Utility
    
    void Update()
    {
        // Test keyboard shortcuts for quick testing
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TestServerConnection();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SimulateWalletConnection();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            StartTestGameFlow();
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            RunCompleteIntegrationTest();
        }
        
        if (Input.GetKeyDown(KeyCode.F5))
        {
            TestRelicEffects();
        }
    }
    
    void OnGUI()
    {
        if (showDebugInfo)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("DOLERO Test Manager - Debug Info");
            GUILayout.Label($"Server: {(serverConnected ? "✅" : "❌")}");
            GUILayout.Label($"Wallet: {(walletConnected ? "✅" : "❌")}");
            GUILayout.Label($"Phase: {currentPhase}");
            GUILayout.Label($"Game Active: {gameActive}");
            GUILayout.Label("Press F1-F5 for quick tests");
            GUILayout.EndArea();
        }
    }
    
    [SerializeField] private bool showDebugInfo = true;
    
    #endregion
}
