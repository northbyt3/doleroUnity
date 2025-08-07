# DOLERO - Game Context & Technical Specification

## Game Overview
DOLERO is a sophisticated 1v1 card game that combines elements of Blackjack, Poker, and strategic card placement with unique relic mechanics and betting phases. Players compete to get closest to 21 without busting, while managing hearts, relics, and strategic betting. The game supports multiple token types for betting, including SOL and BONK.

## Table System & Economics

### Table Tiers
| Table | Seat Fee | Max Bet | Rake | Rake Cap | Token Type |
|-------|----------|---------|------|----------|------------|
| Micro | 0.01 SOL | 0.15 SOL | 3% | 0.006 SOL | SOL |
| Medium | 0.05 SOL | 1.25 SOL | 3% | 0.05 SOL | SOL |
| Large | 0.5 SOL | 15 SOL | 3% | 0.1 SOL | SOL |
| BONK | 1000 BONK | 15000 BONK | 3% | 300 BONK | BONK |

### Economic Flow
1. **Seat Payment**: Players pay seat fee upon entering game (SOL or BONK based on table type)
2. **Pot Building**: Seat fees + betting amounts accumulate in pot (same token type as table)
3. **Rake Collection**: 3% of final pot (capped per table) sent to house account
4. **Winner Payout**: Remaining pot sent to game winner
5. **Target Account**: Rake fees sent to designated house account (must be configurable per token type)

### Multi-Token Support
- **SOL Tables**: Traditional SOL-based betting with standard SOL transaction handling
- **BONK Tables**: BONK token betting with SPL token transaction handling
- **Token Validation**: Each table type enforces specific token usage
- **Cross-Token Prevention**: Players cannot mix SOL and BONK in same game
- **Network Support**: Both mainnet and devnet configurations supported

## Game Structure

### Initial Game State
- **Players**: 2 players (Player 1, Player 2)
- **Hearts**: Each player starts with 3 hearts
- **Pot**: Initialized with seat fees from both players
- **Game Status**: Active until one player wins or both players have 0 hearts

### Game Loop
```
Game Start → Round Loop → Game End
    ↓
Round: Relic Selection → Card Playing → Betting → Resolution
    ↓
If both players have hearts: Continue to next round
If one player has 0 hearts: Game Over, other player wins
If both players have 0 hearts: Sudden Death round(s)
```

## Round Mechanics

### Phase 1: Relic Selection
**Duration**: 15 seconds per player
**Options**: 3 relics + 1 JOKER + Skip option

#### Relic Effects
1. **High Stakes**
   - Win: Deal 2 ❤️ heart damage to opponent
   - Lose: Receive 2 ❤️ heart damage
   - Risk/Reward: High volatility
   - Note: Affects both winner and loser heart changes

2. **Plan B**
   - Play only 2 cards (instead of 2-3)
   - Receive 6 ♻️ swaps (instead of 3)
   - Strategy: More flexibility, fewer cards

3. **Third Eye**
   - Show your first ♻️ swap to opponent
   - See opponent's first ♻️ swap
   - Information: Creates information asymmetry

4. **Fair Game**
   - Negate all relics and jokers for the round
   - Strategy: Neutralizes opponent's advantages

5. **The Closer**
   - Win with exactly 21: Gain 1 ❤️ heart
   - Tie: Lose 2 ❤️ hearts
   - Precision: Rewards perfect play

6. **Fast Hand**
   - Gain 1+ ♻️ swap
   - Timer reduced to 15 seconds
   - Pressure: Time constraint for more options
   - Note: Exact swap gain not specified in original description

#### JOKER System
- **Selection**: 50/50 probability of good or bad joker (must use MagicBlock VRF)
- **Good JOKER**: +1 ♻️ swap
- **Bad JOKER**: -1 ♻️ swap
- **Risk**: Uncertainty adds strategic element

### Phase 2: Card Playing
**Base Timer**: 30 seconds (modifiable by relics)
**Base Swaps**: 3 swaps (modifiable by relics)

#### Card Management
- **Hand Size**: 3 cards drawn from deck (must use MagicBlock VRF)
- **Table Setup**: 3 blank cards on each player's side
- **Card Placement**: Play cards to substitute blank cards
- **Position Importance**: Cards numbered 1, 2, 3 (reveal order: 3→2→1)
- **Card Rearrangement**: Players can change the order of played cards on their table
- **Strategic Positioning**: Card order affects betting phase reveal sequence
- **Fair Dealing**: All card draws must use MagicBlock VRF for provably fair randomness

#### Swap Mechanics
- **Multi-Card Swaps**: Select any number of cards to discard
- **Single Swap Cost**: 1 swap action regardless of cards discarded
- **Examples**:
  - Swap 1 card (4→Q): 1 swap used
  - Swap 2 cards (4,3→Q,A): 1 swap used
  - Swap 3 cards (2,3,Q→K,7,9): 1 swap used

