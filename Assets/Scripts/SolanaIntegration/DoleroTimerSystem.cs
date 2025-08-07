using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

/// <summary>
/// Handles timer coordination for DOLERO game
/// Implements Phase 3 of DOLERO development plan - Timer System Implementation
/// Coordinates with Web2 delegate for synchronized timing across players
/// </summary>
public class DoleroTimerSystem : MonoBehaviour
{
    [Header("Timer Configuration")]
    [SerializeField] private float relicSelectionDuration = 15f;
    [SerializeField] private float cardPlayingDuration = 30f;
    [SerializeField] private float bettingDuration = 45f;
    [SerializeField] private float warningThreshold = 5f;
    
    [Header("UI References")]
    [SerializeField] private Slider timerSlider;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject timerWarning;
    [SerializeField] private Image timerFillImage;
    [SerializeField] private Color normalTimerColor = Color.green;
    [SerializeField] private Color warningTimerColor = Color.red;
    
    [Header("Audio References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip warningSound;
    [SerializeField] private AudioClip timeoutSound;
    
    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    
    // Timer state
    private float currentTime;
    private float maxTime;
    private bool timerActive;
    private TimerPhase currentPhase;
    private Coroutine timerCoroutine;
    private float timerModifier = 1f; // For relic effects
    private bool autoLockTriggered = false;
    
    // Events
    public static event Action<TimerPhase> OnTimerStarted;
    public static event Action<TimerPhase> OnTimerExpired;
    public static event Action<float> OnTimerUpdated;
    public static event Action OnTimerWarning;
    public static event Action<string> OnAutoLockTriggered;
    
    public static DoleroTimerSystem Instance { get; private set; }
    
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
        InitializeTimerSystem();
        SetupUI();
    }
    
    #region Initialization
    
    private void InitializeTimerSystem()
    {
        currentPhase = TimerPhase.None;
        UpdateTimerUI();
        
        // Listen for game state changes
        // Note: OnGamePhaseChanged delegate is not accessible, needs to be subscribed differently
        // Note: OnRelicEffectApplied event is not accessible
        // if (DoleroRelicSystem.Instance != null)
        // {
        //     DoleroRelicSystem.OnRelicEffectApplied += OnRelicEffectApplied;
        // }
    }
    
    private void SetupUI()
    {
        if (timerWarning != null)
        {
            timerWarning.SetActive(false);
        }
        
        if (timerFillImage != null)
        {
            timerFillImage.color = normalTimerColor;
        }
        
        // Initialize with GameManager's existing timer if available
        if (gameManager != null)
        {
            // Connect to existing timer system
            SyncWithExistingTimer();
        }
    }
    
    /// <summary>
    /// Sync with existing GameManager timer system
    /// INSTRUCTION: This integrates with your existing timer in GameManager
    /// </summary>
    private void SyncWithExistingTimer()
    {
        if (gameManager != null)
        {
            // Get current timer values from GameManager
            float existingTimer = gameManager.timer;
            bool hasEnemyChosenRelic = true; // This should come from game state
            
            if (hasEnemyChosenRelic && existingTimer > 0)
            {
                // Sync our timer with the existing one
                currentTime = existingTimer;
                maxTime = gameManager.timer; // Use the max time from GameManager
                timerActive = true;
                currentPhase = TimerPhase.CardPlaying;
                
                // Start our timer to stay in sync
                if (timerCoroutine != null)
                {
                    StopCoroutine(timerCoroutine);
                }
                timerCoroutine = StartCoroutine(TimerCoroutine());
            }
        }
    }
    
    #endregion
    
    #region Timer Management
    
    /// <summary>
    /// Start relic selection timer (15 seconds)
    /// </summary>
    public async Task StartRelicSelectionTimer()
    {
        await StartTimer(relicSelectionDuration, TimerPhase.RelicSelection);
    }
    
    /// <summary>
    /// Start card playing timer (30 seconds, modifiable by relics)
    /// </summary>
    public async Task StartCardPlayingTimer(float? customDuration = null)
    {
        float duration = customDuration ?? (cardPlayingDuration * timerModifier);
        await StartTimer(duration, TimerPhase.CardPlaying);
    }
    
    /// <summary>
    /// Start betting timer (45 seconds)
    /// </summary>
    public async Task StartBettingTimer()
    {
        await StartTimer(bettingDuration, TimerPhase.Betting);
    }
    
    /// <summary>
    /// Start a timer with specified duration and phase
    /// </summary>
    private async Task StartTimer(float duration, TimerPhase phase)
    {
        try
        {
            // Coordinate timer start with Web2 delegate
            var timerRequest = new TimerStartRequest
            {
                gameId = DoleroSolanaManager.Instance.CurrentGameId,
                playerId = DoleroSolanaManager.Instance.PlayerId,
                phase = phase,
                duration = duration
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<TimerStartResponse>("game/start-timer", timerRequest, "POST");
            
            if (response?.success == true)
            {
                // Start local timer
                currentTime = duration;
                maxTime = duration;
                timerActive = true;
                currentPhase = phase;
                autoLockTriggered = false;
                
                // Start timer coroutine
                if (timerCoroutine != null)
                {
                    StopCoroutine(timerCoroutine);
                }
                timerCoroutine = StartCoroutine(TimerCoroutine());
                
                // Update UI
                UpdateTimerUI();
                
                Debug.Log($"Timer started: {phase} for {duration} seconds");
                OnTimerStarted?.Invoke(phase);
            }
            else
            {
                Debug.LogError($"Failed to start timer: {response?.message}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Timer start failed: {e.Message}");
        }
    }
    
    /// <summary>
    /// Stop the current timer
    /// </summary>
    public void StopTimer()
    {
        timerActive = false;
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        HideTimerWarning();
        UpdateTimerUI();
        
        Debug.Log($"Timer stopped: {currentPhase}");
    }
    
    /// <summary>
    /// Modify card playing timer duration (for Fast Hand relic)
    /// </summary>
    public void ModifyCardPlayingTimer(float modifier)
    {
        timerModifier = modifier;
        
        // If card playing timer is active, adjust it
        if (timerActive && currentPhase == TimerPhase.CardPlaying)
        {
            float newDuration = cardPlayingDuration * modifier;
            float elapsed = maxTime - currentTime;
            float remaining = newDuration - elapsed;
            
            if (remaining > 0)
            {
                currentTime = remaining;
                maxTime = newDuration;
                UpdateTimerUI();
            }
        }
        
        Debug.Log($"Timer modifier applied: {modifier}x");
    }
    
    /// <summary>
    /// Reset timer modifications to default
    /// </summary>
    public void ResetTimerModifications()
    {
        timerModifier = 1f;
        Debug.Log("Timer modifications reset to default");
    }
    
    #endregion
    
    #region Timer Coroutine
    
    private IEnumerator TimerCoroutine()
    {
        while (currentTime > 0 && timerActive)
        {
            currentTime -= Time.deltaTime;
            
            // Update UI
            UpdateTimerUI();
            OnTimerUpdated?.Invoke(currentTime);
            
            // Check for warning threshold
            if (currentTime <= warningThreshold && currentTime > 0)
            {
                ShowTimerWarning();
            }
            
            // Sync with GameManager timer if in card playing phase
            if (currentPhase == TimerPhase.CardPlaying && gameManager != null)
            {
                // Update GameManager's timer to stay in sync
                gameManager.timer = currentTime;
            }
            
            yield return null;
        }
        
        if (currentTime <= 0)
        {
            // Can't use await in a coroutine, start the async task
            _ = HandleTimerExpiration();
        }
    }
    
    #endregion
    
    #region Timer Expiration Handling
    
    /// <summary>
    /// Handle timer expiration based on current phase
    /// </summary>
    private async Task HandleTimerExpiration()
    {
        timerActive = false;
        
        Debug.Log($"Timer expired: {currentPhase}");
        OnTimerExpired?.Invoke(currentPhase);
        
        // Play timeout sound
        if (audioSource != null && timeoutSound != null)
        {
            audioSource.PlayOneShot(timeoutSound);
        }
        
        // Handle phase-specific timeout actions
        switch (currentPhase)
        {
            case TimerPhase.RelicSelection:
                await HandleRelicSelectionTimeout();
                break;
                
            case TimerPhase.CardPlaying:
                await HandleCardPlayingTimeout();
                break;
                
            case TimerPhase.Betting:
                await HandleBettingTimeout();
                break;
        }
        
        UpdateTimerUI();
    }
    
    private async Task HandleRelicSelectionTimeout()
    {
        // Auto-skip relic selection
        if (DoleroRelicSystem.Instance != null)
        {
            DoleroRelicSystem.Instance.SkipRelicSelection();
        }
        
        Debug.Log("Relic selection timeout - auto-skip");
    }
    
    private async Task HandleCardPlayingTimeout()
    {
        if (autoLockTriggered) return;
        
        autoLockTriggered = true;
        
        try
        {
            // Coordinate auto-lock with Web2 delegate
            var autoLockRequest = new AutoLockRequest
            {
                gameId = DoleroSolanaManager.Instance.CurrentGameId,
                playerId = DoleroSolanaManager.Instance.PlayerId
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<AutoLockResponse>("game/auto-lock", autoLockRequest, "POST");
            
            if (response?.success == true)
            {
                // Auto-play minimum cards if needed
                await AutoPlayMinimumCards();
                
                // Trigger auto-lock in GameManager
                if (gameManager != null)
                {
                    gameManager.StandButton(); // This should trigger the existing lock-in logic
                }
                
                Debug.Log("Card playing timeout - auto-lock triggered");
                OnAutoLockTriggered?.Invoke("Card playing timeout");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Auto-lock failed: {e.Message}");
        }
    }
    
    private async Task HandleBettingTimeout()
    {
        // Auto-action based on current bet state
        if (DoleroBettingSystem.Instance != null)
        {
            if (DoleroBettingSystem.Instance.CurrentBet > 0)
            {
                // If there's a bet to match, auto-fold
                await DoleroBettingSystem.Instance.FoldBet();
            }
            else
            {
                // If no bet, auto-reveal
                await DoleroBettingSystem.Instance.RevealCards();
            }
        }
        
        Debug.Log("Betting timeout - auto-action triggered");
    }
    
    /// <summary>
    /// Auto-play minimum cards when timer expires
    /// INSTRUCTION: Connect this to your card playing system
    /// This function should auto-play the minimum required cards (cards 1 and 2)
    /// </summary>
    private async Task AutoPlayMinimumCards()
    {
        try
        {
            if (gameManager?.playerDeck != null)
            {
                var playerCards = gameManager.playerDeck.cards;
                int cardsPlayed = 0;
                
                // Count currently played cards
                foreach (var card in playerCards)
                {
                    if (card.isPlayed)
                    {
                        cardsPlayed++;
                    }
                }
                
                // Auto-play minimum required cards (2 cards minimum)
                if (cardsPlayed < 2)
                {
                    int cardsToPlay = 2 - cardsPlayed;
                    int cardIndex = 0;
                    
                    foreach (var card in playerCards)
                    {
                        if (!card.isPlayed && cardsToPlay > 0)
                        {
                            // Mark card as played
                            card.isPlayed = true;
                            
                            // Place card in position (cards 1 and 2)
                            // This should connect to your card positioning system
                            Debug.Log($"Auto-played card {cardIndex + 1}: {card.cardValue} of suit {card.cardRank}");
                            
                            cardsToPlay--;
                            cardIndex++;
                        }
                        
                        if (cardsToPlay <= 0) break;
                    }
                    
                    Debug.Log($"Auto-played {2 - cardsPlayed} cards due to timeout");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Auto-play minimum cards failed: {e.Message}");
        }
    }
    
    #endregion
    
    #region Relic Effects
    
    // Temporarily comment out until event is properly configured
    // private void OnRelicEffectApplied(DoleroRelicSystem.RelicType relicType, DoleroRelicSystem.RelicEffectData effectData)
    // {
    //     switch (relicType)
    //     {
    //         case DoleroRelicSystem.RelicType.FastHand:
    //             // Fast Hand reduces timer to 15 seconds (50% of 30)
    //             ModifyCardPlayingTimer(0.5f);
    //             Debug.Log("Fast Hand relic applied - timer reduced to 15 seconds");
    //             break;
    //             
    //         case DoleroRelicSystem.RelicType.FairGame:
    //             // Fair Game resets all modifications
    //             ResetTimerModifications();
    //             Debug.Log("Fair Game relic applied - timer modifications reset");
    //             break;
    //     }
    // }
    
    #endregion
    
    #region Game State Integration
    
    // Temporarily comment out until event delegate is properly configured
    // private void OnGamePhaseChanged(GamePhase newPhase)
    // {
    //     // Stop current timer when phase changes
    //     StopTimer();
    //     
    //     // Start appropriate timer for new phase
    //     switch (newPhase)
    //     {
    //         case GamePhase.RelicSelection:
    //             _ = StartRelicSelectionTimer();
    //             break;
    //             
    //         case GamePhase.CardPlaying:
    //             _ = StartCardPlayingTimer();
    //             break;
    //             
    //         case GamePhase.Betting:
    //             _ = StartBettingTimer();
    //             break;
    //     }
    // }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateTimerUI()
    {
        // Update timer text
        if (timerText != null)
        {
            if (timerActive)
            {
                timerText.text = $"{currentTime:F1}s";
            }
            else
            {
                timerText.text = "0.0s";
            }
        }
        
        // Update timer slider
        if (timerSlider != null)
        {
            if (maxTime > 0)
            {
                timerSlider.value = currentTime / maxTime;
            }
            else
            {
                timerSlider.value = 0;
            }
        }
        
        // Update timer color based on remaining time
        if (timerFillImage != null)
        {
            if (currentTime <= warningThreshold && currentTime > 0)
            {
                timerFillImage.color = warningTimerColor;
            }
            else
            {
                timerFillImage.color = normalTimerColor;
            }
        }
        
        // Sync with GameManager timer UI if available
        if (gameManager != null && gameManager.timerText != null)
        {
            if (currentPhase == TimerPhase.CardPlaying)
            {
                gameManager.timerText.text = $"{currentTime:F2} sec";
            }
        }
        
        // Note: gameManager.slider is private and inaccessible
        // You'll need to make it public in GameManager.cs or provide a public method to update it
        // if (gameManager != null && gameManager.slider != null)
        // {
        //     if (currentPhase == TimerPhase.CardPlaying && maxTime > 0)
        //     {
        //         gameManager.slider.value = currentTime / maxTime;
        //     }
        // }
    }
    
    private void ShowTimerWarning()
    {
        if (timerWarning != null && !timerWarning.activeInHierarchy)
        {
            timerWarning.SetActive(true);
            OnTimerWarning?.Invoke();
            
            // Play warning sound
            if (audioSource != null && warningSound != null)
            {
                audioSource.PlayOneShot(warningSound);
            }
        }
    }
    
    private void HideTimerWarning()
    {
        if (timerWarning != null)
        {
            timerWarning.SetActive(false);
        }
    }
    
    #endregion
    
    #region Public Properties
    
    public float CurrentTime => currentTime;
    public float MaxTime => maxTime;
    public bool IsTimerActive => timerActive;
    public TimerPhase CurrentPhase => currentPhase;
    public float TimerModifier => timerModifier;
    
    #endregion
    
    #region Data Structures
    
    public enum TimerPhase
    {
        None,
        RelicSelection,
        CardPlaying,
        Betting
    }
    
    [Serializable]
    public class TimerStartRequest
    {
        public string gameId;
        public string playerId;
        public TimerPhase phase;
        public float duration;
    }
    
    [Serializable]
    public class TimerStartResponse
    {
        public bool success;
        public string message;
        public float serverTime;
    }
    
    [Serializable]
    public class AutoLockRequest
    {
        public string gameId;
        public string playerId;
    }
    
    [Serializable]
    public class AutoLockResponse
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
        // Stop timer
        StopTimer();
        
        // Unsubscribe from events
        // Note: OnGamePhaseChanged delegate is not accessible
        
        // Note: OnRelicEffectApplied event is not accessible
        // if (DoleroRelicSystem.OnRelicEffectApplied != null)
        // {
        //     DoleroRelicSystem.OnRelicEffectApplied -= OnRelicEffectApplied;
        // }
    }
}
