# UNITY INTEGRATION GUIDE - DOLERO SMART CONTRACT

## Overview
This document outlines how Unity can integrate with the DOLERO smart contract implementation, providing specific function calls, data structures, and integration patterns for each game phase.

## 🏗️ SMART CONTRACT INTEGRATION ARCHITECTURE

### **Unity ↔ Web2 Delegate ↔ Smart Contract Flow:**
```
Unity Client → Web2 Delegate → Smart Contract → Solana Blockchain
Unity Client ← Web2 Delegate ← Smart Contract ← Solana Blockchain
```

## 🚀 PHASE 1: CORE GAMEPLAY INTEGRATION

### **1.1 SWAP SYSTEM INTEGRATION**

#### **Unity Implementation:**
```csharp
public class SwapSystem : MonoBehaviour
{
    [Header("Swap UI Elements")]
    public GameObject swapPanel;
    public Transform cardContainer;
    public Button confirmSwapButton;
    public Text swapCountText;
    
    private List<Card> selectedCards = new List<Card>();
    private int swapsRemaining;
    
    // Call Web2 Delegate to initiate swap
    public async void InitiateSwap(List<int> cardPositions, List<Card> newCards)
    {
        try {
            var swapData = new SwapRequest {
                gameId = currentGameId,
                playerId = playerId,
                cardPositions = cardPositions,
                newCards = newCards
            };
            
            // Call Web2 delegate
            await web2Delegate.CallSwapCards(swapData);
            
            // Update UI
            UpdateSwapCount();
            RefreshCardDisplay();
            
        } catch (Exception e) {
            Debug.LogError($"Swap failed: {e.Message}");
            ShowError("Swap failed. Please try again.");
        }
    }
    
    // Handle swap response from Web2
    public void OnSwapResponse(SwapResponse response)
    {
        if (response.success) {
            // Update local card state
            UpdateLocalCards(response.newCards);
            
            // Show success animation
            ShowSwapAnimation();
            
            // Check for Third Eye relic effects
            if (response.thirdEyeActive) {
                ShowThirdEyeNotification();
            }
        }
    }
}
```

#### **Web2 Delegate Integration:**
```csharp
public class Web2Delegate : MonoBehaviour
{
    public async Task<SwapResponse> CallSwapCards(SwapRequest request)
    {
        var response = await httpClient.PostAsync("/api/games/swap", 
            JsonUtility.ToJson(request));
        
        return JsonUtility.FromJson<SwapResponse>(response);
    }
}
```

### **1.2 CARD POSITIONING SYSTEM INTEGRATION**

#### **Unity Implementation:**
```csharp
public class CardPositioningSystem : MonoBehaviour
{
    [Header("Positioning UI")]
    public DragDropManager dragDropManager;
    public Transform positionSlots;
    public Button lockPositionsButton;
    
    private Dictionary<int, Vector3> cardPositions = new Dictionary<int, Vector3>();
    
    // Handle card drag and drop
    public void OnCardDragged(Card card, Vector3 newPosition)
    {
        cardPositions[card.id] = newPosition;
        UpdatePositionPreview();
    }
    
    // Lock card positions
    public async void LockCardPositions()
    {
        try {
            var positions = cardPositions.Select(kvp => kvp.Key).ToList();
            
            await web2Delegate.CallLockCardPositions(currentGameId, positions);
            
            // Disable drag and drop
            dragDropManager.SetInteractable(false);
            lockPositionsButton.interactable = false;
            
            // Show locked animation
            ShowLockedAnimation();
            
        } catch (Exception e) {
            Debug.LogError($"Position lock failed: {e.Message}");
        }
    }
}
```

### **1.3 PROGRESSIVE CARD REVEAL INTEGRATION**

#### **Unity Implementation:**
```csharp
public class ProgressiveRevealSystem : MonoBehaviour
{
    [Header("Reveal UI")]
    public GameObject initialRevealPanel;
    public GameObject finalRevealPanel;
    public Transform revealedCardsContainer;
    public Transform hiddenCardContainer;
    
    // Handle initial reveal (cards 3 & 2)
    public async void HandleInitialReveal(List<Card> revealedCards)
    {
        // Show revealed cards
        DisplayRevealedCards(revealedCards);
        
        // Start betting phase
        StartBettingPhase();
        
        // Show betting UI
        ShowBettingInterface();
    }
    
    // Handle final reveal (card 1)
    public async void HandleFinalReveal(Card finalCard)
    {
        // Reveal the hidden card with animation
        await RevealCardAnimation(finalCard);
        
        // Calculate and display winner
        CalculateWinner();
        
        // Show game completion
        ShowGameCompletion();
    }
    
    // Call Web2 to trigger reveal phases
    public async void TriggerInitialReveal()
    {
        await web2Delegate.CallRevealInitialCards(currentGameId);
    }
    
    public async void TriggerFinalReveal()
    {
        await web2Delegate.CallRevealFinalCard(currentGameId);
    }
}
```

