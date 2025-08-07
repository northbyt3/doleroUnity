using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Manages Solana wallet connection and transactions for DOLERO
/// Integrates with the Solana Unity SDK
/// </summary>
public class DoleroWalletManager : MonoBehaviour
{
    [Header("Wallet Settings")]
    public bool autoConnectWallet = false;
    public string rpcUrl = "https://api.devnet.solana.com"; // Devnet for testing
    
    [Header("Wallet Status")]
    public bool isWalletConnected = false;
    public string walletAddress = "";
    public double walletBalance = 0.0;
    public string connectionStatus = "Disconnected";
    
    // Events
    public static event Action<bool> OnWalletConnectionChanged;
    public static event Action<string> OnWalletAddressChanged;
    public static event Action<double> OnBalanceChanged;
    public static event Action<string> OnTransactionCompleted;
    public static event Action<string> OnTransactionFailed;
    
    // Wallet instance
    private object walletInstance; // Will be Solana.Unity.Wallet.Wallet
    private object rpcClient; // Will be Solana.Unity.Rpc.RpcClient
    
    public static DoleroWalletManager Instance { get; private set; }
    
    // Transaction types for DOLERO
    public enum TransactionType
    {
        JoinGame,
        PlaceBet,
        SelectRelic,
        PlayCard,
        SwapCards,
        RevealCards
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
        if (autoConnectWallet)
        {
            ConnectWallet();
        }
    }
    
    /// <summary>
    /// Connect to Solana wallet (Phantom, Solflare, etc.)
    /// </summary>
    public void ConnectWallet()
    {
        StartCoroutine(ConnectWalletCoroutine());
    }
    
    IEnumerator ConnectWalletCoroutine()
    {
        Debug.Log("🔗 Connecting to Solana wallet...");
        connectionStatus = "Connecting...";
        
        try
        {
            // Initialize RPC client
            InitializeRpcClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Wallet connection error: {e.Message}");
            connectionStatus = $"Error: {e.Message}";
            OnWalletConnectionChanged?.Invoke(false);
            yield break;
        }
        
        // Try to connect to wallet
        yield return StartCoroutine(ConnectToWallet());
        
        if (isWalletConnected)
        {
            // Get wallet info
            yield return StartCoroutine(GetWalletInfo());
            
            Debug.Log($"✅ Wallet connected: {walletAddress}");
            Debug.Log($"💰 Balance: {walletBalance} SOL");
            
            OnWalletConnectionChanged?.Invoke(true);
            OnWalletAddressChanged?.Invoke(walletAddress);
            OnBalanceChanged?.Invoke(walletBalance);
        }
        else
        {
            Debug.LogError("❌ Failed to connect wallet");
            connectionStatus = "Connection failed";
            OnWalletConnectionChanged?.Invoke(false);
        }
    }
    
    void InitializeRpcClient()
    {
        try
        {
            // Initialize RPC client using Solana Unity SDK
            // This will be implemented based on the actual SDK structure
            Debug.Log($"📡 Initializing RPC client for {rpcUrl}");
            
            // Example structure (actual implementation depends on SDK)
            // rpcClient = new Solana.Unity.Rpc.RpcClient(rpcUrl);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize RPC client: {e.Message}");
            throw;
        }
    }
    
    IEnumerator ConnectToWallet()
    {
        Debug.Log("🔐 Attempting wallet connection...");
        
        // Simulate connection process
        yield return new WaitForSeconds(1f);
        
        try
        {
            // Try to connect to wallet using Solana Unity SDK
            // This is a placeholder - actual implementation depends on SDK structure
            
            // For now, simulate a successful connection
            // In real implementation, this would use the actual SDK
            isWalletConnected = true;
            walletAddress = "DemoWallet1234567890abcdef";
            connectionStatus = "Connected";
            
            Debug.Log("✅ Wallet connection successful (demo mode)");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Wallet connection failed: {e.Message}");
            isWalletConnected = false;
            connectionStatus = "Connection failed";
        }
    }
    
    IEnumerator GetWalletInfo()
    {
        if (!isWalletConnected) yield break;
        
        try
        {
            // Get wallet balance and other info
            // This would use the actual SDK methods
            
            // Simulate getting balance
            walletBalance = 1.5; // Demo balance
            Debug.Log($"💰 Wallet balance: {walletBalance} SOL");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get wallet info: {e.Message}");
        }
    }
    
