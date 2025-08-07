using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Handles card swap system for DOLERO game
/// Implements Phase 1.1 of DOLERO development plan - Swap System Implementation
/// Coordinates with Web2 delegate and handles Third Eye relic interactions
/// </summary>
public class DoleroCardSwapSystem : MonoBehaviour
{
    [Header("Swap Configuration")]
    [SerializeField] private int baseSwapCount = 3;
    [SerializeField] private float swapAnimationDuration = 0.5f;
    
    [Header("UI References")]
    [SerializeField] private GameObject swapPanel;
    [SerializeField] private UnityEngine.UI.Button confirmSwapButton;
    [SerializeField] private TMPro.TextMeshProUGUI swapCountText;
    [SerializeField] private GameObject thirdEyeNotificationPanel;
    
    [Header("Game References")]
    [SerializeField] private HorizontalCardHolder playerCardHolder;
    [SerializeField] private GameManager gameManager;
    
    // Swap state
    private List<Card> selectedCardsForSwap = new List<Card>();
    private int swapsRemaining;
    private bool isSwapping = false;
    private bool thirdEyeActive = false;
    private bool isFirstSwap = true;
    
    // Events
    public static event Action<int> OnSwapsChanged;
    public static event Action<List<CardData>> OnSwapCompleted;
    public static event Action<SwapData> OnThirdEyeSwapRevealed;
    public static event Action<string> OnSwapError;
    