### **1.4 BETTING SYSTEM INTEGRATION**

#### **Unity Implementation:**
```csharp
public class BettingSystem : MonoBehaviour
{
    [Header("Betting UI")]
    public GameObject bettingPanel;
    public Slider raiseSlider;
    public Button raiseButton;
    public Button callButton;
    public Button foldButton;
    public Button revealButton;
    public Text currentBetText;
    public Text potAmountText;
    
    private BettingState currentBettingState;
    
    // Handle betting actions
    public async void RaiseBet(float amount)
    {
        try {
            var bettingAction = new BettingAction {
                type = "RAISE",
                amount = (ulong)(amount * LAMPORTS_PER_SOL)
            };
            
            await web2Delegate.CallBettingAction(currentGameId, bettingAction);
            
            // Update UI
            UpdateBettingUI();
            
        } catch (Exception e) {
            Debug.LogError($"Raise failed: {e.Message}");
        }
    }
    
    public async void CallBet()
    {
        try {
            var bettingAction = new BettingAction { type = "CALL" };
            await web2Delegate.CallBettingAction(currentGameId, bettingAction);
            UpdateBettingUI();
        } catch (Exception e) {
            Debug.LogError($"Call failed: {e.Message}");
        }
    }
    
    public async void FoldBet()
    {
        try {
            var bettingAction = new BettingAction { type = "FOLD" };
            await web2Delegate.CallBettingAction(currentGameId, bettingAction);
            
            // Show fold animation
            ShowFoldAnimation();
            
        } catch (Exception e) {
            Debug.LogError($"Fold failed: {e.Message}");
        }
    }
    
    public async void RevealCards()
    {
        try {
            var bettingAction = new BettingAction { type = "REVEAL" };
            await web2Delegate.CallBettingAction(currentGameId, bettingAction);
            
            // Trigger final reveal
            await TriggerFinalReveal();
            
        } catch (Exception e) {
            Debug.LogError($"Reveal failed: {e.Message}");
        }
    }
    
    // Handle betting state updates
    public void OnBettingStateUpdate(BettingState newState)
    {
        currentBettingState = newState;
        UpdateBettingUI();
        
        // Handle turn changes
        if (newState.currentPlayer == playerId) {
            EnablePlayerActions();
        } else {
            DisablePlayerActions();
        }
    }
}
```

## 🎮 PHASE 2: RELIC SYSTEM INTEGRATION

### **2.1 THIRD EYE RELIC INTEGRATION**

#### **Unity Implementation:**
```csharp
public class ThirdEyeRelic : MonoBehaviour
{
    [Header("Third Eye UI")]
    public GameObject thirdEyePanel;
    public Transform opponentSwapDisplay;
    public Text swapInfoText;
    
    // Handle Third Eye activation
    public void OnThirdEyeActivated()
    {
        // Show Third Eye UI
        thirdEyePanel.SetActive(true);
        
        // Display opponent's first swap
        DisplayOpponentSwap();
        
        // Show information sharing notification
        ShowInformationSharingNotification();
    }
    
    // Handle swap information sharing
    public void OnSwapInformationShared(SwapInfo swapInfo)
    {
        // Display shared swap information
        DisplaySharedSwapInfo(swapInfo);
        
        // Show strategic hints
        ShowStrategicHints(swapInfo);
    }
}
```

### **2.2 FAST HAND RELIC INTEGRATION**

#### **Unity Implementation:**
```csharp
public class FastHandRelic : MonoBehaviour
{
    [Header("Fast Hand UI")]
    public GameObject fastHandIndicator;
    public Text timerText;
    
    // Handle Fast Hand activation
    public void OnFastHandActivated()
    {
        // Show Fast Hand indicator
        fastHandIndicator.SetActive(true);
        
        // Modify timer display
        ModifyTimerDisplay(15f); // 15 seconds instead of 30
        
        // Show speed boost animation
        ShowSpeedBoostAnimation();
    }
    
    // Handle timer modification
    public void OnTimerModified(float newDuration)
    {
        // Update timer display
        UpdateTimerDisplay(newDuration);
        
        // Show timer modification effect
        ShowTimerModificationEffect();
    }
}
```

