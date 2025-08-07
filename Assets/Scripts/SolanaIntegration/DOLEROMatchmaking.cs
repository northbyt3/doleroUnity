using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;

/// <summary>
/// DOLERO Matchmaking Client for Unity
/// Handles WebSocket connection to the DOLERO matchmaking server
/// Follows the exact protocol specified in README-UNITY.md
/// </summary>
public class DOLEROMatchmaking : MonoBehaviour
{
    [Header("Server Configuration")]
    public TMPro.TMP_InputField serverUrlInputField;
    [SerializeField] private float heartbeatInterval = 30f;
    [SerializeField] private float reconnectDelay = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // WebSocket connection
    private WebSocket webSocket;
    private bool isConnected = false;
    private bool isConnectionEstablished = false;
    private bool isAuthenticated = false;
    private string sessionId;
    private string playerId;
    
    // Heartbeat system
    private Coroutine heartbeatCoroutine;
    private DateTime lastHeartbeat;
    
    // Matchmaking state
    private bool isInMatchmaking = false;
    private string currentTableType;
    
    // Game session state
    private string currentGameSessionId;
    private string opponentAddress;
    private long playInAmount;
    
    // Events
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnAuthenticationSuccess;
    public event Action<string> OnAuthenticationFailed;
    public event Action<string> OnMatchmakingStarted;
    public event Action OnMatchmakingCancelled;
    public event Action<string, string, string, long> OnMatchFound; // gameSessionId, opponentAddress, tableType, playInAmount
    public event Action<string> OnOpponentDisconnected;
    public event Action<string, string> OnError; // errorCode, message
    public event Action<string, object> OnGameStateUpdate; // state, data
    
    // Message classes following README-UNITY.md format
    [System.Serializable]
    public class WebSocketMessage
    {
        public string type;
        public long timestamp;
    }
    
    [System.Serializable]
    public class AuthenticationMessage : WebSocketMessage
    {
        public string publicAddress;
    }
    
    [System.Serializable]
    public class AuthenticationResponseMessage : WebSocketMessage
    {
        public string status;
        public string sessionId;
        public string playerId;
    }
    
    [System.Serializable]
    public class RequestMatchMessage
    {
        public string type;
        public string tableType;
        public string playerId;
        // Removed session_id and timestamp - server doesn't use them
    }
    
    [System.Serializable]
    public class MatchFoundMessage : WebSocketMessage
    {
        public string gameSessionId;
        public string player1;
        public string player2;
        public string tableType;
        public long playInAmount;
    }
    
    [System.Serializable]
    public class GameStateUpdateMessage : WebSocketMessage
    {
        public string state;
        public string gameSessionId;
        public object data;
    }
    
    [System.Serializable]
    public class ErrorMessage : WebSocketMessage
    {
        public string message;
        public string code;
    }
    
    [System.Serializable]
    public class DisconnectionMessage : WebSocketMessage
    {
        public string message;
    }
    
    [System.Serializable]
    public class ConnectionEstablishedMessage : WebSocketMessage
    {
        public string connectionId;
    }
    
    [System.Serializable]
    public class HeartbeatMessage
    {
        public string type;
        // No additional fields needed - no timestamp
    }
    
    void Start()
    {
        // Don't auto-connect on start - let user control connection
    }
    
    void OnDestroy()
    {
        Disconnect();
    }
    
    /// <summary>
    /// Initialize WebSocket connection
    /// </summary>
    private void InitializeWebSocket()
    {
        try
        {
            string targetUrl = $"ws://{(string.IsNullOrEmpty(serverUrlInputField.text) ? "localhost" : serverUrlInputField.text)}:3000";
            
            LogDebug("Initializing WebSocket connection to: " + targetUrl);
            
            webSocket = new WebSocket(targetUrl);
            webSocket.OnOpen += OnWebSocketOpen;
            webSocket.OnMessage += OnWebSocketMessage;
            webSocket.OnClose += OnWebSocketClose;
            webSocket.OnError += OnWebSocketError;
            
            webSocket.Connect();
        }
        catch (Exception e)
        {
            LogError("Failed to initialize WebSocket: " + e.Message);
        }
    }
    
