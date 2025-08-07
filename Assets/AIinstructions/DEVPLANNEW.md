# DOLERO - NEW DEVELOPMENT PLAN

## Overview
This document outlines the **updated development plan** for the DOLERO project, incorporating all missing features, deprecations, and coordination requirements between Web2 and Web3 components.

## 🎯 PROJECT STATUS & DEPRECATIONS

### **DEPRECATED FEATURES (COMPLETELY REMOVED)**
- ❌ **MagicBlock VRF Integration**: Completely removed from both Web2 and Web3
- ❌ **VRF Dependencies**: All VRF-related code and dependencies removed
- ❌ **Timer Smart Contract Logic**: Moved entirely to Web2 delegate responsibility

### **CURRENT IMPLEMENTATION STATUS**
- ✅ **Basic Smart Contract**: Core game structure implemented
- ✅ **Multi-Token Support**: SOL and BONK token support working
- ✅ **Basic Game Flow**: Relic selection, card drawing, basic betting
- ✅ **Emergency Controls**: Pause/resume functionality
- ❌ **Advanced Gameplay**: Missing betting loop, progressive reveal, swap system
- ❌ **Relic System**: Incomplete (only 2/6 relics implemented)
- ❌ **Timer System**: Web2 responsibility (not yet implemented)

## 🚀 PHASE 1: CORE GAMEPLAY IMPLEMENTATION (CRITICAL)

### **1.1 SWAP SYSTEM IMPLEMENTATION**
**Priority**: CRITICAL
**Timeline**: Week 1-2

#### **Web3 Smart Contract Changes:**
```rust
// Add swap functionality
pub fn swap_cards(ctx: Context<SwapCards>, card_positions: Vec<u8>, new_cards: Vec<Card>) -> Result<()>

// Add swap tracking for Third Eye relic
pub struct Swap {
    pub player: Pubkey,
    pub original_cards: Vec<Card>,
    pub new_cards: Vec<Card>,
    pub swap_count: u8,
    pub is_first_swap: bool,
}
```

#### **Web2 Delegate Changes:**
```javascript
// Track swaps for Third Eye relic
class SwapTracker {
  trackSwap(gameId, playerId, swapData) { /* ... */ }
  shareSwapInformation(gameId, playerId, swapData) { /* ... */ }
}
```

### **1.2 CARD POSITIONING SYSTEM**
**Priority**: CRITICAL
**Timeline**: Week 2-3

#### **Web3 Smart Contract Changes:**
```rust
// Add card positioning
pub struct Card {
    pub value: u8,
    pub suit: u8,
    pub position: u8, // 1, 2, or 3
    pub is_revealed: bool,
}

// Add card rearrangement
pub fn rearrange_cards(ctx: Context<RearrangeCards>, new_positions: Vec<u8>) -> Result<()>
```

#### **Web2 Delegate Changes:**
```javascript
// Coordinate card positioning
class CardPositionCoordinator {
  handleCardRearrangement(gameId, playerId, newPositions) { /* ... */ }
  validateCardPositions(positions) { /* ... */ }
}
```

### **1.3 PROGRESSIVE CARD REVEAL SYSTEM**
**Priority**: CRITICAL
**Timeline**: Week 3-4

#### **Web3 Smart Contract Changes:**
```rust
// Split reveal into two phases
pub fn reveal_initial_cards(ctx: Context<RevealInitialCards>) -> Result<()> // Cards 3 & 2
pub fn reveal_final_card(ctx: Context<RevealFinalCard>) -> Result<()> // Card 1

// Update game state
pub struct Game {
    // ... existing fields ...
    pub revealed_cards: Vec<Card>, // Cards 3 & 2
    pub hidden_card: Option<Card>, // Card 1
    pub betting_complete: bool,
}
```

#### **Web2 Delegate Changes:**
```javascript
// Coordinate progressive reveal
class ProgressiveRevealCoordinator {
  coordinateInitialReveal(gameId) { /* ... */ }
  coordinateFinalReveal(gameId) { /* ... */ }
  notifyPlayersOfReveal(gameId, revealType, cards) { /* ... */ }
}
```