### **2.3 PLAN B RELIC INTEGRATION**

#### **Unity Implementation:**
```csharp
public class PlanBRelic : MonoBehaviour
{
    [Header("Plan B UI")]
    public GameObject planBIndicator;
    public Text extraSwapsText;
    
    // Handle Plan B activation
    public void OnPlanBActivated()
    {
        // Show Plan B indicator
        planBIndicator.SetActive(true);
        
        // Update swap count (6 swaps total)
        UpdateSwapCount(6);
        
        // Show 2-card play mode
        EnableTwoCardPlayMode();
        
        // Show Plan B animation
        ShowPlanBAnimation();
    }
}
```

### **2.4 FAIR GAME RELIC INTEGRATION**

#### **Unity Implementation:**
```csharp
public class FairGameRelic : MonoBehaviour
{
    [Header("Fair Game UI")]
    public GameObject fairGameIndicator;
    public GameObject relicNegationEffect;
    
    // Handle Fair Game activation
    public void OnFairGameActivated()
    {
        // Show Fair Game indicator
        fairGameIndicator.SetActive(true);
        
        // Negate all other relics
        NegateAllRelics();
        
        // Show fair game effect
        ShowFairGameEffect();
        
        // Display "Fair Game" notification
        ShowFairGameNotification();
    }
}
```

## ⏰ PHASE 3: TIMER SYSTEM INTEGRATION

### **3.1 ENHANCED TIMER MANAGEMENT**

#### **Unity Implementation:**
```csharp
public class TimerSystem : MonoBehaviour
{
    [Header("Timer UI")]
    public Text timerText;
    public Image timerFill;
    public GameObject timerWarning;
    
    private float currentTime;
    private float maxTime;
    private bool timerActive;
    
    // Start relic selection timer
    public void StartRelicSelectionTimer()
    {
        StartTimer(15f, "Relic Selection");
    }
    
    // Start card playing timer
    public void StartCardPlayingTimer(float duration = 30f)
    {
        StartTimer(duration, "Card Playing");
    }
    
    // Start betting timer
    public void StartBettingTimer()
    {
        StartTimer(45f, "Betting");
    }
    
    private void StartTimer(float duration, string phase)
    {
        currentTime = duration;
        maxTime = duration;
        timerActive = true;
        
        // Show timer UI
        ShowTimerUI();
        
        // Start countdown
        StartCoroutine(TimerCountdown());
    }
    
    private IEnumerator TimerCountdown()
    {
        while (currentTime > 0 && timerActive)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();
            
            // Show warning when time is low
            if (currentTime <= 5f)
            {
                ShowTimerWarning();
            }
            
            yield return null;
        }
        
        if (currentTime <= 0)
        {
            OnTimerExpired();
        }
    }
    
    private void OnTimerExpired()
    {
        // Handle auto-lock or timeout
        HandleTimerExpiration();
        
        // Show timeout animation
        ShowTimeoutAnimation();
    }
}
```

### **3.2 AUTO-LOCK COORDINATION**

#### **Unity Implementation:**
```csharp
public class AutoLockSystem : MonoBehaviour
{
    // Handle auto-lock when timer expires
    public async void HandleAutoLock()
    {
        try {
            // Auto-play minimum cards
            await AutoPlayMinimumCards();
            
            // Lock card positions
            await web2Delegate.CallAutoLockCards(currentGameId);
            
            // Show auto-lock animation
            ShowAutoLockAnimation();
            
        } catch (Exception e) {
            Debug.LogError($"Auto-lock failed: {e.Message}");
        }
    }
    
    // Auto-play minimum cards
    private async Task AutoPlayMinimumCards()
    {
        var playerCards = GetPlayerCards();
        var cardsToPlay = playerCards.Take(2).ToList();
        
        await web2Delegate.CallAutoPlayCards(currentGameId, cardsToPlay);
    }
}
```

## 🃏 PHASE 4: JOKER SYSTEM INTEGRATION

### **4.1 JOKER SELECTION SYSTEM**