    /// <summary>
    /// Handle WebSocket connection opened
    /// </summary>
    private void OnWebSocketOpen(object sender, EventArgs e)
    {
        isConnected = true;
        LogDebug("WebSocket connection established");
        
        // Reset authentication state on new connection
        isConnectionEstablished = false;
        isAuthenticated = false;
        sessionId = null;
        playerId = null;
        
        // Start heartbeat
        StartHeartbeat();
        
        OnConnected?.Invoke();
    }
    
    /// <summary>
    /// Handle WebSocket messages
    /// </summary>
    private void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        try
        {
            LogDebug("Received message: " + e.Data);
            var message = JsonConvert.DeserializeObject<WebSocketMessage>(e.Data);
            HandleMessage(message, e.Data);
        }
        catch (Exception ex)
        {
            LogError("Failed to parse WebSocket message: " + ex.Message);
        }
    }
    
    /// <summary>
    /// Handle WebSocket connection closed
    /// </summary>
    private void OnWebSocketClose(object sender, CloseEventArgs e)
    {
        isConnected = false;
        isConnectionEstablished = false;
        isAuthenticated = false;
        sessionId = null;
        playerId = null;
        
        LogDebug("WebSocket connection closed: " + e.Reason);
        
        // Stop heartbeat
        StopHeartbeat();
        
        // Attempt to reconnect
        StartCoroutine(ReconnectAfterDelay());
        
        OnDisconnected?.Invoke();
    }
    
    /// <summary>
    /// Handle WebSocket errors
    /// </summary>
    private void OnWebSocketError(object sender, ErrorEventArgs e)
    {
        LogError("WebSocket error: " + e.Message);
    }
    
    /// <summary>
    /// Handle incoming messages based on type
    /// </summary>
    private void HandleMessage(WebSocketMessage message, string rawData)
    {
        LogDebug("Processing message type: " + message.type);
        
        switch (message.type)
        {
            case "connection_established":
                HandleConnectionEstablished(rawData);
                break;
                
            case "authentication_response":
                HandleAuthenticationResponse(rawData);
                break;
                
            case "match_found":
                HandleMatchFound(rawData);
                break;
                
            case "game_state_update":
                HandleGameStateUpdate(rawData);
                break;
                
            case "disconnection":
                HandleDisconnection(rawData);
                break;
                
            case "error":
                HandleError(rawData);
                break;
                
            default:
                LogDebug("Unknown message type: " + message.type);
                break;
        }
    }
    
    /// <summary>
    /// Handle connection established message
    /// </summary>
    private void HandleConnectionEstablished(string rawData)
    {
        try
        {
            var connectionData = JsonConvert.DeserializeObject<ConnectionEstablishedMessage>(rawData);
            LogDebug($"Connection established with server. Connection ID: {connectionData.connectionId}");
            isConnectionEstablished = true;
            // The server has confirmed the connection is ready
            // We can now proceed with authentication
        }
        catch (Exception e)
        {
            LogError("Failed to handle connection established: " + e.Message);
        }
    }
    
    /// <summary>
    /// Handle authentication response
    /// </summary>
    private void HandleAuthenticationResponse(string rawData)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<AuthenticationResponseMessage>(rawData);
            
            if (response.status == "success")
            {
                isAuthenticated = true;
                sessionId = response.sessionId;
                playerId = response.playerId;
                
                LogDebug("Authentication successful for player: " + playerId);
                LogDebug("Session ID: " + sessionId);
                
                OnAuthenticationSuccess?.Invoke(playerId);
            }
            else
            {
                LogError("Authentication failed: " + response.status);
                OnAuthenticationFailed?.Invoke("Authentication failed");
            }
        }
        catch (Exception e)
        {
            LogError("Failed to parse authentication response: " + e.Message);
        }
    }
    
    /// <summary>
    /// Handle match found
    /// </summary>
    private void HandleMatchFound(string rawData)
    {
        try
        {
            var matchData = JsonConvert.DeserializeObject<MatchFoundMessage>(rawData);
            
            isInMatchmaking = false;
            currentGameSessionId = matchData.gameSessionId;
            opponentAddress = matchData.player1 == playerId ? matchData.player2 : matchData.player1;
            playInAmount = matchData.playInAmount;
            
            LogDebug($"Match found! Game Session: {currentGameSessionId}, Opponent: {opponentAddress}, Table: {matchData.tableType}, Play-in: {playInAmount}");
            
            OnMatchFound?.Invoke(currentGameSessionId, opponentAddress, matchData.tableType, playInAmount);
        }
        catch (Exception e)
        {
            LogError("Failed to parse match found message: " + e.Message);
        }
    }
    
    /// <summary>
    /// Handle game state update
    /// </summary>
    private void HandleGameStateUpdate(string rawData)
    {
        try
        {
            var stateData = JsonConvert.DeserializeObject<GameStateUpdateMessage>(rawData);
            
            LogDebug($"Game state update: {stateData.state} for session: {stateData.gameSessionId}");
            
            OnGameStateUpdate?.Invoke(stateData.state, stateData.data);
        }
        catch (Exception e)
        {
            LogError("Failed to parse game state update: " + e.Message);
        }
    }
    
    /// <summary>
    /// Handle disconnection
    /// </summary>
    private void HandleDisconnection(string rawData)
    {
        try
        {
            var disconnectionData = JsonConvert.DeserializeObject<DisconnectionMessage>(rawData);
            
            LogDebug("Opponent disconnected: " + disconnectionData.message);
            
            OnOpponentDisconnected?.Invoke(currentGameSessionId);
        }
        catch (Exception e)
        {
            LogError("Failed to parse disconnection message: " + e.Message);
        }
    }
    
    /// <summary>
    /// Handle error messages
    /// </summary>
    private void HandleError(string rawData)
    {
        try
        {
            var errorData = JsonConvert.DeserializeObject<ErrorMessage>(rawData);
            
            LogError($"Server error: {errorData.code} - {errorData.message}");
            
            // Handle specific authentication errors
            if (errorData.code == "AUTH_REQUIRED" || errorData.code == "INVALID_ADDRESS")
            {
                isAuthenticated = false;
                sessionId = null;
                playerId = null;
                OnAuthenticationFailed?.Invoke(errorData.message);
            }
            
            OnError?.Invoke(errorData.code, errorData.message);
        }
        catch (Exception e)
        {
            LogError("Failed to parse error message: " + e.Message);
        }
    }
    
    /// <summary>
    /// Authenticate user with public address (following README format)
    /// </summary>
    public void AuthenticateUser(string publicAddress)
    {
        if (!isConnected)
        {
            LogError("Cannot authenticate: not connected to server");
            return;
        }
        
        if (!isConnectionEstablished)
        {
            LogError("Cannot authenticate: connection not yet established with server");
            return;
        }
        
        var authMessage = new AuthenticationMessage
        {
            type = "authentication",
            publicAddress = publicAddress,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        
        SendMessage(authMessage);
        LogDebug("Authentication request sent for player: " + publicAddress);
    }
    
    /// <summary>
    /// Wrapper function for Unity events - Authenticate with connected wallet
    /// </summary>
    public void AuthenticateUser()
    {
        // Check if wallet is connected
        if (Web3.Instance == null || Web3.Instance.WalletBase == null || Web3.Instance.WalletBase.Account == null)
        {
            LogError("Cannot authenticate: No wallet connected. Please connect your wallet first.");
            return;
        }
        
        // Get the wallet address
        string walletAddress = Web3.Instance.WalletBase.Account.PublicKey.ToString();
        
        LogDebug($"Authenticating with wallet: {walletAddress}");
        AuthenticateUser(walletAddress);
    }
    
    /// <summary>
    /// Wrapper function for Unity events - Authenticate with test values
    /// </summary>
    public void AuthenticateUserTest()
    {
        AuthenticateUser("11111111111111111111111111111111");
    }
    
    /// <summary>
    /// Wrapper function for Unity events - Authenticate with connected wallet
    /// </summary>
    public void AuthenticateUserWithWallet()
    {
        if (IsWalletConnected())
        {
            string walletAddress = GetWalletAddress();
            LogDebug($"Authenticating with connected wallet: {walletAddress}");
            AuthenticateUser(walletAddress);
        }
        else
        {
            LogError("Cannot authenticate: No wallet connected. Please connect your wallet first.");
        }
    }
    
    /// <summary>
    /// Test sequence: Connect, wait for connection established, then authenticate
    /// </summary>
    public void TestConnectionSequence()
    {
        LogDebug("Starting test connection sequence...");
        Connect();
        
        // Wait a bit for connection to establish, then authenticate
        StartCoroutine(TestSequenceCoroutine());
    }
    
    private IEnumerator TestSequenceCoroutine()
    {
        // Wait for connection to be established
        float timeout = 10f;
        float elapsed = 0f;
        
        while (!isConnectionEstablished && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (isConnectionEstablished)
        {
            LogDebug("Connection established, proceeding with authentication...");
            AuthenticateUserTest();
        }
        else
        {
            LogError("Connection not established within timeout period");
        }
    }
    
    /// <summary>
    /// Complete test sequence: Connect, authenticate, then request match
    /// </summary>
    public void TestCompleteSequence()
    {
        LogDebug("Starting complete test sequence...");
        Connect();
        
        // Wait for connection to establish, then authenticate, then request match
        StartCoroutine(CompleteTestSequenceCoroutine());
    }
    
    private IEnumerator CompleteTestSequenceCoroutine()
    {
        // Step 1: Wait for connection to be established
        float timeout = 10f;
        float elapsed = 0f;
        
        while (!isConnectionEstablished && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (!isConnectionEstablished)
        {
            LogError("Connection not established within timeout period");
            yield break;
        }
        
        LogDebug("Connection established, proceeding with authentication...");
        
        // Use actual wallet address if connected, otherwise use test address
        if (IsWalletConnected())
        {
            string walletAddress = GetWalletAddress();
            LogDebug($"Using connected wallet address: {walletAddress}");
            AuthenticateUser(walletAddress);
        }
        else
        {
            LogDebug("No wallet connected, using test address");
            AuthenticateUserTest();
        }
        
        // Step 2: Wait for authentication to complete
        elapsed = 0f;
        while (!isAuthenticated && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (!isAuthenticated)
        {
            LogError("Authentication not completed within timeout period");
            yield break;
        }
        
        LogDebug("Authentication successful, sending heartbeat to maintain session...");
        SendHeartbeat();
        
        // Step 3: Wait a bit after authentication before requesting match
        yield return new WaitForSeconds(1f);
        
        LogDebug("Proceeding with match request...");
        RequestMatchSmall();
    }
    
    /// <summary>
    /// Start matchmaking for specified table type (following README format)
    /// </summary>
    public void RequestMatch(string tableType)
    {
        if (!isAuthenticated)
        {
            LogError("Cannot request match: not authenticated");
            return;
        }
        
        if (!isConnectionEstablished)
        {
            LogError("Cannot request match: connection not established");
            return;
        }
        
        if (isInMatchmaking)
        {
            LogError("Already in matchmaking");
            return;
        }
        
        var matchMessage = new RequestMatchMessage
        {
            type = "request_match",
            tableType = tableType,
            playerId = playerId
            // Removed session_id and timestamp - server doesn't need them
        };
        
        LogDebug($"Sending match request for table type: {tableType}");
        
        SendMessage(matchMessage);
        isInMatchmaking = true;
        currentTableType = tableType;
        
        LogDebug("Match request sent for table type: " + tableType);
        OnMatchmakingStarted?.Invoke(tableType);
    }
    
    /// <summary>
    /// Cancel current matchmaking request
    /// </summary>
    public void CancelMatchmaking()
    {
        if (!isAuthenticated)
        {
            LogError("Cannot cancel matchmaking: not authenticated");
            return;
        }
        
        if (!isInMatchmaking)
        {
            LogError("Not currently in matchmaking");
            return;
        }
        
        var cancelMessage = new
        {
            type = "cancel_match",
            playerId = playerId,
            tableType = currentTableType
            // Removed session_id and timestamp - server doesn't need them
        };
        
        SendMessage(cancelMessage);
        isInMatchmaking = false;
        currentTableType = null;
        
        LogDebug("Matchmaking cancelled");
        OnMatchmakingCancelled?.Invoke();
    }
    
    /// <summary>
    /// Wrapper function for Unity events - Request match with small table
    /// </summary>
    public void RequestMatchSmall()
    {
        RequestMatch("small");
    }
    
    /// <summary>
    /// Wrapper function for Unity events - Request match with medium table
    /// </summary>
    public void RequestMatchMedium()
    {
        RequestMatch("medium");
    }
    
    /// <summary>
    /// Wrapper function for Unity events - Request match with big table
    /// </summary>
    public void RequestMatchBig()
    {
        RequestMatch("big");
    }
    
    /// <summary>
    /// Wrapper function for Unity events - Cancel matchmaking
    /// </summary>
    public void CancelMatchmakingWrapper()
    {
        CancelMatchmaking();
    }
    
    /// <summary>
    /// Send delegation ready message
    /// </summary>
    public void SendDelegationReady(string delegationId)
    {
        if (string.IsNullOrEmpty(currentGameSessionId))
        {
            LogError("Cannot send delegation ready: no active game session");
            return;
        }
        
        var message = new
        {
            type = "delegation_ready",
            gameSessionId = currentGameSessionId,
            playerId = playerId,
            delegationId = delegationId
        };
        
        SendMessage(message);
        LogDebug("Delegation ready sent for session: " + currentGameSessionId);
    }
    
    /// <summary>
    /// Send relic selection
    /// </summary>
    public void SendRelicSelection(int relicIndex, bool jokerPlus = false)
    {
        if (string.IsNullOrEmpty(currentGameSessionId))
        {
            LogError("Cannot send relic selection: no active game session");
            return;
        }
        
        var message = new
        {
            type = "relic_selection",
            gameSessionId = currentGameSessionId,
            relicIndex = relicIndex,
            jokerPlus = jokerPlus
        };
        
        SendMessage(message);
        LogDebug($"Relic selection sent: {relicIndex}, Joker+: {jokerPlus}");
    }
    
    /// <summary>
    /// Send card action
    /// </summary>
    public void SendCardAction(string action, object data)
    {
        if (string.IsNullOrEmpty(currentGameSessionId))
        {
            LogError("Cannot send card action: no active game session");
            return;
        }
        
        var message = new
        {
            type = "card_action",
            gameSessionId = currentGameSessionId,
            action = action,
            data = data
            // Removed timestamp - server doesn't need it
        };
        
        SendMessage(message);
        LogDebug($"Card action sent: {action}");
    }
    
    /// <summary>
    /// Send lock in message
    /// </summary>
    public void SendLockIn()
    {
        if (string.IsNullOrEmpty(currentGameSessionId))
        {
            LogError("Cannot send lock in: no active game session");
            return;
        }
        
        var message = new
        {
            type = "lock_in",
            gameSessionId = currentGameSessionId
        };
        
        SendMessage(message);
        LogDebug("Lock in sent");
    }
    
    /// <summary>
    /// Send betting action
    /// </summary>
    public void SendBettingAction(string action, long amount = 0)
    {
        if (string.IsNullOrEmpty(currentGameSessionId))
        {
            LogError("Cannot send betting action: no active game session");
            return;
        }
        
        var message = new
        {
            type = "betting_action",
            gameSessionId = currentGameSessionId,
            action = action,
            amount = amount
        };
        
        SendMessage(message);
        LogDebug($"Betting action sent: {action}, Amount: {amount}");
    }
    
    /// <summary>
    /// Send heartbeat to server
    /// </summary>
    public void SendHeartbeat()
    {
        var heartbeat = new HeartbeatMessage
        {
            type = "heartbeat"
        };
        
        SendMessage(heartbeat);
    }
    
    /// <summary>
    /// Send message to server
    /// </summary>
    private void SendMessage(object message)
    {
        if (!isConnected)
        {
            LogError("Cannot send message: not connected to server");
            return;
        }
        
        try
        {
            string jsonMessage = JsonConvert.SerializeObject(message);
            webSocket.Send(jsonMessage);
            LogDebug("Sent message: " + jsonMessage);
        }
        catch (Exception e)
        {
            LogError("Failed to send message: " + e.Message);
        }
    }
    
    /// <summary>
    /// Start heartbeat system
    /// </summary>
    private void StartHeartbeat()
    {
        StopHeartbeat();
        heartbeatCoroutine = StartCoroutine(HeartbeatRoutine());
    }
    
    /// <summary>
    /// Stop heartbeat system
    /// </summary>
    private void StopHeartbeat()
    {
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }
    }
    
    /// <summary>
    /// Heartbeat routine
    /// </summary>
    private IEnumerator HeartbeatRoutine()
    {
        while (isConnected)
        {
            yield return new WaitForSeconds(heartbeatInterval);
            
            if (isConnected)
            {
                SendHeartbeat();
            }
        }
    }
    
    /// <summary>
    /// Reconnect after delay
    /// </summary>
    private IEnumerator ReconnectAfterDelay()
    {
        yield return new WaitForSeconds(reconnectDelay);
        
        if (!isConnected)
        {
            LogDebug("Attempting to reconnect...");
            InitializeWebSocket();
        }
    }
    
    /// <summary>
    /// Connect to the matchmaking server
    /// </summary>
    public void Connect()
    {
        if (isConnected)
        {
            LogDebug("Already connected to server");
            return;
        }
        
        InitializeWebSocket();
    }
    
    /// <summary>
    /// Disconnect from server
    /// </summary>
    public void Disconnect()
    {
        if (webSocket != null)
        {
            StopHeartbeat();
            webSocket.Close();
            webSocket = null;
        }
        
        isConnected = false;
        isConnectionEstablished = false;
        isAuthenticated = false;
        sessionId = null;
        playerId = null;
        isInMatchmaking = false;
        currentTableType = null;
        currentGameSessionId = null;
        opponentAddress = null;
        playInAmount = 0;
    }
    
    // Public properties for status checking
    public bool IsConnected => isConnected;
    public bool IsConnectionEstablished => isConnectionEstablished;
    public bool IsAuthenticated => isAuthenticated;
    public bool IsInMatchmaking => isInMatchmaking;
    public string CurrentTableType => currentTableType;
    public string PlayerId => playerId;
    public string SessionId => sessionId;
    public string CurrentGameSessionId => currentGameSessionId;
    public string OpponentAddress => opponentAddress;
    public long PlayInAmount => playInAmount;
    
    /// <summary>
    /// Check if a wallet is connected
    /// </summary>
    public bool IsWalletConnected()
    {
        return Web3.Instance != null && Web3.Instance.WalletBase != null && Web3.Instance.WalletBase.Account != null;
    }
    
    /// <summary>
    /// Get the connected wallet address
    /// </summary>
    public string GetWalletAddress()
    {
        if (IsWalletConnected())
        {
            return Web3.Instance.WalletBase.Account.PublicKey.ToString();
        }
        return null;
    }
    
    /// <summary>
    /// Get detailed connection status for debugging
    /// </summary>
    public string GetConnectionStatus()
    {
        return $"Connected: {isConnected}, ConnectionEstablished: {isConnectionEstablished}, Authenticated: {isAuthenticated}, InMatchmaking: {isInMatchmaking}, Player: {playerId ?? "None"}, GameSession: {currentGameSessionId ?? "None"}";
    }
    
    /// <summary>
    /// Debug logging
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DOLERO Matchmaking] {message}");
        }
    }
    
    /// <summary>
    /// Error logging
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[DOLERO Matchmaking] {message}");
    }
}

