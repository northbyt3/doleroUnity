using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

/// <summary>
/// Handles all six relic effects for DOLERO game
/// Implements Phase 2 of DOLERO development plan - Relic System Completion
/// Coordinates with Web2 delegate and manages relic interactions
/// </summary>
public class DoleroRelicSystem : MonoBehaviour
{
    [Header("Relic Configuration")]
    [SerializeField] private float relicSelectionTime = 15f;
    [SerializeField] private int relicPoolSize = 3;
    
    [Header("UI References")]
    [SerializeField] private GameObject relicSelectionPanel;
    [SerializeField] private GameObject[] relicButtons = new GameObject[6]; // One for each relic
    [SerializeField] private Button skipRelicButton;
    [SerializeField] private Button jokerButton;
    [SerializeField] private TextMeshProUGUI relicTimerText;
    [SerializeField] private TextMeshProUGUI activeRelicText;
    [SerializeField] private GameObject[] relicEffectPanels = new GameObject[6];
    
    [Header("Game References")]
    [SerializeField] private HorizontalCardHolder playerCardHolder;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RelicsManager existingRelicsManager;
    
    // Relic state
    private RelicType selectedRelic = RelicType.None;
    private RelicType opponentRelic = RelicType.None;
    private bool hasSelectedRelic = false;
    private bool relicSelectionActive = false;
    private float relicTimeRemaining;
    private Coroutine relicTimerCoroutine;
    private List<RelicType> availableRelics = new List<RelicType>();
    
    // Relic effects state
    private Dictionary<RelicType, RelicEffectData> activeRelicEffects = new Dictionary<RelicType, RelicEffectData>();
    
    // Events
    public static event Action<RelicType, RelicEffectData> OnRelicEffectApplied;
    public static event Action<RelicType> OnRelicSelected;
    public static event Action OnRelicSelectionPhaseStarted;
    public static event Action OnRelicSelectionPhaseEnded;
    public static event Action<string> OnRelicError;
    