    /// <summary>
    /// Send a transaction to the DOLERO smart contract
    /// </summary>
    public void SendTransaction(TransactionType type, object data)
    {
        if (!isWalletConnected)
        {
            Debug.LogError("❌ Wallet not connected!");
            return;
        }
        
        StartCoroutine(SendTransactionCoroutine(type, data));
    }
    
    IEnumerator SendTransactionCoroutine(TransactionType type, object data)
    {
        Debug.Log($"📤 Sending transaction: {type}");
        
        // Create transaction data
        var transactionData = new
        {
            type = type.ToString(),
            data = data,
            walletAddress = walletAddress,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        
        try
        {
            // Send to server first (for game state)
            if (SimpleDirectWebSocket.Instance != null)
            {
                SimpleDirectWebSocket.Instance.SendMessage("transaction", transactionData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to send to server: {e.Message}");
            OnTransactionFailed?.Invoke(e.Message);
            yield break;
        }
        
        // Then send to blockchain
        yield return StartCoroutine(SendToBlockchain(type, data));
        
        Debug.Log($"✅ Transaction completed: {type}");
        OnTransactionCompleted?.Invoke(type.ToString());
    }
    
    IEnumerator SendToBlockchain(TransactionType type, object data)
    {
        // This would use the actual Solana SDK to send transactions
        // For now, we'll simulate the process
        
        Debug.Log($"🔗 Sending to blockchain: {type}");
        
        // Simulate blockchain transaction
        yield return new WaitForSeconds(2f);
        
        // In real implementation, this would:
        // 1. Create transaction
        // 2. Sign with wallet
        // 3. Send to network
        // 4. Wait for confirmation
        
        Debug.Log($"✅ Blockchain transaction confirmed: {type}");
    }
    
    /// <summary>
    /// Join a DOLERO game (requires wallet connection)
    /// </summary>
    public void JoinGame(string gameId = null)
    {
        if (!isWalletConnected)
        {
            Debug.LogError("❌ Wallet not connected! Connect wallet first.");
            return;
        }
        
        var gameData = new
        {
            gameId = gameId ?? Guid.NewGuid().ToString(),
            entryFee = 0.1, // SOL
            timestamp = DateTime.UtcNow.ToString("o")
        };
        
        SendTransaction(TransactionType.JoinGame, gameData);
    }
    
    /// <summary>
    /// Place a bet in the game
    /// </summary>
    public void PlaceBet(string action, double amount)
    {
        if (!isWalletConnected)
        {
            Debug.LogError("❌ Wallet not connected!");
            return;
        }
        
        var betData = new
        {
            action = action, // RAISE, CALL, FOLD, REVEAL
            amount = amount,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        
        SendTransaction(TransactionType.PlaceBet, betData);
    }
    
    /// <summary>
    /// Select a relic (requires wallet for verification)
    /// </summary>
    public void SelectRelic(int relicId)
    {
        if (!isWalletConnected)
        {
            Debug.LogError("❌ Wallet not connected!");
            return;
        }
        
        var relicData = new
        {
            relicId = relicId,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        
        SendTransaction(TransactionType.SelectRelic, relicData);
    }
    
    /// <summary>
    /// Disconnect wallet
    /// </summary>
    public void DisconnectWallet()
    {
        Debug.Log("🔌 Disconnecting wallet...");
        
        isWalletConnected = false;
        walletAddress = "";
        walletBalance = 0.0;
        connectionStatus = "Disconnected";
        
        OnWalletConnectionChanged?.Invoke(false);
        OnWalletAddressChanged?.Invoke("");
        OnBalanceChanged?.Invoke(0.0);
        
        Debug.Log("✅ Wallet disconnected");
    }
    
    /// <summary>
    /// Get current wallet status
    /// </summary>
    public WalletStatus GetWalletStatus()
    {
        return new WalletStatus
        {
            isConnected = isWalletConnected,
            address = walletAddress,
            balance = walletBalance,
            status = connectionStatus
        };
    }
    
    [Serializable]
    public class WalletStatus
    {
        public bool isConnected;
        public string address;
        public double balance;
        public string status;
    }
    
    void OnDestroy()
    {
        if (isWalletConnected)
        {
            DisconnectWallet();
        }
    }
}