### **1.4 BETTING SYSTEM OVERHAUL**
**Priority**: CRITICAL
**Timeline**: Week 4-5

#### **Web3 Smart Contract Changes:**
```rust
// Add betting actions
pub enum BettingAction {
    Raise { amount: u64 },
    Call,
    Fold,
    Reveal,
}

// Replace place_bet with betting_action
pub fn betting_action(ctx: Context<BettingAction>, action: BettingAction) -> Result<()>

// Add betting state management
pub struct BettingState {
    pub phase: BettingPhase,
    pub current_player: Pubkey,
    pub last_action: Option<BettingAction>,
    pub is_complete: bool,
}
```

#### **Web2 Delegate Changes:**
```javascript
// Coordinate betting actions
class BettingCoordinator {
  handleBettingAction(gameId, playerId, action) { /* ... */ }
  validateBettingAction(game, playerId, action) { /* ... */ }
  notifyOpponentOfAction(gameId, playerId, action) { /* ... */ }
}
```

## 🎮 PHASE 2: RELIC SYSTEM COMPLETION (IMPORTANT)

### **2.1 THIRD EYE RELIC IMPLEMENTATION**
**Priority**: IMPORTANT
**Timeline**: Week 5-6

#### **Web3 Smart Contract Changes:**
```rust
// Add Third Eye functionality
pub fn handle_third_eye_reveal(ctx: Context<ThirdEyeReveal>) -> Result<()>

// Add information sharing
pub struct SharedInformation {
    pub player1_first_swap: Option<Swap>,
    pub player2_first_swap: Option<Swap>,
    pub third_eye_active: bool,
}
```

#### **Web2 Delegate Changes:**
```javascript
// Coordinate Third Eye information sharing
class ThirdEyeCoordinator {
  handleThirdEyeActivation(gameId, playerId) { /* ... */ }
  shareSwapInformation(gameId, playerId, swapData) { /* ... */ }
  notifyPlayersOfSharedInfo(gameId, sharedData) { /* ... */ }
}
```

### **2.2 FAST HAND RELIC IMPLEMENTATION**
**Priority**: IMPORTANT
**Timeline**: Week 6-7

#### **Web3 Smart Contract Changes:**
```rust
// Add Fast Hand relic effect
RelicType::FastHand => {
    player.swaps = player.swaps.saturating_add(1);
    // Timer reduction handled by Web2
}
```

#### **Web2 Delegate Changes:**
```javascript
// Handle Fast Hand timer modification
class FastHandTimerManager {
  modifyTimerForFastHand(gameId, playerId) { /* ... */ }
  updateCardPlayingTimer(gameId, newDuration) { /* ... */ }
}
```

### **2.3 PLAN B RELIC IMPLEMENTATION**
**Priority**: IMPORTANT
**Timeline**: Week 7-8

#### **Web3 Smart Contract Changes:**
```rust
// Add Plan B relic effect
RelicType::PlanB => {
    player.plan_b_active = true;
    player.swaps = 6; // Extra swaps
    // 2-card play mode
}
```

#### **Web2 Delegate Changes:**
```javascript
// Coordinate Plan B functionality
class PlanBCoordinator {
  handlePlanBActivation(gameId, playerId) { /* ... */ }
  validatePlanBPlayMode(gameId, playerId) { /* ... */ }
}
```

### **2.4 FAIR GAME RELIC IMPLEMENTATION**
**Priority**: IMPORTANT
**Timeline**: Week 8-9

#### **Web3 Smart Contract Changes:**
```rust
// Add Fair Game relic effect
RelicType::FairGame => {
    game.fair_game_active = true;
    // Negate all other relics for the round
}
```

#### **Web2 Delegate Changes:**
```javascript
// Coordinate Fair Game functionality
class FairGameCoordinator {
  handleFairGameActivation(gameId, playerId) { /* ... */ }
  negateOtherRelics(gameId) { /* ... */ }
}
```

## ⏰ PHASE 3: TIMER SYSTEM IMPLEMENTATION (IMPORTANT)

### **3.1 ENHANCED TIMER MANAGEMENT**
**Priority**: IMPORTANT
**Timeline**: Week 9-10

