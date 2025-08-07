# DOLERO Unity Integration Instructions

## Overview
This document provides comprehensive instructions for integrating the DOLERO Solana systems with your existing Unity components. These systems implement the complete DOLERO development plan and coordinate with your Web2 server.

## 🎯 CRITICAL INTEGRATION POINTS

### 1. CARD DRAWING AND INSTANTIATION

#### **Function: `UpdateCardWithBlockchainData(Card card, CardData newCardData)`**
**Location**: `DoleroCardSwapSystem.cs` (Line 240)

**PURPOSE**: This function updates a Unity Card with blockchain data.

**INTEGRATION INSTRUCTIONS**:
```csharp
// CONNECT THIS TO YOUR CARD INSTANTIATION SYSTEM
// When you receive new card data from blockchain (via Web2 delegate), call:

DoleroCardSwapSystem.Instance.UpdateCardWithBlockchainData(yourCard, blockchainCardData);

// This will:
// 1. Update card.cardValue and card.cardRank with blockchain values
// 2. Update the visual representation using card.cardVisual.SetCardVisual()
// 3. Play card flip animations
```

#### **Function: `RevealCard(Card card, CardData cardData)`**
**Location**: `DoleroProgressiveRevealSystem.cs` (Line 240)

**PURPOSE**: Reveals a card with blockchain data and animations.

**INTEGRATION INSTRUCTIONS**:
```csharp
// CONNECT THIS TO YOUR CARD REVEAL SYSTEM
// When cards need to be revealed (cards 3&2, then card 1), call:

await DoleroProgressiveRevealSystem.Instance.RevealCard(yourCard, blockchainCardData);

// This will:
// 1. Update card with blockchain values
// 2. Play reveal animation (card.cardVisual.TurnCardUp())
// 3. Mark card as revealed
```

### 2. EXISTING GAME MANAGER INTEGRATION

#### **Timer System Sync**
**Location**: `DoleroTimerSystem.cs` (Line 64)

**PURPOSE**: Syncs with your existing GameManager timer.

**INTEGRATION INSTRUCTIONS**:
```csharp
// YOUR EXISTING GAMEMANAGER TIMER IS AUTOMATICALLY SYNCED
// The DoleroTimerSystem connects to:
// - gameManager.timer (your existing timer variable)
// - gameManager.timerText (your existing timer text)
// - gameManager.slider (your existing timer slider)

// When our system updates timers, it also updates your GameManager UI
// Your existing timer logic continues to work alongside the new system
```

#### **Auto-Lock Integration**
**Location**: `DoleroTimerSystem.cs` (Line 351)

**PURPOSE**: Connects to your existing StandButton functionality.

**INTEGRATION INSTRUCTIONS**:
```csharp
// WHEN TIMER EXPIRES, WE CALL YOUR EXISTING STANDBUTTON
// In HandleCardPlayingTimeout(), we call:
gameManager.StandButton(); // This triggers your existing lock-in logic

// We also auto-play minimum cards if needed before calling StandButton
```

#### **Relic Manager Integration**
**Location**: `DoleroRelicSystem.cs` (Line 61)

**PURPOSE**: Works with your existing RelicsManager.

**INTEGRATION INSTRUCTIONS**:
```csharp
// CONNECT YOUR EXISTING RELICSMANAGER
// In the Unity Inspector, assign your existing RelicsManager to:
// DoleroRelicSystem.existingRelicsManager

// When relic selection starts, we show your existing relic UI
// When relic is selected, we hide your existing relic UI
// Your existing PickButton() and SkipButton() can call our system
```

### 3. CARD SELECTION AND SWAPPING

#### **Connect Card Selection to Swap System**
**INTEGRATION INSTRUCTIONS**:
```csharp
// MODIFY YOUR EXISTING CARD SELECTION LOGIC
// In your Card.cs OnPointerUp method, add:

if (selected)
{
    // Add to swap system
    DoleroCardSwapSystem.Instance.SelectCardForSwap(this);
}
else
{
    // Remove from swap system
    DoleroCardSwapSystem.Instance.DeselectCardForSwap(this);
}
```