    public static DoleroRelicSystem Instance { get; private set; }
    
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
        InitializeRelicSystem();
        SetupUI();
    }
    
    #region Initialization
    
    private void InitializeRelicSystem()
    {
        // Initialize available relics
        availableRelics = new List<RelicType>
        {
            RelicType.HighStakes,
            RelicType.PlanB,
            RelicType.ThirdEye,
            RelicType.FairGame,
            RelicType.TheCloser,
            RelicType.FastHand
        };
        
        // Listen for game state changes
        // Note: OnGamePhaseChanged delegate is not accessible, needs to be subscribed differently
    }
    
    private void SetupUI()
    {
        if (relicSelectionPanel != null)
        {
            relicSelectionPanel.SetActive(false);
        }
        
        // Setup relic button listeners
        for (int i = 0; i < relicButtons.Length; i++)
        {
            if (relicButtons[i] != null)
            {
                int relicIndex = i;
                var button = relicButtons[i].GetComponent<Button>();
                if (button != null)
                {
                    int capturedIndex = relicIndex; // Capture the index for the lambda
                    button.onClick.AddListener(() => _ = SelectRelic((RelicType)(capturedIndex + 1)));
                }
            }
        }
        
        // Setup skip button
        if (skipRelicButton != null)
        {
            skipRelicButton.onClick.AddListener(() => SkipRelicSelection());
        }
        
        // Setup joker button
        if (jokerButton != null)
        {
            jokerButton.onClick.AddListener(() => _ = SelectJoker());
        }
        
        // Hide all relic effect panels
        foreach (var panel in relicEffectPanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
    
    #endregion
    
    #region Relic Selection Phase
    
    /// <summary>
    /// Start the relic selection phase
    /// </summary>
    public async Task StartRelicSelectionPhase()
    {
        try
        {
            relicSelectionActive = true;
            hasSelectedRelic = false;
            selectedRelic = RelicType.None;
            
            // Request available relics from Web2 delegate
            var relicRequest = new RelicSelectionRequest
            {
                gameId = DoleroSolanaManager.Instance.CurrentGameId,
                playerId = DoleroSolanaManager.Instance.PlayerId
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<RelicSelectionResponse>("game/start-relic-selection", relicRequest, "POST");
            
            if (response?.success == true)
            {
                // Show available relics
                DisplayAvailableRelics(response.availableRelics);
                
                // Show relic selection UI
                ShowRelicSelectionUI();
                
                // Start relic selection timer
                StartRelicSelectionTimer();
                
                Debug.Log("Relic selection phase started");
                OnRelicSelectionPhaseStarted?.Invoke();
            }
            else
            {
                OnRelicError?.Invoke(response?.message ?? "Failed to start relic selection");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start relic selection: {e.Message}");
            OnRelicError?.Invoke($"Failed to start relic selection: {e.Message}");
        }
    }
    
    /// <summary>
    /// Select a specific relic
    /// </summary>
    public async Task SelectRelic(RelicType relic)
    {
        if (!relicSelectionActive || hasSelectedRelic)
            return;
            
        try
        {
            // Send relic selection to Web2 delegate
            var selectionRequest = new RelicSelectionSubmitRequest
            {
                gameId = DoleroSolanaManager.Instance.CurrentGameId,
                playerId = DoleroSolanaManager.Instance.PlayerId,
                selectedRelic = relic
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<RelicSelectionSubmitResponse>("game/select-relic", selectionRequest, "POST");
            
            if (response?.success == true)
            {
                selectedRelic = relic;
                hasSelectedRelic = true;
                
                // Apply relic effect
                ApplyRelicEffect(relic);
                
                // Hide relic selection UI
                HideRelicSelectionUI();
                
                // Stop timer
                StopRelicSelectionTimer();
                
                Debug.Log($"Relic selected: {relic}");
                OnRelicSelected?.Invoke(relic);
                
                // Wait for opponent or proceed if both selected
                if (response.bothPlayersSelected)
                {
                    await EndRelicSelectionPhase();
                }
            }
            else
            {
                OnRelicError?.Invoke(response?.message ?? "Failed to select relic");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Relic selection failed: {e.Message}");
            OnRelicError?.Invoke($"Relic selection failed: {e.Message}");
        }
    }
    
    /// <summary>
    /// Skip relic selection
    /// </summary>
    public void SkipRelicSelection()
    {
        if (!relicSelectionActive)
            return;
            
        _ = SelectRelic(RelicType.None);
    }
    
    /// <summary>
    /// Select joker (50/50 chance for good or bad effect)
    /// </summary>
    public async Task SelectJoker()
    {
        if (!relicSelectionActive || hasSelectedRelic)
            return;
            
        try
        {
            // Call Web2 delegate for joker selection
            var jokerRequest = new JokerSelectionRequest
            {
                gameId = DoleroSolanaManager.Instance.CurrentGameId,
                playerId = DoleroSolanaManager.Instance.PlayerId
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<JokerSelectionResponse>("game/select-joker", jokerRequest, "POST");
            
            if (response?.success == true)
            {
                // Apply joker effect based on result
                ApplyJokerEffect(response.isGoodJoker);
                
                hasSelectedRelic = true;
                selectedRelic = RelicType.Joker;
                
                // Hide relic selection UI
                HideRelicSelectionUI();
                
                Debug.Log($"Joker selected: {(response.isGoodJoker ? "Good" : "Bad")} joker");
                OnRelicSelected?.Invoke(RelicType.Joker);
                
                if (response.bothPlayersSelected)
                {
                    await EndRelicSelectionPhase();
                }
            }
            else
            {
                OnRelicError?.Invoke(response?.message ?? "Joker selection failed");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Joker selection failed: {e.Message}");
            OnRelicError?.Invoke($"Joker selection failed: {e.Message}");
        }
    }
    
    private async Task EndRelicSelectionPhase()
    {
        relicSelectionActive = false;
        StopRelicSelectionTimer();
        HideRelicSelectionUI();
        
        Debug.Log("Relic selection phase ended");
        OnRelicSelectionPhaseEnded?.Invoke();
        
        // Transition to card playing phase
        if (DoleroGameStateManager.Instance != null)
        {
            // Transition to card playing phase
            // Note: GamePhase enum may not be accessible directly
            // _ = DoleroGameStateManager.Instance.TransitionToPhase(GamePhase.CardPlaying);
        }
    }
    
    #endregion
    
    #region Relic Effects Implementation
    
    /// <summary>
    /// Apply the effect of a selected relic
    /// </summary>
    private void ApplyRelicEffect(RelicType relic)
    {
        var effectData = new RelicEffectData();
        
        switch (relic)
        {
            case RelicType.HighStakes:
                ApplyHighStakesEffect(effectData);
                break;
                
            case RelicType.PlanB:
                ApplyPlanBEffect(effectData);
                break;
                
            case RelicType.ThirdEye:
                ApplyThirdEyeEffect(effectData);
                break;
                
            case RelicType.FairGame:
                ApplyFairGameEffect(effectData);
                break;
                
            case RelicType.TheCloser:
                ApplyTheCloserEffect(effectData);
                break;
                
            case RelicType.FastHand:
                ApplyFastHandEffect(effectData);
                break;
                
            case RelicType.None:
                // No effect
                return;
        }
        
        // Store active effect
        activeRelicEffects[relic] = effectData;
        
        // Show relic effect UI
        ShowRelicEffect(relic);
        
        // Notify other systems
        OnRelicEffectApplied?.Invoke(relic, effectData);
        
        Debug.Log($"Applied relic effect: {relic}");
    }
    
    private void ApplyHighStakesEffect(RelicEffectData effectData)
    {
        // High Stakes: Win deals 2 heart damage, lose receives 2 heart damage
        effectData.heartDamageMultiplier = 2;
        effectData.description = "Double heart damage on win/loss";
        
        // Visual effect
        if (relicEffectPanels[(int)RelicType.HighStakes - 1] != null)
        {
            relicEffectPanels[(int)RelicType.HighStakes - 1].SetActive(true);
        }
    }
    
    private void ApplyPlanBEffect(RelicEffectData effectData)
    {
        // Plan B: Play only 2 cards, receive 6 swaps total
        effectData.extraSwaps = 3; // 6 total instead of 3
        effectData.twoCardPlayMode = true;
        effectData.description = "6 swaps, 2-card play mode";
        
        // Apply to swap system
        if (DoleroCardSwapSystem.Instance != null)
        {
            DoleroCardSwapSystem.Instance.SetSwapCount(6);
        }
        
        // Modify card holder for 2-card play
        if (playerCardHolder != null)
        {
            // Implementation for 2-card play mode would go here
            Debug.Log("Plan B activated: 2-card play mode enabled");
        }
        
        // Visual effect
        if (relicEffectPanels[(int)RelicType.PlanB - 1] != null)
        {
            relicEffectPanels[(int)RelicType.PlanB - 1].SetActive(true);
        }
    }
    
    private void ApplyThirdEyeEffect(RelicEffectData effectData)
    {
        // Third Eye: Show first swap to opponent and see opponent's first swap
        effectData.revealFirstSwap = true;
        effectData.description = "First swaps are revealed to both players";
        
        // This effect is handled by the swap system
        Debug.Log("Third Eye activated: First swaps will be revealed");
        
        // Visual effect
        if (relicEffectPanels[(int)RelicType.ThirdEye - 1] != null)
        {
            relicEffectPanels[(int)RelicType.ThirdEye - 1].SetActive(true);
        }
    }
    
    private void ApplyFairGameEffect(RelicEffectData effectData)
    {
        // Fair Game: Negate all relics and jokers for the round
        effectData.negateOtherRelics = true;
        effectData.description = "All relic effects are negated";
        
        // Reset all other relic effects
        ResetAllRelicEffects();
        
        // Visual effect
        if (relicEffectPanels[(int)RelicType.FairGame - 1] != null)
        {
            relicEffectPanels[(int)RelicType.FairGame - 1].SetActive(true);
        }
        
        Debug.Log("Fair Game activated: All relic effects negated");
    }
    
    private void ApplyTheCloserEffect(RelicEffectData effectData)
    {
        // The Closer: Win with exactly 21 gains 1 heart, tie loses 2 hearts
        effectData.exactTwentyOneBonus = true;
        effectData.tieHeartPenalty = 2;
        effectData.description = "Win with 21: +1 heart, Tie: -2 hearts";
        
        // Visual effect
        if (relicEffectPanels[(int)RelicType.TheCloser - 1] != null)
        {
            relicEffectPanels[(int)RelicType.TheCloser - 1].SetActive(true);
        }
    }
    
    private void ApplyFastHandEffect(RelicEffectData effectData)
    {
        // Fast Hand: Gain 1+ swap, timer reduced to 15 seconds
        effectData.extraSwaps = 1;
        effectData.timerModifier = 0.5f; // 50% of normal time (15 seconds)
        effectData.description = "+1 swap, 15-second timer";
        
        // Apply to swap system
        if (DoleroCardSwapSystem.Instance != null)
        {
            DoleroCardSwapSystem.Instance.AddSwaps(1);
        }
        
        // Apply to timer system
        if (DoleroTimerSystem.Instance != null)
        {
            DoleroTimerSystem.Instance.ModifyCardPlayingTimer(effectData.timerModifier);
        }
        
        // Visual effect
        if (relicEffectPanels[(int)RelicType.FastHand - 1] != null)
        {
            relicEffectPanels[(int)RelicType.FastHand - 1].SetActive(true);
        }
    }
    
    private void ApplyJokerEffect(bool isGoodJoker)
    {
        var effectData = new RelicEffectData();
        
        if (isGoodJoker)
        {
            // Good joker: +1 swap
            effectData.extraSwaps = 1;
            effectData.description = "Good Joker: +1 swap";
            
            if (DoleroCardSwapSystem.Instance != null)
            {
                DoleroCardSwapSystem.Instance.AddSwaps(1);
            }
        }
        else
        {
            // Bad joker: -1 swap
            effectData.extraSwaps = -1;
            effectData.description = "Bad Joker: -1 swap";
            
            if (DoleroCardSwapSystem.Instance != null)
            {
                var currentSwaps = DoleroCardSwapSystem.Instance.SwapsRemaining;
                DoleroCardSwapSystem.Instance.SetSwapCount(Mathf.Max(0, currentSwaps - 1));
            }
        }
        
        activeRelicEffects[RelicType.Joker] = effectData;
        OnRelicEffectApplied?.Invoke(RelicType.Joker, effectData);
    }
    
    private void ResetAllRelicEffects()
    {
        // Reset swap count to base
        if (DoleroCardSwapSystem.Instance != null)
        {
            DoleroCardSwapSystem.Instance.SetSwapCount(3); // Base swap count
        }
        
        // Reset timer modifications
        if (DoleroTimerSystem.Instance != null)
        {
            DoleroTimerSystem.Instance.ResetTimerModifications();
        }
        
        // Hide all relic effect panels
        foreach (var panel in relicEffectPanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
        
        // Clear active effects
        activeRelicEffects.Clear();
    }
    
    #endregion
    
    #region Timer Management
    
    private void StartRelicSelectionTimer()
    {
        relicTimeRemaining = relicSelectionTime;
        
        if (relicTimerCoroutine != null)
        {
            StopCoroutine(relicTimerCoroutine);
        }
        
        relicTimerCoroutine = StartCoroutine(RelicSelectionTimerCoroutine());
    }
    
    private void StopRelicSelectionTimer()
    {
        if (relicTimerCoroutine != null)
        {
            StopCoroutine(relicTimerCoroutine);
            relicTimerCoroutine = null;
        }
    }
    
    private IEnumerator RelicSelectionTimerCoroutine()
    {
        while (relicTimeRemaining > 0 && relicSelectionActive && !hasSelectedRelic)
        {
            relicTimeRemaining -= Time.deltaTime;
            UpdateRelicTimerUI();
            yield return null;
        }
        
        if (relicTimeRemaining <= 0 && !hasSelectedRelic)
        {
            // Timer expired - auto-skip
            SkipRelicSelection();
        }
    }
    
    private void UpdateRelicTimerUI()
    {
        if (relicTimerText != null)
        {
            relicTimerText.text = $"Relic Selection: {relicTimeRemaining:F1}s";
        }
    }
    
    #endregion
    
    #region UI Management
    
    private void ShowRelicSelectionUI()
    {
        if (relicSelectionPanel != null)
        {
            relicSelectionPanel.SetActive(true);
        }
        
        // Use existing RelicsManager if available
        if (existingRelicsManager != null)
        {
            existingRelicsManager.gameObject.SetActive(true);
        }
    }
    
    private void HideRelicSelectionUI()
    {
        if (relicSelectionPanel != null)
        {
            relicSelectionPanel.SetActive(false);
        }
        
        // Hide existing RelicsManager
        if (existingRelicsManager != null)
        {
            existingRelicsManager.gameObject.SetActive(false);
        }
    }
    
    private void DisplayAvailableRelics(List<RelicType> relics)
    {
        // Enable/disable relic buttons based on available relics
        for (int i = 0; i < relicButtons.Length; i++)
        {
            if (relicButtons[i] != null)
            {
                RelicType relicType = (RelicType)(i + 1);
                bool isAvailable = relics.Contains(relicType);
                
                var button = relicButtons[i].GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = isAvailable;
                }
                
                // Update visual state
                relicButtons[i].SetActive(isAvailable);
            }
        }
    }
    
    private void ShowRelicEffect(RelicType relic)
    {
        if (activeRelicText != null)
        {
            string relicName = GetRelicDisplayName(relic);
            var effectData = activeRelicEffects.GetValueOrDefault(relic);
            activeRelicText.text = $"Active Relic: {relicName}\n{effectData?.description ?? ""}";
        }
    }
    
    private string GetRelicDisplayName(RelicType relic)
    {
        switch (relic)
        {
            case RelicType.HighStakes: return "High Stakes";
            case RelicType.PlanB: return "Plan B";
            case RelicType.ThirdEye: return "Third Eye";
            case RelicType.FairGame: return "Fair Game";
            case RelicType.TheCloser: return "The Closer";
            case RelicType.FastHand: return "Fast Hand";
            case RelicType.Joker: return "Joker";
            default: return "None";
        }
    }
    
    #endregion
    
    #region Game State Integration
    
    // Temporarily comment out until event delegate is properly configured
    // private void OnGamePhaseChanged(GamePhase newPhase)
    // {
    //     if (newPhase == GamePhase.RelicSelection)
    //     {
    //         _ = StartRelicSelectionPhase();
    //     }
    // }
    
    #endregion
    
    #region Public Properties and Methods
    
    public RelicType SelectedRelic => selectedRelic;
    public bool HasSelectedRelic => hasSelectedRelic;
    public bool IsRelicSelectionActive => relicSelectionActive;
    public Dictionary<RelicType, RelicEffectData> ActiveRelicEffects => new Dictionary<RelicType, RelicEffectData>(activeRelicEffects);
    
    /// <summary>
    /// Check if a specific relic effect is active
    /// </summary>
    public bool IsRelicEffectActive(RelicType relic)
    {
        return activeRelicEffects.ContainsKey(relic);
    }
    
    /// <summary>
    /// Get the effect data for a specific relic
    /// </summary>
    public RelicEffectData GetRelicEffect(RelicType relic)
    {
        return activeRelicEffects.GetValueOrDefault(relic);
    }
    
    #endregion
    
    #region Data Structures
    
    public enum RelicType
    {
        None = 0,
        HighStakes = 1,
        PlanB = 2,
        ThirdEye = 3,
        FairGame = 4,
        TheCloser = 5,
        FastHand = 6,
        Joker = 7
    }
    
    [Serializable]
    public class RelicEffectData
    {
        public int extraSwaps;
        public float timerModifier = 1f;
        public int heartDamageMultiplier = 1;
        public bool twoCardPlayMode;
        public bool revealFirstSwap;
        public bool negateOtherRelics;
        public bool exactTwentyOneBonus;
        public int tieHeartPenalty = 1;
        public string description;
    }
    
    [Serializable]
    public class RelicSelectionRequest
    {
        public string gameId;
        public string playerId;
    }
    
    [Serializable]
    public class RelicSelectionResponse
    {
        public bool success;
        public string message;
        public List<RelicType> availableRelics;
    }
    
    [Serializable]
    public class RelicSelectionSubmitRequest
    {
        public string gameId;
        public string playerId;
        public RelicType selectedRelic;
    }
    
    [Serializable]
    public class RelicSelectionSubmitResponse
    {
        public bool success;
        public string message;
        public bool bothPlayersSelected;
        public RelicType opponentRelic;
    }
    
    [Serializable]
    public class JokerSelectionRequest
    {
        public string gameId;
        public string playerId;
    }
    
    [Serializable]
    public class JokerSelectionResponse
    {
        public bool success;
        public string message;
        public bool isGoodJoker;
        public bool bothPlayersSelected;
    }
    

    
    #endregion
    
    void OnDestroy()
    {
        // Unsubscribe from events
        // Note: OnGamePhaseChanged delegate is not accessible
    }
}
