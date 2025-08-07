using UnityEngine;

/// <summary>
/// Simple test to verify wallet integration works
/// </summary>
public class WalletTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool autoTestOnStart = true;
    public bool testWalletConnection = true;
    public bool testTransactions = true;
    
    private DoleroWalletManager walletManager;
    
    void Start()
    {
        if (autoTestOnStart)
        {
            StartCoroutine(RunWalletTests());
        }
    }
    
    System.Collections.IEnumerator RunWalletTests()
    {
        Debug.Log("🧪 Starting wallet integration tests...");
        
        // Wait for wallet manager to be available
        yield return new WaitForSeconds(1f);
        
        walletManager = FindObjectOfType<DoleroWalletManager>();
        if (walletManager == null)
        {
            Debug.LogError("❌ DoleroWalletManager not found!");
            yield break;
        }
        
        Debug.Log("✅ Wallet manager found");
        
        if (testWalletConnection)
        {
            yield return StartCoroutine(TestWalletConnection());
        }
        
        if (testTransactions && walletManager.isWalletConnected)
        {
            yield return StartCoroutine(TestTransactions());
        }
        
        Debug.Log("🧪 Wallet integration tests completed!");
    }
    
    System.Collections.IEnumerator TestWalletConnection()
    {
        Debug.Log("🔗 Testing wallet connection...");
        
        // Subscribe to wallet events
        DoleroWalletManager.OnWalletConnectionChanged += OnWalletConnectionChanged;
        DoleroWalletManager.OnTransactionCompleted += OnTransactionCompleted;
        
        // Connect wallet
        walletManager.ConnectWallet();
        
        // Wait for connection
        yield return new WaitForSeconds(3f);
        
        if (walletManager.isWalletConnected)
        {
            Debug.Log("✅ Wallet connection test passed!");
        }
        else
        {
            Debug.LogError("❌ Wallet connection test failed!");
        }
    }
    
    System.Collections.IEnumerator TestTransactions()
    {
        Debug.Log("📤 Testing transactions...");
        
        // Test 1: Join Game
        Debug.Log("🎮 Testing Join Game transaction...");
        walletManager.JoinGame("test-game-001");
        yield return new WaitForSeconds(2f);
        
        // Test 2: Select Relic
        Debug.Log("🔮 Testing Select Relic transaction...");
        walletManager.SelectRelic(1);
        yield return new WaitForSeconds(2f);
        
        // Test 3: Place Bet
        Debug.Log("💰 Testing Place Bet transaction...");
        walletManager.PlaceBet("RAISE", 0.1);
        yield return new WaitForSeconds(2f);
        
        Debug.Log("✅ Transaction tests completed!");
    }
    
    void OnWalletConnectionChanged(bool connected)
    {
        if (connected)
        {
            Debug.Log("✅ Wallet connected successfully!");
        }
        else
        {
            Debug.Log("❌ Wallet disconnected");
        }
    }
    
    void OnTransactionCompleted(string transactionType)
    {
        Debug.Log($"✅ Transaction completed: {transactionType}");
    }
    
    [ContextMenu("Test Wallet Connection")]
    public void TestWalletConnectionManual()
    {
        StartCoroutine(TestWalletConnection());
    }
    
    [ContextMenu("Test Transactions")]
    public void TestTransactionsManual()
    {
        if (walletManager != null && walletManager.isWalletConnected)
        {
            StartCoroutine(TestTransactions());
        }
        else
        {
            Debug.LogError("❌ Wallet not connected! Connect wallet first.");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        DoleroWalletManager.OnWalletConnectionChanged -= OnWalletConnectionChanged;
        DoleroWalletManager.OnTransactionCompleted -= OnTransactionCompleted;
    }
}