#### **Web2 Delegate Implementation:**
```javascript
class EnhancedTimerManager {
  // Relic Selection Timer (15 seconds)
  startRelicSelectionTimer(gameId, duration = 15000) { /* ... */ }
  
  // Card Playing Timer (30 seconds, modifiable by relics)
  startCardPlayingTimer(gameId, duration = 30000, fastHandActive = false) { /* ... */ }
  
  // Betting Timer (45 seconds)
  startBettingTimer(gameId, duration = 45000) { /* ... */ }
  
  // Auto-lock functionality
  handleCardPlayingTimeout(gameId) { /* ... */ }
}
```

### **3.2 AUTO-LOCK COORDINATION**
**Priority**: IMPORTANT
**Timeline**: Week 10-11

#### **Web2 Delegate Implementation:**
```javascript
class AutoLockCoordinator {
  handleCardPlayingTimeout(gameId) { /* ... */ }
  autoPlayMinimumCards(gameId, playerId) { /* ... */ }
  callSmartContractAutoLock(gameId) { /* ... */ }
}
```

## 🃏 PHASE 4: JOKER SYSTEM IMPLEMENTATION (NICE TO HAVE)

### **4.1 JOKER SELECTION SYSTEM**
**Priority**: NICE TO HAVE
**Timeline**: Week 11-12

#### **Web3 Smart Contract Changes:**
```rust
// Add joker selection (simple 50/50, no VRF)
pub fn select_joker(ctx: Context<SelectJoker>) -> Result<()> {
    let joker_result = if random_bool() {
        JokerResult::Good
    } else {
        JokerResult::Bad
    };
    
    match joker_result {
        JokerResult::Good => player.swaps = player.swaps.saturating_add(1),
        JokerResult::Bad => player.swaps = player.swaps.saturating_sub(1),
    }
    
    player.joker_result = Some(joker_result);
}
```

#### **Web2 Delegate Changes:**
```javascript
// Coordinate joker selection
class JokerCoordinator {
  handleJokerSelection(gameId, playerId) { /* ... */ }
  notifyPlayersOfJokerResult(gameId, playerId, result) { /* ... */ }
}
```

## 🔧 PHASE 5: POLISH & ENHANCEMENT (NICE TO HAVE)

### **5.1 ENHANCED GAME STATE MANAGEMENT**
**Priority**: NICE TO HAVE
**Timeline**: Week 12-13

#### **Web3 Smart Contract Changes:**
```rust
// Add detailed game states
pub enum GameStatus {
    RelicSelection,
    CardPlaying,
    CardPositioning, // New
    InitialReveal,   // New
    Betting,
    FinalReveal,     // New
    Completed,
    SuddenDeath,
    Refunded,
}

// Add state validation
pub fn validate_game_state(game: &Game, action: &GameAction) -> Result<()>
```

#### **Web2 Delegate Changes:**
```javascript
// Enhanced state synchronization
class GameStateSynchronizer {
  syncGameState(gameId) { /* ... */ }
  handleStateTransition(gameId, oldState, newState) { /* ... */ }
  validateStateConsistency(gameId) { /* ... */ }
}
```

### **5.2 ERROR HANDLING & RECOVERY**
**Priority**: NICE TO HAVE
**Timeline**: Week 13-14

#### **Web2 Delegate Implementation:**
```javascript
class ErrorHandler {
  handleSmartContractError(gameId, error) { /* ... */ }
  recoverGameState(gameId) { /* ... */ }
  notifyPlayersOfError(gameId, error) { /* ... */ }
}
```

## 📋 IMPLEMENTATION CHECKLIST

### **Phase 1: Core Gameplay (Critical)**
- [ ] **Swap System Implementation**
  - [ ] Add swap functionality to smart contract
  - [ ] Add swap tracking for Third Eye relic
  - [ ] Implement swap validation
  - [ ] Test swap functionality

- [ ] **Card Positioning System**
  - [ ] Add card position tracking
  - [ ] Implement card rearrangement
  - [ ] Add position validation
  - [ ] Test positioning system

- [ ] **Progressive Card Reveal**
  - [ ] Split reveal into two phases
  - [ ] Add revealed/hidden card tracking
  - [ ] Implement reveal coordination
  - [ ] Test progressive reveal

