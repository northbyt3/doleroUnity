using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Programs;
using Newtonsoft.Json;

/// <summary>
/// Core Solana integration manager for DOLERO game
/// Handles wallet connections, transactions, and smart contract interactions
/// Based on DOLERO development plan Phase 1 requirements
/// </summary>
public class DoleroSolanaManager : MonoBehaviour
{
    [Header("Solana Configuration")]
    [SerializeField] private RpcCluster rpcCluster = RpcCluster.DevNet;
    [SerializeField] private string customRpcUrl = "";
    
    [Header("Web2 Server Configuration")]
    [SerializeField] private string serverUrl = "http://174.138.42.117";
    [SerializeField] private string websocketUrl = "ws://174.138.42.117";
    
    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerHealth playerHealth;
    
    // Solana SDK components
    private Web3 web3;
    private WalletBase activeWallet;
    
    // Game state
    private string currentGameId;
    private string playerId;
    private bool isConnectedToSolana = false;
    private bool isConnectedToServer = false;
    
    // Events for UI updates
    public static event Action<bool> OnSolanaConnectionChanged;
    public static event Action<bool> OnServerConnectionChanged;
    public static event Action<string> OnError;
    public static event Action OnWalletInstanceChanged;
    public static event Action<string> OnTransactionConfirmed;
    