#### **Connect Swap Button**
**INTEGRATION INSTRUCTIONS**:
```csharp
// MODIFY YOUR EXISTING SWAP BUTTON
// In your HorizontalCardHolder.SwapButton() method, replace with:

public void SwapButton()
{
    // Use new swap system instead of old logic
    DoleroCardSwapSystem.Instance.ConfirmSwap();
}
```

### 4. CARD POSITIONING SYSTEM

#### **Function: `GetPlayerCardsInRevealOrder()`**
**Location**: `DoleroProgressiveRevealSystem.cs` (Line 324)

**PURPOSE**: Gets cards in the order they should be revealed (3→2→1).

**INTEGRATION INSTRUCTIONS**:
```csharp
// CONNECT TO YOUR CARD POSITIONING SYSTEM
// This function sorts cards by position (ParentIndex()) for reveal order
// Cards are revealed in order: position 3, position 2, position 1

// YOUR CARD POSITIONING LOGIC SHOULD:
// 1. Set card.ParentIndex() based on final position
// 2. Mark cards as played (card.isPlayed = true)
// 3. The system will handle reveal order automatically
```

### 5. HEART SYSTEM INTEGRATION

#### **Player Health Integration**
**INTEGRATION INSTRUCTIONS**:
```csharp
// YOUR EXISTING PLAYERHEALTH SYSTEM IS AUTOMATICALLY USED
// The systems call your existing PlayerHealth.TakeDamage() method:

// In betting system (fold):
gameManager.GetComponent<PlayerHealth>().TakeDamage();

// In reveal system (game loss):
gameManager.GetComponent<PlayerHealth>().TakeDamage();

// RELIC EFFECTS MODIFY HEART DAMAGE:
// High Stakes: 2x heart damage
// The Closer: Special rules for exact 21 and ties
```

### 6. VISUAL EFFECTS AND ANIMATIONS

#### **Card Visual System**
**INTEGRATION INSTRUCTIONS**:
```csharp
// YOUR EXISTING CARDVISUAL SYSTEM IS USED
// The systems call your existing CardVisual methods:

card.cardVisual.TurnCardDown();     // Hide card
card.cardVisual.TurnCardUp();       // Reveal card
card.cardVisual.SetCardVisual(value, suit); // Update sprite

// ADD THESE CONNECTIONS IN YOUR CARDVISUAL SYSTEM:
// - Swap animations (highlight selected cards)
// - Reveal animations (progressive reveals)
// - Relic effect animations (visual feedback)
```

## 🔧 SETUP INSTRUCTIONS

### 1. Unity Inspector Setup

1. **DoleroSolanaManager**:
   - Assign GameManager reference
   - Assign PlayerHealth reference
   - Set server URL to: `http://174.138.42.117`

2. **DoleroCardSwapSystem**:
   - Assign HorizontalCardHolder (player deck)
   - Create swap UI panels and assign references
   - Assign confirm swap button

3. **DoleroProgressiveRevealSystem**:
   - Assign player and opponent card holders
   - Create reveal UI panels
   - Assign GameManager reference

4. **DoleroBettingSystem**:
   - Create betting UI with raise slider, buttons
   - Assign timer UI elements
   - Assign GameManager reference

5. **DoleroRelicSystem**:
   - Assign existing RelicsManager
   - Create relic UI buttons (6 relics + joker + skip)
   - Create relic effect panels

6. **DoleroTimerSystem**:
   - Assign timer UI elements
   - Assign GameManager reference
   - Configure timer durations

7. **DoleroGameStateManager**:
   - Create phase panels for each game phase
   - Assign GameManager and PlayerHealth references
   - Create debug UI (optional)

### 2. Event Connections

#### **GameManager Events**
```csharp
// IN YOUR GAMEMANAGER, CONNECT TO DOLERO EVENTS:

void Start()
{
    // Listen for game state changes
    DoleroGameStateManager.OnGamePhaseChanged += OnPhaseChanged;
    
    // Listen for timer events
    DoleroTimerSystem.OnTimerExpired += OnTimerExpired;
    
    // Listen for relic effects
    DoleroRelicSystem.OnRelicEffectApplied += OnRelicApplied;
}

private void OnPhaseChanged(DoleroGameStateManager.GamePhase phase)
{
    // Update your existing UI based on phase
    switch (phase)
    {
        case DoleroGameStateManager.GamePhase.CardPlaying:
            // Enable your existing card playing UI
            break;
        case DoleroGameStateManager.GamePhase.Betting:
            // Show your betting interface
            break;
    }
}
```

