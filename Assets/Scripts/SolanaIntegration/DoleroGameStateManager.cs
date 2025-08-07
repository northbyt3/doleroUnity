using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

// Game phase enum - moved outside class for public access
public enum GamePhase
{
    Waiting = 0,
    RelicSelection = 1,
    CardPlaying = 2,
    CardPositioning = 3,
    InitialReveal = 4,
    Betting = 5,
    FinalReveal = 6,
    Completed = 7
}

// Game status enum - moved outside class for public access  
public enum DoleroGameStatus
{
    Waiting,
    Active,
    Paused,
    Completed,
    Error
}

/// <summary>
/// Comprehensive game state management and synchronization for DOLERO
/// Implements Phase 5 of DOLERO development plan - Enhanced Game State Management
/// Coordinates all game phases and maintains state consistency
/// </summary>
public class DoleroGameStateManager : MonoBehaviour
{
    [Header("Game Configuration")]
    [SerializeField] private GameConfig gameConfig;
    
    [Header("UI References")]
    [SerializeField] private GameObject[] phasePanels = new GameObject[7]; // One for each phase
    [SerializeField] private TextMeshProUGUI currentPhaseText;
    [SerializeField] private TextMeshProUGUI gameStatusText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private GameObject disconnectionPanel;
    
    [Header("Debug UI")]
    [SerializeField] private bool showDebugUI = false;
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI debugStateText;
    
    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerHealth playerHealth;
    
    // Game state
    private GameState currentGameState;
    private GamePhase currentPhase;
    private DoleroGameStatus gameStatus;
    private bool isPaused = false;
    private bool isStateTransitioning = false;
    private float stateUpdateInterval = 1f;
    private Coroutine stateUpdateCoroutine;
    
    // State validation
    private Dictionary<GamePhase, List<GamePhase>> validTransitions;
    private DateTime lastStateUpdate;
    
