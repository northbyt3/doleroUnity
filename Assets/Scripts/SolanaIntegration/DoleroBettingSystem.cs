using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

/// <summary>
/// Handles betting system for DOLERO game
/// Implements Phase 1.4 of DOLERO development plan - Betting System Overhaul
/// Supports RAISE, CALL, FOLD, and REVEAL actions with progressive betting mechanics
/// </summary>
public class DoleroBettingSystem : MonoBehaviour
{
    [Header("Betting Configuration")]
    [SerializeField] private decimal baseBetAmount = 0.01m; // SOL
    [SerializeField] private decimal maxBetAmount = 0.15m; // Micro table limit
    [SerializeField] private float bettingTimerDuration = 45f;
    
    [Header("UI References")]
    [SerializeField] private GameObject bettingPanel;
    [SerializeField] private Slider raiseSlider;
    [SerializeField] private Button raiseButton;
    [SerializeField] private Button callButton;
    [SerializeField] private Button foldButton;
    [SerializeField] private Button revealButton;
    [SerializeField] private TextMeshProUGUI currentBetText;
    [SerializeField] private TextMeshProUGUI potAmountText;
    [SerializeField] private TextMeshProUGUI bettingStatusText;
    [SerializeField] private TextMeshProUGUI opponentActionText;
    
    [Header("Timer UI")]
    [SerializeField] private Slider bettingTimerSlider;
    [SerializeField] private TextMeshProUGUI bettingTimerText;
    
    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    
    // Betting state
    private BettingState currentBettingState;
    private decimal currentBet = 0;
    private decimal totalPot = 0;
    private bool isBettingActive = false;
    private bool isPlayerTurn = false;
    private float bettingTimeRemaining;
    private Coroutine bettingTimerCoroutine;
    
    // Events
    public static event Action<BettingAction> OnBettingActionTaken;
    public static event Action<BettingState> OnBettingStateUpdated;
    public static event Action OnBettingPhaseStarted;
    public static event Action<string> OnBettingPhaseEnded;
    public static event Action<string> OnBettingError;
    
