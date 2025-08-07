using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Handles progressive card reveal system for DOLERO game
/// Implements Phase 1.3 of DOLERO development plan - Progressive Card Reveal System
/// Reveals cards 3 & 2 first, then card 1 after betting phase
/// </summary>
public class DoleroProgressiveRevealSystem : MonoBehaviour
{
    [Header("Reveal Configuration")]
    [SerializeField] private float revealAnimationDuration = 1f;
    [SerializeField] private float cardRevealDelay = 0.3f;
    
    [Header("UI References")]
    [SerializeField] private GameObject initialRevealPanel;
    [SerializeField] private GameObject finalRevealPanel;
    [SerializeField] private Transform revealedCardsContainer;
    [SerializeField] private Transform hiddenCardContainer;
    [SerializeField] private TMPro.TextMeshProUGUI revealPhaseText;
    
    [Header("Game References")]
    [SerializeField] private HorizontalCardHolder playerCardHolder;
    [SerializeField] private HorizontalCardHolder opponentCardHolder;
    [SerializeField] private GameManager gameManager;
    
    // Reveal state
    private List<Card> revealedCards = new List<Card>();
    private Card hiddenCard;
    private bool initialRevealComplete = false;
    private bool finalRevealComplete = false;
    private RevealPhase currentPhase = RevealPhase.Waiting;
    
    // Events
    public static event Action<List<CardData>> OnInitialRevealCompleted;
    public static event Action<CardData> OnFinalRevealCompleted;
    public static event Action<RevealPhase> OnRevealPhaseChanged;
    public static event Action<string> OnRevealError;
    