#### **Unity Implementation:**
```csharp
public class JokerSystem : MonoBehaviour
{
    [Header("Joker UI")]
    public GameObject jokerPanel;
    public Button selectJokerButton;
    public Text jokerResultText;
    
    // Handle joker selection
    public async void SelectJoker()
    {
        try {
            await web2Delegate.CallSelectJoker(currentGameId);
            
            // Show joker selection animation
            ShowJokerSelectionAnimation();
            
        } catch (Exception e) {
            Debug.LogError($"Joker selection failed: {e.Message}");
        }
    }
    
    // Handle joker result
    public void OnJokerResult(JokerResult result)
    {
        // Display joker result
        DisplayJokerResult(result);
        
        // Apply joker effects
        ApplyJokerEffects(result);
        
        // Show result animation
        ShowJokerResultAnimation(result);
    }
}
```

## 🔧 PHASE 5: POLISH & ENHANCEMENT INTEGRATION

### **5.1 ENHANCED GAME STATE MANAGEMENT**

#### **Unity Implementation:**
```csharp
public class GameStateManager : MonoBehaviour
{
    [Header("State UI")]
    public GameObject[] statePanels;
    public Text currentStateText;
    
    private GameStatus currentState;
    
    // Handle state transitions
    public void OnGameStateChanged(GameStatus newState)
    {
        var oldState = currentState;
        currentState = newState;
        
        // Handle state transition
        HandleStateTransition(oldState, newState);
        
        // Update UI
        UpdateStateUI();
        
        // Show transition animation
        ShowStateTransitionAnimation(oldState, newState);
    }
    
    private void HandleStateTransition(GameStatus oldState, GameStatus newState)
    {
        switch (newState)
        {
            case GameStatus.RelicSelection:
                ShowRelicSelectionUI();
                StartRelicSelectionTimer();
                break;
                
            case GameStatus.CardPlaying:
                ShowCardPlayingUI();
                StartCardPlayingTimer();
                break;
                
            case GameStatus.CardPositioning:
                ShowCardPositioningUI();
                EnableDragAndDrop();
                break;
                
            case GameStatus.InitialReveal:
                ShowInitialRevealUI();
                break;
                
            case GameStatus.Betting:
                ShowBettingUI();
                StartBettingTimer();
                break;
                
            case GameStatus.FinalReveal:
                ShowFinalRevealUI();
                break;
                
            case GameStatus.Completed:
                ShowGameCompletionUI();
                break;
        }
    }
}
```

### **5.2 ERROR HANDLING & RECOVERY**

#### **Unity Implementation:**
```csharp
public class ErrorHandler : MonoBehaviour
{
    [Header("Error UI")]
    public GameObject errorPanel;
    public Text errorText;
    public Button retryButton;
    
    // Handle smart contract errors
    public void OnSmartContractError(string error)
    {
        // Display error message
        ShowError(error);
        
        // Attempt to recover
        AttemptRecovery();
        
        // Show retry option
        ShowRetryOption();
    }
    
    // Handle game state recovery
    public async void RecoverGameState()
    {
        try {
            // Get current state from Web2
            var gameState = await web2Delegate.GetGameState(currentGameId);
            
            // Update local state
            UpdateLocalGameState(gameState);
            
            // Restart appropriate timers
            RestartTimers(gameState);
            
            // Show recovery success
            ShowRecoverySuccess();
            
        } catch (Exception e) {
            Debug.LogError($"Recovery failed: {e.Message}");
            ShowRecoveryFailed();
        }
    }
}
```

## 📋 INTEGRATION CHECKLIST

### **Phase 1: Core Gameplay (Critical)**
- [ ] **Swap System Integration**
  - [ ] Implement swap UI and logic
  - [ ] Add swap validation
  - [ ] Handle Third Eye relic effects
  - [ ] Test swap functionality

- [ ] **Card Positioning Integration**
  - [ ] Implement drag and drop system
  - [ ] Add position validation
  - [ ] Handle position locking
  - [ ] Test positioning system

- [ ] **Progressive Reveal Integration**
  - [ ] Implement initial reveal UI
  - [ ] Add final reveal UI
  - [ ] Handle reveal animations
  - [ ] Test progressive reveal

- [ ] **Betting System Integration**
  - [ ] Implement betting UI
  - [ ] Add betting actions (RAISE/CALL/FOLD/REVEAL)
  - [ ] Handle betting state management
  - [ ] Test betting system

### **Phase 2: Relic System (Important)**
- [ ] **Third Eye Relic**
  - [ ] Implement information sharing UI
  - [ ] Add swap display system
  - [ ] Handle strategic hints
  - [ ] Test Third Eye functionality

- [ ] **Fast Hand Relic**
  - [ ] Implement timer modification UI
  - [ ] Add speed boost animations
  - [ ] Handle timer adjustments
  - [ ] Test Fast Hand effects