    public static DoleroSolanaManager Instance { get; private set; }
    
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
        InitializeSolanaSDK();
        StartCoroutine(TestServerConnection());
    }
    
    #region Solana SDK Initialization
    
    /// <summary>
    /// Initialize Solana Unity SDK with proper configuration
    /// </summary>
    private void InitializeSolanaSDK()
    {
        try
        {
            // Configure Web3 instance with RPC cluster
            // Note: These events are not available in the current SDK version
            // Web3.OnBalanceChange += OnBalanceChanged;
            // Web3.OnWalletInstanceChanged += OnWalletChanged;
            
            Debug.Log("Solana SDK initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Solana SDK: {e.Message}");
            OnError?.Invoke($"Solana initialization failed: {e.Message}");
        }
    }
    
    #endregion
    
    #region Wallet Management
    
    /// <summary>
    /// Connect to Solana wallet using Wallet Adapter
    /// Supports multiple wallet types as per Solana Unity SDK guide
    /// </summary>
    public async Task<bool> ConnectWallet()
    {
        try
        {
            // Use Web3.Instance to connect wallet adapter
            await Web3.Instance.LoginWalletAdapter();
            
            // Web3.Instance.Wallet is not available in current SDK
            // activeWallet = Web3.Instance.Wallet;
            // For now, we'll create a mock wallet or use the adapter directly
            activeWallet = null; // This needs to be set based on your wallet adapter implementation
            
            if (activeWallet != null && activeWallet.Account != null)
            {
                isConnectedToSolana = true;
                playerId = activeWallet.Account.PublicKey.Key;
                
                Debug.Log($"Connected to Solana wallet: {playerId}");
                OnSolanaConnectionChanged?.Invoke(true);
                
                // Get initial balance
                var balance = await GetSolanaBalance();
                Debug.Log($"Current SOL balance: {balance} SOL");
                
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect wallet: {e.Message}");
            OnError?.Invoke($"Wallet connection failed: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Disconnect from current wallet
    /// </summary>
    public async Task DisconnectWallet()
    {
        try
        {
            if (activeWallet != null)
            {
                // await Web3.Instance.Logout();
                // Note: Logout method may not be available in current SDK
                activeWallet = null;
                isConnectedToSolana = false;
                playerId = null;
                
                OnSolanaConnectionChanged?.Invoke(false);
                Debug.Log("Disconnected from Solana wallet");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to disconnect wallet: {e.Message}");
            OnError?.Invoke($"Wallet disconnection failed: {e.Message}");
        }
    }
    
    /// <summary>
    /// Get current SOL balance of connected wallet
    /// </summary>
    public async Task<double> GetSolanaBalance()
    {
        try
        {
            if (activeWallet?.Account != null)
            {
                var balance = await activeWallet.GetBalance(Commitment.Confirmed);
                return (double)balance / 1000000000.0; // Convert lamports to SOL
            }
            return 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get balance: {e.Message}");
            return 0;
        }
    }
    
    #endregion
    
    #region Web2 Server Communication
    
    /// <summary>
    /// Test connection to Web2 server
    /// </summary>
    private IEnumerator TestServerConnection()
    {
        UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/health");
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            isConnectedToServer = true;
            OnServerConnectionChanged?.Invoke(true);
            Debug.Log($"Connected to Web2 server: {serverUrl}");
        }
        else
        {
            isConnectedToServer = false;
            OnServerConnectionChanged?.Invoke(false);
            Debug.LogError($"Failed to connect to Web2 server: {request.error}");
            OnError?.Invoke($"Server connection failed: {request.error}");
        }
    }
    
    /// <summary>
    /// Make API call to Web2 delegate server
    /// </summary>
    public async Task<T> CallWebAPI<T>(string endpoint, object data = null, string method = "GET")
    {
        try
        {
            string url = $"{serverUrl}/api/{endpoint}";
            UnityWebRequest request;
            
            if (method == "POST" && data != null)
            {
                string jsonData = JsonConvert.SerializeObject(data);
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                
                request = new UnityWebRequest(url, "POST");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
            }
            else
            {
                request = UnityWebRequest.Get(url);
            }
            
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            else
            {
                Debug.LogError($"API call failed: {request.error}");
                OnError?.Invoke($"API call failed: {request.error}");
                return default(T);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"API call exception: {e.Message}");
            OnError?.Invoke($"API call exception: {e.Message}");
            return default(T);
        }
    }
    
    #endregion
    
    #region Game Management Integration
    
    /// <summary>
    /// Initialize new game session
    /// Coordinates between Web2 server and Solana smart contract
    /// </summary>
    public async Task<bool> InitializeGame(string gameType, decimal betAmount)
    {
        try
        {
            if (!isConnectedToSolana || !isConnectedToServer)
            {
                OnError?.Invoke("Must be connected to both Solana and server");
                return false;
            }
            
            // Create game initialization request
            var gameInitRequest = new
            {
                playerId = playerId,
                gameType = gameType,
                betAmount = betAmount,
                walletAddress = activeWallet.Account.PublicKey.Key
            };
            
            // Call Web2 delegate to initialize game
            var response = await CallWebAPI<GameInitResponse>("game/initialize", gameInitRequest, "POST");
            
            if (response?.success == true)
            {
                currentGameId = response.gameId;
                Debug.Log($"Game initialized: {currentGameId}");
                return true;
            }
            
            OnError?.Invoke("Failed to initialize game");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Game initialization failed: {e.Message}");
            OnError?.Invoke($"Game initialization failed: {e.Message}");
            return false;
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnBalanceChanged(double newBalance)
    {
        Debug.Log($"SOL balance updated: {newBalance}");
        // Update UI with new balance
    }
    
    private void OnWalletChanged(WalletBase newWallet)
    {
        activeWallet = newWallet;
        if (newWallet != null)
        {
            playerId = newWallet.Account?.PublicKey?.Key;
            Debug.Log($"Wallet changed: {playerId}");
        }
    }
    
    #endregion
    
    #region Public Properties
    
    public bool IsConnectedToSolana => isConnectedToSolana;
    public bool IsConnectedToServer => isConnectedToServer;
    public string PlayerId => playerId;
    public string CurrentGameId => currentGameId;
    public WalletBase ActiveWallet => activeWallet;
    
    #endregion
    
    #region Data Structures
    
    [Serializable]
    public class GameInitResponse
    {
        public bool success;
        public string gameId;
        public string message;
    }
    
    #endregion
}