    public static DoleroProgressiveRevealSystem Instance { get; private set; }
    
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
        InitializeRevealSystem();
        SetupUI();
    }
    
    #region Initialization
    
    private void InitializeRevealSystem()
    {
        currentPhase = RevealPhase.Waiting;
        UpdateRevealUI();
        
        // Listen for game state changes
        // Note: OnGamePhaseChanged delegate is not accessible, needs to be subscribed differently
    }
    
    private void SetupUI()
    {
        if (initialRevealPanel != null)
        {
            initialRevealPanel.SetActive(false);
        }
        
        if (finalRevealPanel != null)
        {
            finalRevealPanel.SetActive(false);
        }
    }
    
    #endregion
    
    #region Initial Reveal (Cards 3 & 2)
    
    /// <summary>
    /// Trigger initial reveal of cards 3 & 2 for both players
    /// Called after card positioning phase is complete
    /// </summary>
    public async Task TriggerInitialReveal()
    {
        try
        {
            if (currentPhase != RevealPhase.Waiting)
            {
                OnRevealError?.Invoke("Invalid phase for initial reveal");
                return;
            }
            
            currentPhase = RevealPhase.InitialReveal;
            OnRevealPhaseChanged?.Invoke(currentPhase);
            
            // Show initial reveal UI
            if (initialRevealPanel != null)
            {
                initialRevealPanel.SetActive(true);
            }
            
            // Call Web2 delegate to coordinate initial reveal
            var revealRequest = new InitialRevealRequest
            {
                gameId = DoleroSolanaManager.Instance.CurrentGameId,
                playerId = DoleroSolanaManager.Instance.PlayerId
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<InitialRevealResponse>("game/initial-reveal", revealRequest, "POST");
            
            if (response?.success == true)
            {
                // Apply initial reveal for both players
                await ApplyInitialReveal(response);
                
                initialRevealComplete = true;
                currentPhase = RevealPhase.BettingPhase;
                OnRevealPhaseChanged?.Invoke(currentPhase);
                
                Debug.Log("Initial reveal completed successfully");
                OnInitialRevealCompleted?.Invoke(response.revealedCards);
                
                // Start betting phase
                StartBettingPhase();
            }
            else
            {
                OnRevealError?.Invoke(response?.message ?? "Initial reveal failed");
                currentPhase = RevealPhase.Waiting;
                OnRevealPhaseChanged?.Invoke(currentPhase);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Initial reveal failed: {e.Message}");
            OnRevealError?.Invoke($"Initial reveal failed: {e.Message}");
            currentPhase = RevealPhase.Waiting;
            OnRevealPhaseChanged?.Invoke(currentPhase);
        }
    }
    
    private async Task ApplyInitialReveal(InitialRevealResponse response)
    {
        // Clear previous revealed cards
        revealedCards.Clear();
        
        // Reveal cards 3 & 2 for player
        var playerCards = GetPlayerCardsInRevealOrder();
        if (playerCards.Count >= 2)
        {
            await RevealCard(playerCards[0], response.revealedCards[0]); // Card 3
            await Task.Delay((int)(cardRevealDelay * 1000));
            await RevealCard(playerCards[1], response.revealedCards[1]); // Card 2
            
            revealedCards.Add(playerCards[0]);
            revealedCards.Add(playerCards[1]);
            
            // Keep card 1 hidden
            if (playerCards.Count >= 3)
            {
                hiddenCard = playerCards[2];
                KeepCardHidden(hiddenCard);
            }
        }
        
        // Reveal cards 3 & 2 for opponent
        var opponentCards = GetOpponentCardsInRevealOrder();
        if (opponentCards.Count >= 2)
        {
            await RevealCard(opponentCards[0], response.opponentRevealedCards[0]); // Card 3
            await Task.Delay((int)(cardRevealDelay * 1000));
            await RevealCard(opponentCards[1], response.opponentRevealedCards[1]); // Card 2
            
            // Keep opponent's card 1 hidden
            if (opponentCards.Count >= 3)
            {
                KeepCardHidden(opponentCards[2]);
            }
        }
        
        UpdateRevealUI();
    }
    
    #endregion
    
    #region Final Reveal (Card 1)
    
    /// <summary>
    /// Trigger final reveal of card 1 for both players
    /// Called after betting phase is complete
    /// </summary>
    public async Task TriggerFinalReveal()
    {
        try
        {
            if (currentPhase != RevealPhase.BettingPhase)
            {
                OnRevealError?.Invoke("Invalid phase for final reveal");
                return;
            }
            
            currentPhase = RevealPhase.FinalReveal;
            OnRevealPhaseChanged?.Invoke(currentPhase);
            
            // Show final reveal UI
            if (finalRevealPanel != null)
            {
                finalRevealPanel.SetActive(true);
            }
            
            // Call Web2 delegate to coordinate final reveal
            var revealRequest = new FinalRevealRequest
            {
                gameId = DoleroSolanaManager.Instance.CurrentGameId,
                playerId = DoleroSolanaManager.Instance.PlayerId
            };
            
            var response = await DoleroSolanaManager.Instance.CallWebAPI<FinalRevealResponse>("game/final-reveal", revealRequest, "POST");
            
            if (response?.success == true)
            {
                // Apply final reveal for both players
                await ApplyFinalReveal(response);
                
                finalRevealComplete = true;
                currentPhase = RevealPhase.Complete;
                OnRevealPhaseChanged?.Invoke(currentPhase);
                
                Debug.Log("Final reveal completed successfully");
                OnFinalRevealCompleted?.Invoke(response.finalCard);
                
                // Calculate and display winner
                CalculateGameWinner(response);
            }
            else
            {
                OnRevealError?.Invoke(response?.message ?? "Final reveal failed");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Final reveal failed: {e.Message}");
            OnRevealError?.Invoke($"Final reveal failed: {e.Message}");
        }
    }
    
    private async Task ApplyFinalReveal(FinalRevealResponse response)
    {
        // Reveal player's hidden card (card 1)
        if (hiddenCard != null)
        {
            await RevealCard(hiddenCard, response.finalCard);
            revealedCards.Add(hiddenCard);
        }
        
        // Reveal opponent's hidden card (card 1)
        var opponentCards = GetOpponentCardsInRevealOrder();
        if (opponentCards.Count >= 3)
        {
            await RevealCard(opponentCards[2], response.opponentFinalCard);
        }
        
        UpdateRevealUI();
    }
    
    #endregion
    
    #region Card Reveal Mechanics
    
    /// <summary>
    /// Reveal a specific card with animation
    /// INSTRUCTION: Connect this to your card reveal animation system
    /// Call this function when you need to reveal a card from blockchain data
    /// </summary>
    public async Task RevealCard(Card card, CardData cardData)
    {
        if (card == null) return;
        
        // Update card with blockchain data
        card.cardValue = cardData.value;
        card.cardRank = cardData.suit;
        
        // Play reveal animation
        if (card.cardVisual != null)
        {
            // Turn card face up and show new values
            card.cardVisual.TurnCardUp();
            card.cardVisual.SetCardVisual(cardData.value, cardData.suit);
        }
        
        // Mark as revealed
        card.isPlayed = true;
        
        // Wait for animation to complete
        await Task.Delay((int)(revealAnimationDuration * 1000));
        
        Debug.Log($"Card revealed: {cardData.value} of suit {cardData.suit} at position {cardData.position}");
    }
    
    private void KeepCardHidden(Card card)
    {
        if (card?.cardVisual != null)
        {
            // Keep card face down
            card.cardVisual.TurnCardDown();
        }
        
        Debug.Log("Card kept hidden for suspense");
    }
    
    /// <summary>
    /// Get player cards in reveal order (position 3, 2, 1)
    /// INSTRUCTION: This function gets cards in the order they should be revealed
    /// Connect this to your card positioning system
    /// </summary>
    private List<Card> GetPlayerCardsInRevealOrder()
    {
        var cards = new List<Card>();
        
        if (playerCardHolder != null && playerCardHolder.cards != null)
        {
            // Sort cards by position (3, 2, 1 reveal order)
            var sortedCards = new List<Card>(playerCardHolder.cards);
            sortedCards.Sort((a, b) => b.ParentIndex().CompareTo(a.ParentIndex()));
            
            // Only include played cards
            foreach (var card in sortedCards)
            {
                if (card.isPlayed)
                {
                    cards.Add(card);
                }
            }
        }
        
        return cards;
    }
    
    private List<Card> GetOpponentCardsInRevealOrder()
    {
        var cards = new List<Card>();
        
        if (opponentCardHolder != null && opponentCardHolder.cards != null)
        {
            // Sort cards by position (3, 2, 1 reveal order)
            var sortedCards = new List<Card>(opponentCardHolder.cards);
            sortedCards.Sort((a, b) => b.ParentIndex().CompareTo(a.ParentIndex()));
            
            // Only include played cards
            foreach (var card in sortedCards)
            {
                if (card.isPlayed)
                {
                    cards.Add(card);
                }
            }
        }
        
        return cards;
    }
    
    #endregion
    
    #region Game Flow Integration
    
    // Temporarily comment out until event delegate is properly configured
    // private void OnGamePhaseChanged(GamePhase newPhase)
    // {
    //     switch (newPhase)
    //     {
    //         case GamePhase.CardPositioning:
    //             // Reset reveal state for new round
    //             ResetRevealState();
    //             break;
    //             
    //         case GamePhase.InitialReveal:
    //             // Trigger initial reveal automatically
    //             StartCoroutine(TriggerInitialRevealCoroutine());
    //             break;
    //             
    //         case GamePhase.FinalReveal:
    //             // Trigger final reveal automatically
    //             StartCoroutine(TriggerFinalRevealCoroutine());
    //             break;
    //     }
    // }
    
    private IEnumerator TriggerInitialRevealCoroutine()
    {
        yield return new WaitForSeconds(0.5f); // Small delay for UI transitions
        _ = TriggerInitialReveal();
    }
    
    private IEnumerator TriggerFinalRevealCoroutine()
    {
        yield return new WaitForSeconds(0.5f); // Small delay for UI transitions
        _ = TriggerFinalReveal();
    }
    
    private void StartBettingPhase()
    {
        // Signal to start betting phase
        if (DoleroGameStateManager.Instance != null)
        {
            // Transition to betting phase
            // Note: GamePhase enum may not be accessible directly
            // _ = DoleroGameStateManager.Instance.TransitionToPhase(GamePhase.Betting);
        }
    }
    
    private void CalculateGameWinner(FinalRevealResponse response)
    {
        // Calculate final scores and determine winner
        var playerScore = CalculatePlayerScore();
        var opponentScore = response.opponentScore;
        
        Debug.Log($"Final scores - Player: {playerScore}, Opponent: {opponentScore}");
        
        // Determine winner and apply heart changes
        if (playerScore > 21 && opponentScore > 21)
        {
            // Both bust - tie
            HandleGameTie();
        }
        else if (playerScore > 21)
        {
            // Player bust - opponent wins
            HandleGameLoss();
        }
        else if (opponentScore > 21)
        {
            // Opponent bust - player wins
            HandleGameWin();
        }
        else
        {
            // Compare scores
            var playerDistance = Math.Abs(21 - playerScore);
            var opponentDistance = Math.Abs(21 - opponentScore);
            
            if (playerDistance < opponentDistance)
            {
                HandleGameWin();
            }
            else if (opponentDistance < playerDistance)
            {
                HandleGameLoss();
            }
            else
            {
                HandleGameTie();
            }
        }
    }
    
    private int CalculatePlayerScore()
    {
        int score = 0;
        foreach (var card in revealedCards)
        {
            if (hiddenCard == card) continue; // Don't double-count hidden card
            score += card.cardValue;
        }
        
        if (hiddenCard != null)
        {
            score += hiddenCard.cardValue;
        }
        
        return score;
    }
    
    private void HandleGameWin()
    {
        Debug.Log("Player wins the round!");
        // Opponent loses heart
        // Apply relic effects if any
    }
    
    private void HandleGameLoss()
    {
        Debug.Log("Player loses the round!");
        // Player loses heart
        if (gameManager != null && gameManager.GetComponent<PlayerHealth>() != null)
        {
            gameManager.GetComponent<PlayerHealth>().TakeDamage();
        }
    }
    
    private void HandleGameTie()
    {
        Debug.Log("Round is a tie!");
        // Both players lose heart (unless modified by relics)
        if (gameManager != null && gameManager.GetComponent<PlayerHealth>() != null)
        {
            gameManager.GetComponent<PlayerHealth>().TakeDamage();
        }
    }
    
    #endregion
    
    #region State Management
    
    private void ResetRevealState()
    {
        revealedCards.Clear();
        hiddenCard = null;
        initialRevealComplete = false;
        finalRevealComplete = false;
        currentPhase = RevealPhase.Waiting;
        
        // Hide UI panels
        if (initialRevealPanel != null)
        {
            initialRevealPanel.SetActive(false);
        }
        
        if (finalRevealPanel != null)
        {
            finalRevealPanel.SetActive(false);
        }
        
        UpdateRevealUI();
    }
    
    private void UpdateRevealUI()
    {
        if (revealPhaseText != null)
        {
            switch (currentPhase)
            {
                case RevealPhase.Waiting:
                    revealPhaseText.text = "Waiting for card positioning...";
                    break;
                case RevealPhase.InitialReveal:
                    revealPhaseText.text = "Revealing cards 3 & 2...";
                    break;
                case RevealPhase.BettingPhase:
                    revealPhaseText.text = "Cards revealed - betting phase";
                    break;
                case RevealPhase.FinalReveal:
                    revealPhaseText.text = "Revealing final card...";
                    break;
                case RevealPhase.Complete:
                    revealPhaseText.text = "All cards revealed";
                    break;
            }
        }
    }
    
    #endregion
    
    #region Public Properties
    
    public bool InitialRevealComplete => initialRevealComplete;
    public bool FinalRevealComplete => finalRevealComplete;
    public RevealPhase CurrentPhase => currentPhase;
    public List<Card> RevealedCards => new List<Card>(revealedCards);
    public Card HiddenCard => hiddenCard;
    
    #endregion
    
    #region Data Structures
    
    public enum RevealPhase
    {
        Waiting,
        InitialReveal,
        BettingPhase,
        FinalReveal,
        Complete
    }
    
    [Serializable]
    public class InitialRevealRequest
    {
        public string gameId;
        public string playerId;
    }
    
    [Serializable]
    public class InitialRevealResponse
    {
        public bool success;
        public string message;
        public List<CardData> revealedCards; // Cards 3 & 2 for player
        public List<CardData> opponentRevealedCards; // Cards 3 & 2 for opponent
    }
    
    [Serializable]
    public class FinalRevealRequest
    {
        public string gameId;
        public string playerId;
    }
    
    [Serializable]
    public class FinalRevealResponse
    {
        public bool success;
        public string message;
        public CardData finalCard; // Card 1 for player
        public CardData opponentFinalCard; // Card 1 for opponent
        public int opponentScore;
    }
    
    [Serializable]
    public class CardData
    {
        public int value;
        public int suit;
        public int position;
        public bool isRevealed;
    }
    

    
    #endregion
    
    void OnDestroy()
    {
        // Unsubscribe from events
        // Note: OnGamePhaseChanged delegate is not accessible
    }
}