- [ ] **Betting System Overhaul**
  - [ ] Add betting actions (RAISE/CALL/FOLD/REVEAL)
  - [ ] Implement betting state management
  - [ ] Add betting validation
  - [ ] Test betting system

### **Phase 2: Relic System (Important)**
- [ ] **Third Eye Relic**
  - [ ] Implement information sharing
  - [ ] Add swap reveal functionality
  - [ ] Test Third Eye coordination

- [ ] **Fast Hand Relic**
  - [ ] Implement timer modification
  - [ ] Add extra swap functionality
  - [ ] Test Fast Hand effects

- [ ] **Plan B Relic**
  - [ ] Implement 2-card play mode
  - [ ] Add extra swap functionality
  - [ ] Test Plan B effects

- [ ] **Fair Game Relic**
  - [ ] Implement relic negation
  - [ ] Add fair game coordination
  - [ ] Test Fair Game effects

### **Phase 3: Timer System (Important)**
- [ ] **Enhanced Timer Management**
  - [ ] Implement relic-modified timers
  - [ ] Add timer state synchronization
  - [ ] Test timer coordination

- [ ] **Auto-Lock Coordination**
  - [ ] Implement auto-lock functionality
  - [ ] Add minimum card enforcement
  - [ ] Test auto-lock system

### **Phase 4: Joker System (Nice to Have)**
- [ ] **Joker Selection**
  - [ ] Implement simple 50/50 selection
  - [ ] Add joker effects
  - [ ] Test joker functionality

### **Phase 5: Polish (Nice to Have)**
- [ ] **Enhanced State Management**
  - [ ] Add detailed game states
  - [ ] Implement state validation
  - [ ] Test state transitions

- [ ] **Error Handling**
  - [ ] Implement error recovery
  - [ ] Add error notification
  - [ ] Test error handling

## 🧪 TESTING STRATEGY

### **Unit Tests Required:**
- Swap functionality tests
- Card positioning tests
- Progressive reveal tests
- Betting action tests
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
- ✅ Remove all VRF-related code from smart contract
- ✅ Remove VRF dependencies from Cargo.toml
- ✅ Replace VRF with simple deterministic random
- ✅ Update joker selection logic
- ✅ Remove VRF from Web2 delegate

### **Timer System Migration:**
- ✅ Move timer logic from smart contract to Web2
- ✅ Add timer state synchronization
- ✅ Implement auto-lock coordination
- ✅ Add timer modification for relics

## 🔄 DEPLOYMENT STRATEGY

### **Phase 1 Deployment:**
1. Deploy updated smart contract with core gameplay
2. Deploy updated Web2 delegate with timer system
3. Test core functionality
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
1. Deploy all updates to mainnet
2. Test with real users
3. Monitor for issues
4. Coordinate with Unity client updates

## 📊 SUCCESS METRICS

### **Technical Metrics:**
- ✅ All core gameplay features implemented
- ✅ All relic effects working correctly
- ✅ Timer system accurate and reliable
- ✅ Error handling robust
- ✅ State synchronization working

### **User Experience Metrics:**
- ✅ Smooth game flow
- ✅ Responsive UI
- ✅ Reliable betting system
- ✅ Fair gameplay
- ✅ No game-breaking bugs

### **Performance Metrics:**
- ✅ Fast transaction processing
- ✅ Reliable timer accuracy
- ✅ Stable WebSocket connections
- ✅ Minimal error rates

## 🎯 NEXT STEPS

1. **Start Phase 1 Implementation** (Week 1)
2. **Coordinate Web2-Web3 Development** (Ongoing)
3. **Implement Testing Strategy** (Parallel)
4. **Prepare Unity Client Integration** (Future)
5. **Plan Production Deployment** (Phase 4)

## 📞 COORDINATION REQUIREMENTS

### **Web3 Team:**
- Implement smart contract changes
- Add new functions and structs
- Update error handling
- Test smart contract functionality

### **Web2 Team:**
- Implement timer management
- Add coordination logic
- Handle information sharing
- Manage state synchronization

### **Unity Team:**
- Prepare for new game flow
- Update UI for new features
- Test with updated systems
- Coordinate deployment timing 