#### Third Eye Interaction
- **First Swap Reveal**: If Third Eye active, first swap shown to opponent
- **Information Sharing**: Both players see each other's first swap results
- **Notification Only**: Revealed cards don't affect gameplay, just information

#### Play Requirements
- **Minimum Cards**: Must play at least 2 cards
- **Manual Lock**: Cannot lock with <2 cards played
- **Auto-Lock**: Timeout or insufficient cards triggers automatic play
- **Auto-Play Logic**: If <2 cards played at timeout, cards 1 and 2 auto-played
- **Card Rearrangement**: Players can reorder their played cards before locking in
- **Final Positioning**: Card order is locked when player locks in
- **Blank Card Handling**: If only 2 cards played, 1 blank card remains on table
- **Full Table**: If 3 cards played, no blank cards remain

#### Lock-In Process
1. Player manually locks or timeout occurs
2. Cards on table flipped face down
3. Both players must lock before proceeding

### Phase 3: Betting Phase
**Trigger**: Both players have locked in their cards

#### Progressive Reveal
1. **Cards 3 & 2**: Revealed face up for both players (based on final card order)
2. **Card 1**: Remains face down (hidden information)
3. **Strategic Betting**: Players bet based on partial information
4. **Order Significance**: Card positioning affects betting strategy and bluffing opportunities

#### Betting Options
- **REVEAL**: End betting, proceed to final reveal
- **RAISE**: Propose additional bet amount (within table limits)
- **CALL**: Accept opponent's raise (pay the raise amount)
- **FOLD**: Decline raise, lose 1 heart, lose round
- **Max Bet Limitation**: If at max bet, only REVEAL option available

#### Raise Mechanics
- **Raise Limits**: Cannot exceed table max bet
- **Raise Continuation**: Players can counter-raise
- **Max Bet Scenario**: If at max bet, only REVEAL option available
- **Betting Loop**: Continues until CALL or FOLD

#### Betting Flow
```
Player 1: RAISE or REVEAL
    ↓
If REVEAL: Ask Player 2 (RAISE or REVEAL)
    ↓
If both REVEAL: End betting phase
    ↓
If RAISE: Player 1 proposes amount
    ↓
Player 2: RAISE, CALL, or FOLD
    ↓
If CALL: End betting phase
If FOLD: Player 2 loses round
If RAISE: Continue loop
```

### Phase 4: Resolution
**Trigger**: Betting phase ends (both REVEAL or CALL/FOLD)

#### Final Reveal
- **Card 1**: Revealed for both players
- **Score Calculation**: Sum of all played cards
- **Bust Check**: Score > 21 = bust

#### Winner Determination
- **Primary**: Closest to 21 without busting
- **Bust Handling**: Busting doesn't immediately lose (hearts system)
- **Tie**: Both players lose 1 heart (unless modified by relics)
- **Card Values**: Standard card values (A=1 or 11, face cards=10, number cards=face value)
- **Bust Definition**: Score > 21

#### Heart System
- **Loss**: Loser loses 1 heart (modified by relics)
- **Win**: Winner gains/loses hearts based on relics
- **Game Continuation**: Both players have >0 hearts
- **Game End**: One player reaches 0 hearts

## Game End Conditions

### Normal Game End
- **Player 1 Wins**: Player 2 has 0 hearts, Player 1 has >0 hearts
- **Player 2 Wins**: Player 1 has 0 hearts, Player 2 has >0 hearts

### Sudden Death
- **Trigger**: Both players have 0 hearts
- **Process**: Play one additional round
- **Winner**: Round winner becomes game winner
- **Tie**: Continue sudden death rounds until winner

### Early Termination
- **Disconnect**: Opponent automatically wins
- **Forfeit**: Opponent automatically wins
- **Winner Payout**: Early termination winner receives full pot (minus rake)

## Technical Implementation Requirements

### MagicBlock VRF Requirement
**Provably Fair Randomness**: DOLERO must use MagicBlock VRF (Verifiable Random Function) system for all card drawing and swapping operations to ensure fair and tamper-proof gameplay.

### State Management
```typescript
interface GameState {
  gameId: string;
  tableType: 'micro' | 'medium' | 'large';
  players: {
    player1: PlayerState;
    player2: PlayerState;
  };
  pot: number;
  currentPhase: GamePhase;
  roundNumber: number;
  bettingHistory: BettingAction[];
  gameEndCondition?: GameEndCondition;
}

interface PlayerState {
  id: string;
  hearts: number;
  hand: Card[];
  tableCards: (Card | null)[]; // Ordered array [position1, position2, position3]
  swaps: number;
  timer: number;
  selectedRelic?: Relic;
  jokerResult?: 'good' | 'bad';
  isLocked: boolean;
  isDisconnected: boolean;
  cardOrderLocked: boolean; // Whether card positions are finalized
}
```

### Timer System
- **Multiple Timers**: Relic selection, card playing
- **Timer Modifications**: Relic effects can change timer duration
- **Auto-Actions**: Timeout triggers automatic actions
- **Synchronization**: Timers must be synchronized between players