/// <summary>
/// Table types for matchmaking (following README specification)
/// </summary>
public static class TableTypes
{
    public const string SMALL = "small";   // 2 SOL play-in, 0.015 SOL max pot
    public const string MEDIUM = "medium"; // 5 SOL play-in, 0.1 SOL max pot
    public const string BIG = "big";       // 10 SOL play-in, 1 SOL max pot
}

/// <summary>
/// Error codes from server (following README specification)
/// </summary>
public static class ErrorCodes
{
    public const string AUTH_REQUIRED = "AUTH_REQUIRED";
    public const string INVALID_ADDRESS = "INVALID_ADDRESS";
    public const string INVALID_TIMESTAMP = "INVALID_TIMESTAMP";
    public const string ALREADY_CONNECTED = "ALREADY_CONNECTED";
    public const string INVALID_TABLE_TYPE = "INVALID_TABLE_TYPE";
    public const string DELEGATION_ERROR = "DELEGATION_ERROR";
    public const string RELIC_SELECTION_ERROR = "RELIC_SELECTION_ERROR";
    public const string CARD_ACTION_ERROR = "CARD_ACTION_ERROR";
    public const string LOCK_IN_ERROR = "LOCK_IN_ERROR";
    public const string BETTING_ACTION_ERROR = "BETTING_ACTION_ERROR";
    public const string INVALID_FORMAT = "INVALID_FORMAT";
    public const string UNKNOWN_TYPE = "UNKNOWN_TYPE";
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
} 