    public static DoleroBettingSystem Instance { get; private set; }
    
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
        InitializeBettingSystem();
        SetupUI();
    }
    
    #region Initialization
    
    private void InitializeBettingSystem()
    {
        currentBettingState = new BettingState
        {
            phase = BettingPhase.Waiting,
            currentPlayer = "",
            lastAction = null,
            isComplete = false
        };
        
        UpdateBettingUI();
        
        // Listen for game state changes
        // Note: OnGamePhaseChanged delegate is not accessible, needs to be subscribed differently
        // Note: OnInitialRevealCompleted event is not accessible
        // if (DoleroProgressiveRevealSystem.Instance != null)
        // {
        //     DoleroProgressiveRevealSystem.OnInitialRevealCompleted += OnInitialRevealCompleted;
        // }
    }
    
    private void SetupUI()
    {
        if (bettingPanel != null)
        {
            bettingPanel.SetActive(false);
        }
        
        // Setup button listeners
        if (raiseButton != null)
        {
            raiseButton.onClick.AddListener(() => _ = RaiseBet());
        }
        
        if (callButton != null)
        {
            callButton.onClick.AddListener(() => _ = CallBet());
        }
        
        if (foldButton != null)
        {
            foldButton.onClick.AddListener(() => _ = FoldBet());
        }
        
        if (revealButton != null)
        {
            revealButton.onClick.AddListener(() => _ = RevealCards());
        }
        
        // Setup raise slider
        if (raiseSlider != null)
        {
            raiseSlider.minValue = (float)baseBetAmount;
            raiseSlider.maxValue = (float)maxBetAmount;
            raiseSlider.value = (float)baseBetAmount;
            raiseSlider.onValueChanged.AddListener(OnRaiseSliderChanged);
        }
    }
    
    #endregion
    
    #region Betting Phase Management
    
    /// <summary>
    /// Start the betting phase after initial card reveal
    /// </summary>
    public async Task StartBettingPhase()
    {
        try
        {
            isBettingActive = true;
            currentBettingState.phase = BettingPhase.Active;
            
            // Initialize betting state with Web2 delegate
            var bettingRequest = new StartBettingRequest
            {
                gameId = DoleroSolanaManager.Instance.CurrentGameId,
                playerId = DoleroSolanaManager.Instance.PlayerId
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<StartBettingResponse>("game/start-betting", bettingRequest, "POST");
            
            if (response?.success == true)
            {
                // Update betting state
                currentBettingState = response.bettingState;
                currentBet = response.currentBet;
                totalPot = response.totalPot;
                isPlayerTurn = response.isPlayerTurn;
                
                // Show betting UI
                ShowBettingUI();
                
                // Start betting timer
                StartBettingTimer();
                
                Debug.Log("Betting phase started successfully");
                OnBettingPhaseStarted?.Invoke();
            }
            else
            {
                OnBettingError?.Invoke(response?.message ?? "Failed to start betting phase");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start betting phase: {e.Message}");
            OnBettingError?.Invoke($"Failed to start betting phase: {e.Message}");
        }
    }
    
    /// <summary>
    /// End the betting phase and proceed to final reveal
    /// </summary>
    public async Task EndBettingPhase(string reason)
    {
        try
        {
            isBettingActive = false;
            currentBettingState.phase = BettingPhase.Complete;
            currentBettingState.isComplete = true;
            
            // Stop betting timer
            StopBettingTimer();
            
            // Hide betting UI
            HideBettingUI();
            
            // Notify Web2 delegate
            var endRequest = new EndBettingRequest
            {
                gameId = DoleroSolanaManager.Instance.CurrentGameId,
                reason = reason
            };
            
            await DoleroSolanaManager.Instance.CallWebAPI<EndBettingResponse>("game/end-betting", endRequest, "POST");
            
            Debug.Log($"Betting phase ended: {reason}");
            OnBettingPhaseEnded?.Invoke(reason);
            
            // Trigger final reveal
            if (DoleroProgressiveRevealSystem.Instance != null)
            {
                await DoleroProgressiveRevealSystem.Instance.TriggerFinalReveal();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to end betting phase: {e.Message}");
            OnBettingError?.Invoke($"Failed to end betting phase: {e.Message}");
        }
    }
    
    #endregion
    
    #region Betting Actions
    
    /// <summary>
    /// Raise the bet by the amount selected on the slider
    /// </summary>
    public async Task RaiseBet()
    {
        if (!CanPerformBettingAction())
            return;
            
        try
        {
            decimal raiseAmount = (decimal)raiseSlider.value;
            
            // Validate raise amount
            if (raiseAmount <= currentBet || raiseAmount > maxBetAmount)
            {
                OnBettingError?.Invoke($"Invalid raise amount: {raiseAmount}");
                return;
            }
            
            var bettingAction = new BettingAction
            {
                type = BettingActionType.Raise,
                amount = raiseAmount,
                playerId = DoleroSolanaManager.Instance.PlayerId,
                timestamp = DateTime.UtcNow
            };
            
            await ExecuteBettingAction(bettingAction);
        }
        catch (Exception e)
        {
            Debug.LogError($"Raise bet failed: {e.Message}");
            OnBettingError?.Invoke($"Raise bet failed: {e.Message}");
        }
    }
    
    /// <summary>
    /// Call the current bet (match opponent's raise)
    /// </summary>
    public async Task CallBet()
    {
        if (!CanPerformBettingAction())
            return;
            
        try
        {
            var bettingAction = new BettingAction
            {
                type = BettingActionType.Call,
                amount = currentBet,
                playerId = DoleroSolanaManager.Instance.PlayerId,
                timestamp = DateTime.UtcNow
            };
            
            await ExecuteBettingAction(bettingAction);
            
            // Calling ends the betting phase
            await EndBettingPhase("Call");
        }
        catch (Exception e)
        {
            Debug.LogError($"Call bet failed: {e.Message}");
            OnBettingError?.Invoke($"Call bet failed: {e.Message}");
        }
    }
    
    /// <summary>
    /// Fold and forfeit the round
    /// </summary>
    public async Task FoldBet()
    {
        if (!CanPerformBettingAction())
            return;
            
        try
        {
            var bettingAction = new BettingAction
            {
                type = BettingActionType.Fold,
                amount = 0,
                playerId = DoleroSolanaManager.Instance.PlayerId,
                timestamp = DateTime.UtcNow
            };
            
            await ExecuteBettingAction(bettingAction);
            
            // Folding ends the round immediately
            HandlePlayerFold();
        }
        catch (Exception e)
        {
            Debug.LogError($"Fold bet failed: {e.Message}");
            OnBettingError?.Invoke($"Fold bet failed: {e.Message}");
        }
    }
    
    /// <summary>
    /// Reveal cards and end betting phase
    /// </summary>
    public async Task RevealCards()
    {
        if (!CanPerformBettingAction())
            return;
            
        try
        {
            var bettingAction = new BettingAction
            {
                type = BettingActionType.Reveal,
                amount = 0,
                playerId = DoleroSolanaManager.Instance.PlayerId,
                timestamp = DateTime.UtcNow
            };
            
            await ExecuteBettingAction(bettingAction);
            
            // Check if both players want to reveal
            if (currentBettingState.bothPlayersReveal)
            {
                await EndBettingPhase("Both Reveal");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Reveal cards failed: {e.Message}");
            OnBettingError?.Invoke($"Reveal cards failed: {e.Message}");
        }
    }
    
    private async Task ExecuteBettingAction(BettingAction action)
    {
        try
        {
            // Send betting action to Web2 delegate
            var actionRequest = new BettingActionRequest
            {
                gameId = DoleroSolanaManager.Instance.CurrentGameId,
                action = action
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<BettingActionResponse>("game/betting-action", actionRequest, "POST");
            
            if (response?.success == true)
            {
                // Update betting state
                currentBettingState = response.bettingState;
                currentBet = response.currentBet;
                totalPot = response.totalPot;
                isPlayerTurn = response.isPlayerTurn;
                
                // Update UI
                UpdateBettingUI();
                
                // Show opponent's action
                if (response.opponentAction != null)
                {
                    ShowOpponentAction(response.opponentAction);
                }
                
                Debug.Log($"Betting action executed: {action.type} for {action.amount}");
                OnBettingActionTaken?.Invoke(action);
                OnBettingStateUpdated?.Invoke(currentBettingState);
            }
            else
            {
                OnBettingError?.Invoke(response?.message ?? "Betting action failed");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Execute betting action failed: {e.Message}");
            OnBettingError?.Invoke($"Execute betting action failed: {e.Message}");
        }
    }
    
    #endregion
    
    #region Betting Timer
    
    private void StartBettingTimer()
    {
        bettingTimeRemaining = bettingTimerDuration;
        
        if (bettingTimerCoroutine != null)
        {
            StopCoroutine(bettingTimerCoroutine);
        }
        
        bettingTimerCoroutine = StartCoroutine(BettingTimerCoroutine());
    }
    
    private void StopBettingTimer()
    {
        if (bettingTimerCoroutine != null)
        {
            StopCoroutine(bettingTimerCoroutine);
            bettingTimerCoroutine = null;
        }
    }
    
    private IEnumerator BettingTimerCoroutine()
    {
        while (bettingTimeRemaining > 0 && isBettingActive)
        {
            bettingTimeRemaining -= Time.deltaTime;
            UpdateTimerUI();
            yield return null;
        }
        
        if (bettingTimeRemaining <= 0 && isBettingActive)
        {
            // Timer expired - auto-fold or auto-reveal
            // Can't use await in a coroutine, start the async task
            _ = HandleBettingTimeout();
        }
    }
    
    private async Task HandleBettingTimeout()
    {
        Debug.Log("Betting timer expired");
        
        if (currentBet > 0)
        {
            // If there's a bet to match, auto-fold
            await FoldBet();
        }
        else
        {
            // If no bet, auto-reveal
            await RevealCards();
        }
    }
    
    #endregion
    
    #region Game State Integration
    
    // Temporarily comment out until event delegate is properly configured
    // private void OnGamePhaseChanged(GamePhase newPhase)
    // {
    //     if (newPhase == GamePhase.Betting)
    //     {
    //         _ = StartBettingPhase();
    //     }
    // }
    
    // Temporarily comment out until event is properly configured
    // private void OnInitialRevealCompleted(List<DoleroProgressiveRevealSystem.CardData> revealedCards)
    // {
    //     // Initial reveal is complete, betting phase can begin
    //     Debug.Log("Initial reveal completed, ready for betting phase");
    // }
    
    private void HandlePlayerFold()
    {
        // Player folded - lose 1 heart and end round
        if (gameManager != null)
        {
            var playerHealth = gameManager.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage();
            }
        }
        
        // End the round
        _ = EndBettingPhase("Fold");
        
        Debug.Log("Player folded and lost 1 heart");
    }
    
    #endregion
    
    #region UI Management
    
    private void ShowBettingUI()
    {
        if (bettingPanel != null)
        {
            bettingPanel.SetActive(true);
        }
        
        UpdateBettingUI();
    }
    
    private void HideBettingUI()
    {
        if (bettingPanel != null)
        {
            bettingPanel.SetActive(false);
        }
    }
    
    private void UpdateBettingUI()
    {
        // Update current bet display
        if (currentBetText != null)
        {
            currentBetText.text = $"Current Bet: {currentBet:F3} SOL";
        }
        
        // Update pot amount display
        if (potAmountText != null)
        {
            potAmountText.text = $"Pot: {totalPot:F3} SOL";
        }
        
        // Update betting status
        if (bettingStatusText != null)
        {
            if (isPlayerTurn)
            {
                bettingStatusText.text = "Your turn to bet";
            }
            else
            {
                bettingStatusText.text = "Waiting for opponent...";
            }
        }
        
        // Update button states
        UpdateButtonStates();
        
        // Update raise slider
        if (raiseSlider != null)
        {
            raiseSlider.minValue = Mathf.Max((float)currentBet + 0.001f, (float)baseBetAmount);
            raiseSlider.maxValue = (float)maxBetAmount;
        }
    }
    
    private void UpdateButtonStates()
    {
        bool canAct = CanPerformBettingAction();
        bool atMaxBet = currentBet >= maxBetAmount;
        
        if (raiseButton != null)
        {
            raiseButton.interactable = canAct && !atMaxBet;
        }
        
        if (callButton != null)
        {
            callButton.interactable = canAct && currentBet > 0;
        }
        
        if (foldButton != null)
        {
            foldButton.interactable = canAct;
        }
        
        if (revealButton != null)
        {
            revealButton.interactable = canAct;
        }
    }
    
    private void UpdateTimerUI()
    {
        if (bettingTimerSlider != null)
        {
            bettingTimerSlider.value = bettingTimeRemaining / bettingTimerDuration;
        }
        
        if (bettingTimerText != null)
        {
            bettingTimerText.text = $"{bettingTimeRemaining:F1}s";
        }
    }
    
    private void OnRaiseSliderChanged(float value)
    {
        // Update raise amount display
        if (currentBetText != null)
        {
            currentBetText.text = $"Raise to: {value:F3} SOL";
        }
    }
    
    private void ShowOpponentAction(BettingAction opponentAction)
    {
        if (opponentActionText != null)
        {
            string actionText = "";
            switch (opponentAction.type)
            {
                case BettingActionType.Raise:
                    actionText = $"Opponent raised to {opponentAction.amount:F3} SOL";
                    break;
                case BettingActionType.Call:
                    actionText = "Opponent called";
                    break;
                case BettingActionType.Fold:
                    actionText = "Opponent folded";
                    break;
                case BettingActionType.Reveal:
                    actionText = "Opponent wants to reveal";
                    break;
            }
            
            opponentActionText.text = actionText;
            
            // Auto-hide after a few seconds
            StartCoroutine(HideOpponentActionAfterDelay(3f));
        }
    }
    
    private IEnumerator HideOpponentActionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (opponentActionText != null)
        {
            opponentActionText.text = "";
        }
    }
    
    #endregion
    
    #region Validation
    
    private bool CanPerformBettingAction()
    {
        return isBettingActive && 
               isPlayerTurn && 
               !currentBettingState.isComplete &&
               DoleroSolanaManager.Instance.IsConnectedToSolana &&
               DoleroSolanaManager.Instance.IsConnectedToServer;
    }
    
    #endregion
    
    #region Public Properties
    
    public bool IsBettingActive => isBettingActive;
    public bool IsPlayerTurn => isPlayerTurn;
    public decimal CurrentBet => currentBet;
    public decimal TotalPot => totalPot;
    public BettingState CurrentBettingState => currentBettingState;
    
    #endregion
    
    #region Data Structures
    
    public enum BettingPhase
    {
        Waiting,
        Active,
        Complete
    }
    
    public enum BettingActionType
    {
        Raise,
        Call,
        Fold,
        Reveal
    }
    
    [Serializable]
    public class BettingState
    {
        public BettingPhase phase;
        public string currentPlayer;
        public BettingAction lastAction;
        public bool isComplete;
        public bool bothPlayersReveal;
    }
    
    [Serializable]
    public class BettingAction
    {
        public BettingActionType type;
        public decimal amount;
        public string playerId;
        public DateTime timestamp;
    }
    
    [Serializable]
    public class StartBettingRequest
    {
        public string gameId;
        public string playerId;
    }
    
    [Serializable]
    public class StartBettingResponse
    {
        public bool success;
        public string message;
        public BettingState bettingState;
        public decimal currentBet;
        public decimal totalPot;
        public bool isPlayerTurn;
    }
    
    [Serializable]
    public class BettingActionRequest
    {
        public string gameId;
        public BettingAction action;
    }
    
    [Serializable]
    public class BettingActionResponse
    {
        public bool success;
        public string message;
        public BettingState bettingState;
        public decimal currentBet;
        public decimal totalPot;
        public bool isPlayerTurn;
        public BettingAction opponentAction;
    }
    
    [Serializable]
    public class EndBettingRequest
    {
        public string gameId;
        public string reason;
    }
    
    [Serializable]
    public class EndBettingResponse
    {
        public bool success;
        public string message;
    }
    
    public enum GamePhase
    {
        RelicSelection,
        CardPlaying,
        CardPositioning,
        InitialReveal,
        Betting,
        FinalReveal,
        Completed
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Unsubscribe from events
        // Note: OnGamePhaseChanged delegate is not accessible
        
        // Note: OnInitialRevealCompleted event is not accessible
        // if (DoleroProgressiveRevealSystem.OnInitialRevealCompleted != null)
        // {
        //     DoleroProgressiveRevealSystem.OnInitialRevealCompleted -= OnInitialRevealCompleted;
        // }
    }
}