- [ ] **Plan B Relic**
  - [ ] Implement 2-card play mode UI
  - [ ] Add extra swap functionality
  - [ ] Handle Plan B animations
  - [ ] Test Plan B effects

- [ ] **Fair Game Relic**
  - [ ] Implement relic negation UI
  - [ ] Add fair game effects
  - [ ] Handle relic negation
  - [ ] Test Fair Game effects

### **Phase 3: Timer System (Important)**
- [ ] **Enhanced Timer Management**
  - [ ] Implement timer UI
  - [ ] Add countdown system
  - [ ] Handle timer warnings
  - [ ] Test timer functionality

- [ ] **Auto-Lock Integration**
  - [ ] Implement auto-lock UI
  - [ ] Add auto-play functionality
  - [ ] Handle timeout animations
  - [ ] Test auto-lock system

### **Phase 4: Joker System (Nice to Have)**
- [ ] **Joker Selection**
  - [ ] Implement joker selection UI
  - [ ] Add joker result display
  - [ ] Handle joker effects
  - [ ] Test joker functionality

### **Phase 5: Polish (Nice to Have)**
- [ ] **Enhanced State Management**
  - [ ] Implement state transition UI
  - [ ] Add state validation
  - [ ] Handle state animations
  - [ ] Test state transitions

- [ ] **Error Handling**
  - [ ] Implement error recovery UI
  - [ ] Add retry functionality
  - [ ] Handle error animations
  - [ ] Test error handling

## 🧪 TESTING STRATEGY

### **Unit Tests Required:**
- Swap functionality tests
- Card positioning tests
- Betting action tests
- Progressive reveal tests
- Relic effect tests
- Timer coordination tests
- State transition tests

### **Integration Tests Required:**
- Complete game flow tests
- Multi-round game tests
- Error handling tests
- Edge case tests
- Web2-Web3 coordination tests

### **Performance Tests Required:**
- Timer accuracy tests
- State synchronization tests
- Error recovery tests
- Load testing

## 📝 DEPRECATION NOTES

### **MagicBlock VRF Removal:**
- ✅ Remove all VRF-related UI elements
- ✅ Update joker selection UI
- ✅ Remove VRF dependencies
- ✅ Test without VRF functionality

### **Timer System Migration:**
- ✅ Move timer logic to Web2 delegate
- ✅ Add timer state synchronization
- ✅ Implement auto-lock coordination
- ✅ Add timer modification for relics

## 🔄 DEPLOYMENT STRATEGY

### **Phase 1 Deployment:**
1. Deploy updated Unity client with core gameplay
2. Test with updated Web2 delegate
3. Test with updated smart contract
4. Deploy to devnet for testing

### **Phase 2 Deployment:**
1. Deploy relic system updates
2. Test relic functionality
3. Deploy to devnet for testing

### **Phase 3 Deployment:**
1. Deploy timer system updates
2. Test timer coordination
3. Deploy to devnet for testing

### **Production Deployment:**
1. Deploy all updates to production
2. Test with real users
3. Monitor for issues
4. Coordinate with Web2 and Web3 teams

## 📊 SUCCESS METRICS

### **Technical Metrics:**
- ✅ All core gameplay features working
- ✅ All relic effects functioning
- ✅ Timer system accurate
- ✅ Error handling robust
- ✅ State synchronization working

### **User Experience Metrics:**
- ✅ Smooth game flow
- ✅ Responsive UI
- ✅ Reliable betting system
- ✅ Fair gameplay
- ✅ No game-breaking bugs

### **Performance Metrics:**
- ✅ Fast UI responses
- ✅ Reliable timer accuracy
- ✅ Stable WebSocket connections
- ✅ Minimal error rates

## 🎯 NEXT STEPS

1. **Start Phase 1 Implementation** (Week 1)
2. **Coordinate with Web2-Web3 Development** (Ongoing)
3. **Implement Testing Strategy** (Parallel)
4. **Prepare for Production Deployment** (Phase 4)
5. **Monitor and Optimize** (Post-deployment)

## 📞 COORDINATION REQUIREMENTS

### **Unity Team:**
- Implement UI for all new features
- Add animations and effects
- Handle user interactions
- Coordinate with Web2 delegate

### **Web2 Team:**
- Provide API endpoints for Unity
- Handle timer management
- Coordinate information sharing
- Manage state synchronization

### **Web3 Team:**
- Ensure smart contract functions work
- Provide transaction confirmations
- Handle error responses
- Coordinate with Web2 delegate 