### Card System
**MagicBlock VRF Requirement**: All card drawing and swapping operations must use MagicBlock VRF system for provably fair randomness.

```typescript
interface Card {
  suit: 'hearts' | 'diamonds' | 'clubs' | 'spades' | 'blank';
  value: number; // 1-13, 0 for blank
  isFaceDown: boolean;
  position?: 1 | 2 | 3; // Table position
}

interface Deck {
  cards: Card[];
  shuffle(): void; // Must use MagicBlock VRF
  draw(count: number): Card[]; // Must use MagicBlock VRF
  reset(): void;
}
```

### Relic System
```typescript
interface Relic {
  id: string;
  name: string;
  description: string;
  effect: RelicEffect;
  duration: 'round' | 'game';
}

interface RelicEffect {
  modifySwaps?: (baseSwaps: number) => number;
  modifyTimer?: (baseTimer: number) => number;
  modifyHearts?: (baseHearts: number, isWinner: boolean) => number;
  revealFirstSwap?: boolean;
  negateRelics?: boolean;
}
```

### Betting System
```typescript
interface BettingAction {
  playerId: string;
  action: 'raise' | 'call' | 'fold' | 'reveal';
  amount?: number;
  timestamp: number;
}

interface Pot {
  baseAmount: number; // Seat fees
  currentBet: number;
  totalAmount: number;
  rakeAmount: number;
  winnerAmount: number;
}
```

### Network Requirements
- **Real-time Sync**: Game state synchronization
- **Disconnect Handling**: Graceful player disconnection
- **Anti-cheat**: Secure card reveal and betting
- **State Validation**: Server-side game state verification
- **VRF Requirement**: MagicBlock VRF must be used for all card drawing and swapping

### UI/UX Considerations
- **Phase Indicators**: Clear indication of current game phase
- **Timer Display**: Visible countdown timers
- **Card Visualization**: Clear card placement and reveal
- **Card Rearrangement**: Intuitive drag-and-drop or click-to-reorder interface
- **Betting Interface**: Intuitive raise/call/fold controls
- **Relic Selection**: Easy relic and joker selection
- **Information Display**: Heart counts, pot size, betting history
- **Position Preview**: Visual indication of reveal order (3→2→1)
- **Skip Option**: Clear skip button during relic selection phase
- **JOKER Probability**: Visual indication of 50/50 chance for joker selection

## Game Balance & Strategy

### Strategic Elements
1. **Information Management**: Third Eye relic creates information asymmetry
2. **Risk Assessment**: High Stakes and JOKER add uncertainty
3. **Resource Management**: Limited swaps and time create tension
4. **Positional Strategy**: Card placement and ordering affects betting decisions
5. **Bluffing**: Hidden card and card positioning allow for strategic deception
6. **Card Positioning**: Strategic ordering of cards for optimal reveal sequence

### Economic Balance
- **Rake Structure**: 3% with caps ensures profitability
- **Escalating Stakes**: Betting phase allows pot building
- **Seat Fees**: Guarantees minimum pot size
- **Risk/Reward**: High Stakes relic balances risk and reward

### Relic Balance
- **High Stakes**: High volatility, high impact
- **Plan B**: Flexibility vs. card count trade-off
- **Third Eye**: Information advantage
- **Fair Game**: Neutralizing option
- **The Closer**: Skill-based reward
- **Fast Hand**: Time pressure for more options

## Development Priorities

### Phase 1: Core Engine
1. Card management and deck system (must use MagicBlock VRF)
2. Basic game state management
3. Timer system implementation
4. Player synchronization

### Phase 2: Game Mechanics
1. Relic system implementation
2. Swap mechanics
3. Card placement and reveal
4. Basic betting system

### Phase 3: Advanced Features
1. Progressive reveal betting
2. Heart system and game end conditions
3. Economic system (rake, payouts)
4. Disconnect handling

### Phase 4: Polish
1. UI/UX implementation
2. Anti-cheat measures
3. Performance optimization
4. Testing and balance adjustments

## Error Handling & Edge Cases

### Disconnection Scenarios
- Player disconnects during relic selection
- Player disconnects during card playing
- Player disconnects during betting
- Reconnection handling

### Timer Edge Cases
- Timer expires during relic selection (counts as skip)
- Timer expires during card playing (auto-lock with current cards)
- Timer modifications by relics
- Network latency affecting timers
- 15-second timeout for relic selection
- 30-second base timeout for card playing (modifiable by relics)

### Betting Edge Cases
- Max bet reached during betting
- Invalid raise amounts
- Concurrent betting actions
- Betting after fold

### Game State Edge Cases
- Invalid game state transitions
- Missing player actions
- Corrupted game state
- Sudden death tie resolution

This document serves as the complete technical specification for implementing the DOLERO card game. All game mechanics, rules, and technical requirements are detailed for development reference. 