### 3. Web2 Server Integration

The systems automatically communicate with your Web2 server at `174.138.42.117`:

- **Swap coordination**: `/api/game/swap`
- **Reveal coordination**: `/api/game/initial-reveal`, `/api/game/final-reveal`
- **Betting coordination**: `/api/game/start-betting`, `/api/game/betting-action`
- **Relic coordination**: `/api/game/start-relic-selection`, `/api/game/select-relic`
- **Timer coordination**: `/api/game/start-timer`, `/api/game/auto-lock`
- **State synchronization**: `/api/game/sync-state`

## 🎮 GAME FLOW INTEGRATION

### Complete Game Flow:
1. **Game Initialization**: `DoleroSolanaManager.InitializeGame()`
2. **Relic Selection**: `DoleroRelicSystem` → Your existing relic UI
3. **Card Playing**: Your existing card system + `DoleroCardSwapSystem`
4. **Card Positioning**: Your existing drag-and-drop + position tracking
5. **Initial Reveal**: `DoleroProgressiveRevealSystem` → Cards 3&2
6. **Betting Phase**: `DoleroBettingSystem` → RAISE/CALL/FOLD/REVEAL
7. **Final Reveal**: `DoleroProgressiveRevealSystem` → Card 1
8. **Round Resolution**: Your existing heart system + winner calculation
9. **Next Round or Game End**: `DoleroGameStateManager`

## 🎯 KEY INTEGRATION FUNCTIONS

### **For Card Drawing/Swapping**:
```csharp
// Call this when you receive card data from blockchain:
await DoleroCardSwapSystem.Instance.UpdateCardWithBlockchainData(card, cardData);
```

### **For Card Reveals**:
```csharp
// Call this for progressive reveals:
await DoleroProgressiveRevealSystem.Instance.RevealCard(card, cardData);
```

### **For Auto-Card Playing**:
```csharp
// This is called automatically on timer expire, connects to your card system:
await DoleroTimerSystem.Instance.AutoPlayMinimumCards();
```

### **For Relic Effects**:
```csharp
// Listen for relic effects and apply to your systems:
DoleroRelicSystem.OnRelicEffectApplied += (relicType, effectData) => {
    // Apply effect to your game systems
};
```

## 🔄 EXISTING CODE MODIFICATIONS

### **Minimal Changes Required**:

1. **Card.cs** - Add swap selection calls in OnPointerUp
2. **HorizontalCardHolder.cs** - Replace SwapButton() logic
3. **GameManager.cs** - Add event listeners for new systems
4. **RelicsManager.cs** - Connect to new relic system

### **No Changes Needed**:
- CardVisual.cs (used as-is)
- CardSpriteDatabase.cs (used as-is)
- PlayerHealth.cs (used as-is)
- Timer visual updates (automatically synced)

## 🚨 IMPORTANT NOTES

1. **All blockchain data comes through the Web2 server** - no direct blockchain calls in Unity
2. **Existing visual systems are reused** - minimal new UI needed
3. **Timer synchronization is automatic** - your existing timer continues working
4. **Error handling is built-in** - systems gracefully handle failures
5. **State persistence** - game state is maintained on server and blockchain

## 🎉 RESULT

After integration, you'll have:
- ✅ Complete Solana blockchain integration
- ✅ All six relic effects working
- ✅ Progressive card reveal system
- ✅ Full betting mechanics (RAISE/CALL/FOLD/REVEAL)
- ✅ Card swap system with Third Eye coordination
- ✅ Synchronized timer system
- ✅ Comprehensive state management
- ✅ Web2 server coordination
- ✅ Your existing UI and animations preserved

The systems are designed to work seamlessly with your existing code while adding the complete DOLERO blockchain functionality as specified in the development plan.