    public static DoleroCardSwapSystem Instance { get; private set; }
    
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
        InitializeSwapSystem();
        SetupUI();
    }
    
    #region Initialization
    
    private void InitializeSwapSystem()
    {
        swapsRemaining = baseSwapCount;
        UpdateSwapUI();
        
        // Listen for relic effects that modify swap count
        // Note: OnRelicEffectApplied delegate is not accessible, needs to be subscribed differently
    }
    
    private void SetupUI()
    {
        if (confirmSwapButton != null)
        {
            confirmSwapButton.onClick.AddListener(ConfirmSwap);
        }
        
        if (swapPanel != null)
        {
            swapPanel.SetActive(false);
        }
        
        if (thirdEyeNotificationPanel != null)
        {
            thirdEyeNotificationPanel.SetActive(false);
        }
    }
    
    #endregion
    
    #region Card Selection for Swaps
    
    /// <summary>
    /// Add card to swap selection
    /// </summary>
    public void SelectCardForSwap(Card card)
    {
        if (isSwapping || swapsRemaining <= 0)
            return;
            
        if (!selectedCardsForSwap.Contains(card))
        {
            selectedCardsForSwap.Add(card);
            card.selected = true;
            
            // Visual feedback
            HighlightCardForSwap(card, true);
            
            Debug.Log($"Card selected for swap: {card.cardValue} of suit {card.cardRank}");
        }
    }
    
    /// <summary>
    /// Remove card from swap selection
    /// </summary>
    public void DeselectCardForSwap(Card card)
    {
        if (selectedCardsForSwap.Contains(card))
        {
            selectedCardsForSwap.Remove(card);
            card.selected = false;
            
            // Visual feedback
            HighlightCardForSwap(card, false);
            
            Debug.Log($"Card deselected for swap: {card.cardValue} of suit {card.cardRank}");
        }
    }
    
    /// <summary>
    /// Clear all selected cards
    /// </summary>
    public void ClearSwapSelection()
    {
        foreach (var card in selectedCardsForSwap)
        {
            card.selected = false;
            HighlightCardForSwap(card, false);
        }
        selectedCardsForSwap.Clear();
    }
    
    private void HighlightCardForSwap(Card card, bool highlight)
    {
        // Apply visual highlighting to show card is selected for swap
        // This should connect to your existing card selection visual system
        if (card.cardVisual != null)
        {
            // You would implement visual highlighting here
            // For example: change card border color, add glow effect, etc.
            Debug.Log($"Highlighting card {card.cardValue} for swap: {highlight}");
        }
    }
    
    #endregion
    
    #region Swap Execution
    
    /// <summary>
    /// Confirm and execute selected card swaps
    /// Coordinates with Web2 delegate and smart contract
    /// </summary>
    public async void ConfirmSwap()
    {
        if (isSwapping || selectedCardsForSwap.Count == 0 || swapsRemaining <= 0)
            return;
            
        isSwapping = true;
        
        try
        {
            // Prepare swap data
            var swapData = PrepareSwapData();
            
            // Call Web2 delegate to handle swap
            var response = await DoleroSolanaManager.Instance.CallWebAPI<SwapResponse>("game/swap", swapData, "POST");
            
            if (response?.success == true)
            {
                // Apply swap results
                await ApplySwapResults(response);
                
                // Handle Third Eye relic if active
                if (thirdEyeActive && isFirstSwap)
                {
                    await HandleThirdEyeReveal(swapData);
                }
                
                // Update state
                swapsRemaining--;
                isFirstSwap = false;
                UpdateSwapUI();
                
                // Clear selection
                ClearSwapSelection();
                
                Debug.Log($"Swap completed successfully. Swaps remaining: {swapsRemaining}");
                OnSwapCompleted?.Invoke(response.newCards);
            }
            else
            {
                OnSwapError?.Invoke(response?.message ?? "Swap failed");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Swap execution failed: {e.Message}");
            OnSwapError?.Invoke($"Swap execution failed: {e.Message}");
        }
        finally
        {
            isSwapping = false;
        }
    }
    
    private SwapData PrepareSwapData()
    {
        var cardPositions = new List<int>();
        var cardIds = new List<string>();
        
        foreach (var card in selectedCardsForSwap)
        {
            cardPositions.Add(card.ParentIndex());
            cardIds.Add($"{card.cardValue}_{card.cardRank}");
        }
        
        return new SwapData
        {
            gameId = DoleroSolanaManager.Instance.CurrentGameId,
            playerId = DoleroSolanaManager.Instance.PlayerId,
            cardPositions = cardPositions,
            cardIds = cardIds,
            isFirstSwap = isFirstSwap,
            thirdEyeActive = thirdEyeActive
        };
    }
    
    private async Task ApplySwapResults(SwapResponse response)
    {
        // Update cards with new values from blockchain
        for (int i = 0; i < response.newCards.Count; i++)
        {
            var newCardData = response.newCards[i];
            var position = response.swapPositions[i];
            
            // Find the card at this position and update it
            var cardToUpdate = GetCardAtPosition(position);
            if (cardToUpdate != null)
            {
                await UpdateCardWithBlockchainData(cardToUpdate, newCardData);
            }
        }
        
        // Play swap animation
        PlaySwapAnimation();
    }
    
    private Card GetCardAtPosition(int position)
    {
        // Get card at specific position in the player's hand
        if (position >= 0 && position < playerCardHolder.cards.Count)
        {
            return playerCardHolder.cards[position];
        }
        return null;
    }
    
    /// <summary>
    /// Update card with new data from blockchain
    /// INSTRUCTION: Connect this to your card instantiation system
    /// Call this function when you receive new card data from the blockchain
    /// </summary>
    public async Task UpdateCardWithBlockchainData(Card card, CardData newCardData)
    {
        // Update card properties with blockchain data
        card.cardValue = newCardData.value;
        card.cardRank = newCardData.suit;
        
        // Update visual representation
        if (card.cardVisual != null)
        {
            card.cardVisual.SetCardVisual(newCardData.value, newCardData.suit);
        }
        
        // Play card flip animation to show new card
        if (card.cardVisual != null)
        {
            card.cardVisual.TurnCardDown();
            await Task.Delay(250); // Wait for flip animation
            card.cardVisual.TurnCardUp();
        }
        
        Debug.Log($"Card updated: {newCardData.value} of suit {newCardData.suit}");
    }
    
    private void PlaySwapAnimation()
    {
        // Play swap animation for selected cards
        foreach (var card in selectedCardsForSwap)
        {
            if (card.cardVisual != null)
            {
                // Use existing swap animation if available
                card.cardVisual.TurnCardDown();
            }
        }
    }
    
    #endregion
    
    #region Third Eye Relic Integration
    
    /// <summary>
    /// Handle Third Eye relic revealing first swap information
    /// </summary>
    private async Task HandleThirdEyeReveal(SwapData swapData)
    {
        try
        {
            // Call Web2 delegate to share swap information
            var thirdEyeData = new ThirdEyeRevealData
            {
                gameId = swapData.gameId,
                playerId = swapData.playerId,
                swapData = swapData
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<ThirdEyeResponse>("game/third-eye-reveal", thirdEyeData, "POST");
            
            if (response?.success == true)
            {
                // Show Third Eye notification to both players
                ShowThirdEyeNotification(response.sharedInfo);
                OnThirdEyeSwapRevealed?.Invoke(swapData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Third Eye reveal failed: {e.Message}");
        }
    }
    
    private void ShowThirdEyeNotification(string sharedInfo)
    {
        if (thirdEyeNotificationPanel != null)
        {
            thirdEyeNotificationPanel.SetActive(true);
            
            // Display shared swap information
            var notificationText = thirdEyeNotificationPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (notificationText != null)
            {
                notificationText.text = $"Third Eye Revealed: {sharedInfo}";
            }
            
            // Auto-hide after a few seconds
            StartCoroutine(HideThirdEyeNotificationAfterDelay(3f));
        }
    }
    
    private IEnumerator HideThirdEyeNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (thirdEyeNotificationPanel != null)
        {
            thirdEyeNotificationPanel.SetActive(false);
        }
    }
    
    #endregion
    
    #region Relic Effects
    
    private void OnRelicEffectApplied(DoleroRelicSystem.RelicType relicType, DoleroRelicSystem.RelicEffectData effectData)
    {
        switch (relicType)
        {
            case DoleroRelicSystem.RelicType.ThirdEye:
                thirdEyeActive = true;
                Debug.Log("Third Eye relic activated - first swap will be revealed");
                break;
                
            case DoleroRelicSystem.RelicType.FastHand:
                // Fast Hand gives +1 swap (handled in relic system)
                swapsRemaining += effectData.extraSwaps;
                UpdateSwapUI();
                Debug.Log($"Fast Hand relic activated - gained {effectData.extraSwaps} extra swap(s)");
                break;
                
            case DoleroRelicSystem.RelicType.PlanB:
                // Plan B gives 6 total swaps
                swapsRemaining = 6;
                UpdateSwapUI();
                Debug.Log("Plan B relic activated - gained extra swaps for 2-card play");
                break;
                
            case DoleroRelicSystem.RelicType.FairGame:
                // Fair Game negates all other relic effects
                swapsRemaining = baseSwapCount;
                thirdEyeActive = false;
                UpdateSwapUI();
                Debug.Log("Fair Game relic activated - all relic effects negated");
                break;
        }
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateSwapUI()
    {
        if (swapCountText != null)
        {
            swapCountText.text = $"Swaps: {swapsRemaining}";
        }
        
        if (confirmSwapButton != null)
        {
            confirmSwapButton.interactable = swapsRemaining > 0 && selectedCardsForSwap.Count > 0 && !isSwapping;
        }
        
        OnSwapsChanged?.Invoke(swapsRemaining);
    }
    
    #endregion
    
    #region Public Properties and Methods
    
    public int SwapsRemaining => swapsRemaining;
    public bool IsSwapping => isSwapping;
    public bool ThirdEyeActive => thirdEyeActive;
    public List<Card> SelectedCards => new List<Card>(selectedCardsForSwap);
    
    /// <summary>
    /// Set swap count (used by relic system)
    /// </summary>
    public void SetSwapCount(int newCount)
    {
        swapsRemaining = newCount;
        UpdateSwapUI();
    }
    
    /// <summary>
    /// Add swaps (used by relic system)
    /// </summary>
    public void AddSwaps(int additionalSwaps)
    {
        swapsRemaining += additionalSwaps;
        UpdateSwapUI();
    }
    
    #endregion
    
    #region Data Structures
    
    [Serializable]
    public class SwapData
    {
        public string gameId;
        public string playerId;
        public List<int> cardPositions;
        public List<string> cardIds;
        public bool isFirstSwap;
        public bool thirdEyeActive;
    }
    
    [Serializable]
    public class SwapResponse
    {
        public bool success;
        public string message;
        public List<CardData> newCards;
        public List<int> swapPositions;
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
    public class ThirdEyeRevealData
    {
        public string gameId;
        public string playerId;
        public SwapData swapData;
    }
    
    [Serializable]
    public class ThirdEyeResponse
    {
        public bool success;
        public string sharedInfo;
        public string message;
    }
    
    [Serializable]
    public class RelicEffectData
    {
        public int extraSwaps;
        public float timerModifier;
        public bool negateOtherRelics;
    }
    
    public enum RelicType
    {
        HighStakes,
        PlanB,
        ThirdEye,
        FairGame,
        TheCloser,
        FastHand
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Unsubscribe from events
        // Note: OnRelicEffectApplied delegate is not accessible
    }
}