    // Events
    public delegate void GamePhaseChangedHandler(GamePhase phase);
    public static event GamePhaseChangedHandler OnGamePhaseChanged;
    public static event Action<DoleroGameStatus> OnGameStatusChanged;
    public static event Action<GameState> OnGameStateUpdated;
    public static event Action<string> OnGameError;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    
    public static DoleroGameStateManager Instance { get; private set; }
    
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
        InitializeGameStateManager();
        SetupUI();
        SetupValidTransitions();
    }
    
    #region Initialization
    
    private void InitializeGameStateManager()
    {
        currentGameState = new GameState
        {
            gameId = "",
            phase = GamePhase.Waiting,
            status = DoleroGameStatus.Waiting,
            roundNumber = 0,
            players = new Dictionary<string, PlayerState>(),
            gameConfig = gameConfig,
            timestamp = DateTime.UtcNow
        };
        
        currentPhase = GamePhase.Waiting;
        gameStatus = DoleroGameStatus.Waiting;
        lastStateUpdate = DateTime.UtcNow;
        
        // Listen for system events
        DoleroSolanaManager.OnSolanaConnectionChanged += OnSolanaConnectionChanged;
        DoleroSolanaManager.OnServerConnectionChanged += OnServerConnectionChanged;
        
        UpdateStateUI();
    }
    
    private void SetupUI()
    {
        // Hide all phase panels initially
        foreach (var panel in phasePanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
        
        // Setup pause/resume buttons
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(() => _ = PauseGame());
        }
        
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() => _ = ResumeGame());
            resumeButton.gameObject.SetActive(false);
        }
        
        // Setup debug UI
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebugUI);
        }
        
        if (disconnectionPanel != null)
        {
            disconnectionPanel.SetActive(false);
        }
    }
    
    private void SetupValidTransitions()
    {
        validTransitions = new Dictionary<GamePhase, List<GamePhase>>
        {
            { GamePhase.Waiting, new List<GamePhase> { GamePhase.RelicSelection } },
            { GamePhase.RelicSelection, new List<GamePhase> { GamePhase.CardPlaying } },
            { GamePhase.CardPlaying, new List<GamePhase> { GamePhase.CardPositioning } },
            { GamePhase.CardPositioning, new List<GamePhase> { GamePhase.InitialReveal } },
            { GamePhase.InitialReveal, new List<GamePhase> { GamePhase.Betting } },
            { GamePhase.Betting, new List<GamePhase> { GamePhase.FinalReveal } },
            { GamePhase.FinalReveal, new List<GamePhase> { GamePhase.Completed, GamePhase.RelicSelection } },
            { GamePhase.Completed, new List<GamePhase> { GamePhase.RelicSelection, GamePhase.Waiting } }
        };
    }
    
    #endregion
    
    #region Game Initialization
    
    /// <summary>
    /// Initialize a new game session
    /// </summary>
    public async Task<bool> InitializeGame(string tableType, decimal betAmount)
    {
        try
        {
            // Initialize game with Solana manager
            bool gameInitialized = await DoleroSolanaManager.Instance.InitializeGame(tableType, betAmount);
            
            if (gameInitialized)
            {
                // Update game state
                currentGameState.gameId = DoleroSolanaManager.Instance.CurrentGameId;
                currentGameState.status = DoleroGameStatus.Active;
                currentGameState.roundNumber = 1;
                
                // Add players to state
                string playerId = DoleroSolanaManager.Instance.PlayerId;
                currentGameState.players[playerId] = new PlayerState
                {
                    id = playerId,
                    hearts = 3,
                    swaps = 3,
                    isConnected = true,
                    isReady = true
                };
                
                // Start state synchronization
                StartStateUpdates();
                
                // Transition to relic selection
                await TransitionToPhase(GamePhase.RelicSelection);
                
                Debug.Log($"Game initialized successfully: {currentGameState.gameId}");
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Game initialization failed: {e.Message}");
            OnGameError?.Invoke($"Game initialization failed: {e.Message}");
            return false;
        }
    }
    
    #endregion
    
    #region Phase Management
    
    /// <summary>
    /// Transition to a new game phase with validation
    /// </summary>
    public async Task<bool> TransitionToPhase(GamePhase newPhase)
    {
        if (isStateTransitioning)
        {
            Debug.LogWarning("State transition already in progress");
            return false;
        }
        
        try
        {
            // Validate transition
            if (!IsValidTransition(currentPhase, newPhase))
            {
                Debug.LogError($"Invalid transition from {currentPhase} to {newPhase}");
                OnGameError?.Invoke($"Invalid transition from {currentPhase} to {newPhase}");
                return false;
            }
            
            isStateTransitioning = true;
            
            // Coordinate transition with Web2 delegate
            var transitionRequest = new PhaseTransitionRequest
            {
                gameId = currentGameState.gameId,
                playerId = DoleroSolanaManager.Instance.PlayerId,
                fromPhase = currentPhase,
                toPhase = newPhase
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<PhaseTransitionResponse>("game/phase-transition", transitionRequest, "POST");
            
            if (response?.success == true)
            {
                // Update local state
                var oldPhase = currentPhase;
                currentPhase = newPhase;
                currentGameState.phase = newPhase;
                currentGameState.timestamp = DateTime.UtcNow;
                
                // Handle phase-specific transitions
                await HandlePhaseTransition(oldPhase, newPhase);
                
                // Update UI
                UpdateStateUI();
                
                Debug.Log($"Phase transition: {oldPhase} → {newPhase}");
                OnGamePhaseChanged?.Invoke(newPhase);
                OnGameStateUpdated?.Invoke(currentGameState);
                
                return true;
            }
            else
            {
                Debug.LogError($"Phase transition failed: {response?.message}");
                OnGameError?.Invoke(response?.message ?? "Phase transition failed");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Phase transition error: {e.Message}");
            OnGameError?.Invoke($"Phase transition error: {e.Message}");
            return false;
        }
        finally
        {
            isStateTransitioning = false;
        }
    }
    
    private async Task HandlePhaseTransition(GamePhase oldPhase, GamePhase newPhase)
    {
        // Handle exiting old phase
        await ExitPhase(oldPhase);
        
        // Handle entering new phase
        await EnterPhase(newPhase);
    }
    
    private async Task ExitPhase(GamePhase phase)
    {
        // Hide phase-specific UI
        int phaseIndex = (int)phase;
        if (phaseIndex >= 0 && phaseIndex < phasePanels.Length && phasePanels[phaseIndex] != null)
        {
            phasePanels[phaseIndex].SetActive(false);
        }
        
        // Phase-specific cleanup
        switch (phase)
        {
            case GamePhase.RelicSelection:
                // No specific cleanup needed
                break;
                
            case GamePhase.CardPlaying:
                // Ensure all timers are stopped
                if (DoleroTimerSystem.Instance != null)
                {
                    DoleroTimerSystem.Instance.StopTimer();
                }
                break;
                
            case GamePhase.Betting:
                // No specific cleanup needed
                break;
        }
    }
    
    private async Task EnterPhase(GamePhase phase)
    {
        // Show phase-specific UI
        int phaseIndex = (int)phase;
        if (phaseIndex >= 0 && phaseIndex < phasePanels.Length && phasePanels[phaseIndex] != null)
        {
            phasePanels[phaseIndex].SetActive(true);
        }
        
        // Phase-specific initialization
        switch (phase)
        {
            case GamePhase.RelicSelection:
                if (DoleroRelicSystem.Instance != null)
                {
                    await DoleroRelicSystem.Instance.StartRelicSelectionPhase();
                }
                break;
                
            case GamePhase.CardPlaying:
                // Initialize card playing phase
                InitializeCardPlayingPhase();
                break;
                
            case GamePhase.CardPositioning:
                // Enable card positioning
                EnableCardPositioning();
                break;
                
            case GamePhase.InitialReveal:
                if (DoleroProgressiveRevealSystem.Instance != null)
                {
                    await DoleroProgressiveRevealSystem.Instance.TriggerInitialReveal();
                }
                break;
                
            case GamePhase.Betting:
                if (DoleroBettingSystem.Instance != null)
                {
                    await DoleroBettingSystem.Instance.StartBettingPhase();
                }
                break;
                
            case GamePhase.FinalReveal:
                if (DoleroProgressiveRevealSystem.Instance != null)
                {
                    await DoleroProgressiveRevealSystem.Instance.TriggerFinalReveal();
                }
                break;
                
            case GamePhase.Completed:
                await HandleRoundCompletion();
                break;
        }
    }
    
    #endregion
    
    #region Phase-Specific Logic
    
    private void InitializeCardPlayingPhase()
    {
        // Setup card playing environment
        if (gameManager?.playerDeck != null)
        {
            gameManager.playerDeck.SetupDeck();
        }
        
        // Enable card interactions
        EnableCardInteractions(true);
        
        Debug.Log("Card playing phase initialized");
    }
    
    private void EnableCardPositioning()
    {
        // Enable drag and drop for card positioning
        if (gameManager?.playerDeck != null)
        {
            foreach (var card in gameManager.playerDeck.cards)
            {
                if (card.isPlayed)
                {
                    card.isDraggable = true;
                }
            }
        }
        
        Debug.Log("Card positioning enabled");
    }
    
    private void EnableCardInteractions(bool enable)
    {
        if (gameManager?.playerDeck != null)
        {
            foreach (var card in gameManager.playerDeck.cards)
            {
                card.isInteractable = enable;
                card.isDraggable = enable;
            }
        }
    }
    
    private async Task HandleRoundCompletion()
    {
        // Update round number
        currentGameState.roundNumber++;
        
        // Check game end conditions
        bool gameEnded = await CheckGameEndConditions();
        
        if (!gameEnded)
        {
            // Start new round
            await Task.Delay(2000); // Brief pause between rounds
            await TransitionToPhase(GamePhase.RelicSelection);
        }
        else
        {
            // End game
            await TransitionToGameEnd();
        }
    }
    
    #endregion
    
    #region Game End Conditions
    
    private async Task<bool> CheckGameEndConditions()
    {
        try
        {
            // Check with Web2 delegate for game end conditions
            var endCheckRequest = new GameEndCheckRequest
            {
                gameId = currentGameState.gameId,
                playerId = DoleroSolanaManager.Instance.PlayerId
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<GameEndCheckResponse>("game/check-end-conditions", endCheckRequest, "POST");
            
            if (response?.gameEnded == true)
            {
                // Update game status
                gameStatus = response.gameResult.winner == DoleroSolanaManager.Instance.PlayerId ? 
                    DoleroGameStatus.PlayerWon : DoleroGameStatus.PlayerLost;
                
                currentGameState.status = gameStatus;
                
                Debug.Log($"Game ended: {response.gameResult.reason}");
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Game end check failed: {e.Message}");
            return false;
        }
    }
    
    private async Task TransitionToGameEnd()
    {
        gameStatus = DoleroGameStatus.Completed;
        currentGameState.status = gameStatus;
        
        // Stop all systems
        StopStateUpdates();
        
        // Show appropriate end screen
        if (gameStatus == DoleroGameStatus.PlayerWon)
        {
            ShowVictoryScreen();
        }
        else
        {
            ShowDefeatScreen();
        }
        
        OnGameStatusChanged?.Invoke(gameStatus);
        Debug.Log($"Game completed with status: {gameStatus}");
    }
    
    #endregion
    
    #region State Synchronization
    
    private void StartStateUpdates()
    {
        if (stateUpdateCoroutine != null)
        {
            StopCoroutine(stateUpdateCoroutine);
        }
        
        stateUpdateCoroutine = StartCoroutine(StateUpdateCoroutine());
    }
    
    private void StopStateUpdates()
    {
        if (stateUpdateCoroutine != null)
        {
            StopCoroutine(stateUpdateCoroutine);
            stateUpdateCoroutine = null;
        }
    }
    
    private IEnumerator StateUpdateCoroutine()
    {
        while (gameStatus == DoleroGameStatus.Active || gameStatus == DoleroGameStatus.Paused)
        {
            if (!isPaused)
            {
                yield return StartCoroutine(SyncGameStateCoroutine());
            }
            
            yield return new WaitForSeconds(stateUpdateInterval);
        }
    }
    
    private IEnumerator SyncGameStateCoroutine()
    {
        // Create sync task and wait for completion
        var syncTask = SyncGameState();
        yield return new WaitUntil(() => syncTask.IsCompleted);
    }
    
    /// <summary>
    /// Synchronize game state with Web2 delegate
    /// </summary>
    public async Task SyncGameState()
    {
        try
        {
            var syncRequest = new GameStateSyncRequest
            {
                gameId = currentGameState.gameId,
                playerId = DoleroSolanaManager.Instance.PlayerId,
                localState = currentGameState
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<GameStateSyncResponse>("game/sync-state", syncRequest, "POST");
            
            if (response?.success == true)
            {
                // Update local state with server state
                if (response.serverState != null)
                {
                    ValidateAndUpdateState(response.serverState);
                }
                
                lastStateUpdate = DateTime.UtcNow;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"State sync failed: {e.Message}");
        }
    }
    
    private void ValidateAndUpdateState(GameState serverState)
    {
        // Validate state consistency
        bool stateValid = ValidateStateConsistency(serverState);
        
        if (stateValid)
        {
            // Update local state
            currentGameState = serverState;
            currentPhase = serverState.phase;
            gameStatus = serverState.status;
            
            // Update UI
            UpdateStateUI();
            
            OnGameStateUpdated?.Invoke(currentGameState);
        }
        else
        {
            Debug.LogWarning("Server state validation failed - potential desync");
            OnGameError?.Invoke("Game state synchronization error");
        }
    }
    
    private bool ValidateStateConsistency(GameState serverState)
    {
        // Basic validation checks
        if (serverState.gameId != currentGameState.gameId)
        {
            Debug.LogError("Game ID mismatch in server state");
            return false;
        }
        
        if (serverState.roundNumber < currentGameState.roundNumber)
        {
            Debug.LogError("Round number regression in server state");
            return false;
        }
        
        // Additional validation logic can be added here
        return true;
    }
    
    #endregion
    
    #region Pause/Resume
    
    /// <summary>
    /// Pause the game
    /// </summary>
    public async Task<bool> PauseGame()
    {
        if (isPaused || gameStatus != DoleroGameStatus.Active)
            return false;
            
        try
        {
            var pauseRequest = new GamePauseRequest
            {
                gameId = currentGameState.gameId,
                playerId = DoleroSolanaManager.Instance.PlayerId
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<GamePauseResponse>("game/pause", pauseRequest, "POST");
            
            if (response?.success == true)
            {
                isPaused = true;
                gameStatus = DoleroGameStatus.Paused;
                currentGameState.status = gameStatus;
                
                // Pause all timers
                if (DoleroTimerSystem.Instance != null)
                {
                    DoleroTimerSystem.Instance.StopTimer();
                }
                
                UpdateStateUI();
                OnGamePaused?.Invoke();
                
                Debug.Log("Game paused");
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Pause game failed: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Resume the game
    /// </summary>
    public async Task<bool> ResumeGame()
    {
        if (!isPaused || gameStatus != DoleroGameStatus.Paused)
            return false;
            
        try
        {
            var resumeRequest = new GameResumeRequest
            {
                gameId = currentGameState.gameId,
                playerId = DoleroSolanaManager.Instance.PlayerId
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<GameResumeResponse>("game/resume", resumeRequest, "POST");
            
            if (response?.success == true)
            {
                isPaused = false;
                gameStatus = DoleroGameStatus.Active;
                currentGameState.status = gameStatus;
                
                UpdateStateUI();
                OnGameResumed?.Invoke();
                
                Debug.Log("Game resumed");
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Resume game failed: {e.Message}");
            return false;
        }
    }
    
    #endregion
    
    #region Connection Handling
    
    private void OnSolanaConnectionChanged(bool connected)
    {
        if (!connected && gameStatus == DoleroGameStatus.Active)
        {
            ShowDisconnectionPanel("Solana connection lost");
        }
        else if (connected && disconnectionPanel != null && disconnectionPanel.activeInHierarchy)
        {
            disconnectionPanel.SetActive(false);
        }
    }
    
    private void OnServerConnectionChanged(bool connected)
    {
        if (!connected && gameStatus == DoleroGameStatus.Active)
        {
            ShowDisconnectionPanel("Server connection lost");
        }
        else if (connected && disconnectionPanel != null && disconnectionPanel.activeInHierarchy)
        {
            disconnectionPanel.SetActive(false);
        }
    }
    
    private void ShowDisconnectionPanel(string reason)
    {
        if (disconnectionPanel != null)
        {
            disconnectionPanel.SetActive(true);
            
            var reasonText = disconnectionPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (reasonText != null)
            {
                reasonText.text = $"Connection lost: {reason}";
            }
        }
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateStateUI()
    {
        // Update current phase text
        if (currentPhaseText != null)
        {
            currentPhaseText.text = $"Phase: {GetPhaseDisplayName(currentPhase)}";
        }
        
        // Update game status text
        if (gameStatusText != null)
        {
            gameStatusText.text = $"Status: {gameStatus}";
        }
        
        // Update pause/resume buttons
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(!isPaused && gameStatus == DoleroGameStatus.Active);
        }
        
        if (resumeButton != null)
        {
            resumeButton.gameObject.SetActive(isPaused);
        }
        
        // Update debug UI
        if (debugStateText != null && showDebugUI)
        {
            UpdateDebugUI();
        }
    }
    
    private void UpdateDebugUI()
    {
        if (debugStateText != null)
        {
            var debugInfo = $"Game ID: {currentGameState.gameId}\n" +
                           $"Phase: {currentPhase}\n" +
                           $"Status: {gameStatus}\n" +
                           $"Round: {currentGameState.roundNumber}\n" +
                           $"Paused: {isPaused}\n" +
                           $"Players: {currentGameState.players.Count}\n" +
                           $"Last Update: {lastStateUpdate:HH:mm:ss}";
                           
            debugStateText.text = debugInfo;
        }
    }
    
    private string GetPhaseDisplayName(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Waiting: return "Waiting for Game";
            case GamePhase.RelicSelection: return "Relic Selection";
            case GamePhase.CardPlaying: return "Card Playing";
            case GamePhase.CardPositioning: return "Card Positioning";
            case GamePhase.InitialReveal: return "Initial Reveal";
            case GamePhase.Betting: return "Betting";
            case GamePhase.FinalReveal: return "Final Reveal";
            case GamePhase.Completed: return "Round Complete";
            default: return phase.ToString();
        }
    }
    
    private void ShowVictoryScreen()
    {
        if (gameManager?.victoryScreen != null)
        {
            gameManager.victoryScreen.SetActive(true);
        }
    }
    
    private void ShowDefeatScreen()
    {
        if (gameManager?.defeatScreen != null)
        {
            gameManager.defeatScreen.SetActive(true);
        }
    }
    
    #endregion
    
    #region Validation
    
    private bool IsValidTransition(GamePhase from, GamePhase to)
    {
        if (validTransitions.ContainsKey(from))
        {
            return validTransitions[from].Contains(to);
        }
        return false;
    }
    
    #endregion
    
    #region Public Properties and Methods
    
    public GameState CurrentGameState => currentGameState;
    public GamePhase CurrentPhase => currentPhase;
            public DoleroGameStatus GameStatus => gameStatus;
    public bool IsPaused => isPaused;
    public bool IsStateTransitioning => isStateTransitioning;
    
    /// <summary>
    /// Force a state synchronization
    /// </summary>
    public async Task ForceSyncState()
    {
        await SyncGameState();
    }
    
    #endregion
    
    #region Data Structures
    
    public enum GamePhase
    {
        Waiting = 0,
        RelicSelection = 1,
        CardPlaying = 2,
        CardPositioning = 3,
        InitialReveal = 4,
        Betting = 5,
        FinalReveal = 6,
        Completed = 7
    }
    
    public enum DoleroGameStatus
    {
        Waiting,
        Active,
        Paused,
        Completed,
        PlayerWon,
        PlayerLost,
        Disconnected,
        Error
    }
    
    [Serializable]
    public class GameState
    {
        public string gameId;
        public GamePhase phase;
        public DoleroGameStatus status;
        public int roundNumber;
        public Dictionary<string, PlayerState> players;
        public GameConfig gameConfig;
        public DateTime timestamp;
    }
    
    [Serializable]
    public class PlayerState
    {
        public string id;
        public int hearts;
        public int swaps;
        public bool isConnected;
        public bool isReady;
        public List<CardData> hand;
        public List<CardData> playedCards;
    }
    
    [Serializable]
    public class CardData
    {
        public int value;
        public int suit;
        public int position;
        public bool isRevealed;
    }
    
    [Serializable]
    public class GameConfig
    {
        public string tableType;
        public decimal betAmount;
        public int maxRounds;
        public float[] phaseDurations;
    }
    
    // API Request/Response structures
    [Serializable]
    public class PhaseTransitionRequest
    {
        public string gameId;
        public string playerId;
        public GamePhase fromPhase;
        public GamePhase toPhase;
    }
    
    [Serializable]
    public class PhaseTransitionResponse
    {
        public bool success;
        public string message;
        public GameState newState;
    }
    
    [Serializable]
    public class GameStateSyncRequest
    {
        public string gameId;
        public string playerId;
        public GameState localState;
    }
    
    [Serializable]
    public class GameStateSyncResponse
    {
        public bool success;
        public string message;
        public GameState serverState;
    }
    
    [Serializable]
    public class GameEndCheckRequest
    {
        public string gameId;
        public string playerId;
    }
    
    [Serializable]
    public class GameEndCheckResponse
    {
        public bool gameEnded;
        public GameResult gameResult;
    }
    
    [Serializable]
    public class GameResult
    {
        public string winner;
        public string reason;
        public int finalScore;
    }
    
    [Serializable]
    public class GamePauseRequest
    {
        public string gameId;
        public string playerId;
    }
    
    [Serializable]
    public class GamePauseResponse
    {
        public bool success;
        public string message;
    }
    
    [Serializable]
    public class GameResumeRequest
    {
        public string gameId;
        public string playerId;
    }
    
    [Serializable]
    public class GameResumeResponse
    {
        public bool success;
        public string message;
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Stop state updates
        StopStateUpdates();
        
        // Unsubscribe from events
        DoleroSolanaManager.OnSolanaConnectionChanged -= OnSolanaConnectionChanged;
        DoleroSolanaManager.OnServerConnectionChanged -= OnServerConnectionChanged;
    }
}
