# DOLERO Unity Solana Integration - Testing Guide

## Quick Start Testing (Press F4 in Play Mode)

### 1. Initial Connection Test
When you press F4, the test panel will appear. Start with these tests:

1. **Test Server Connection**
   - Click "Test Server Connection"
   - You should see: "Connected to Web2 server at 174.138.42.117"
   - If it fails, check your internet connection

2. **Test Wallet Connection** (Optional for now)
   - Click "Connect Wallet"
   - This will attempt to connect to Phantom or other Solana wallets
   - Note: This may not work fully without proper Solana SDK setup

### 2. Game Flow Testing

Test the game phases in order:

#### Phase 1: Relic Selection
- Click "Start Relic Selection"
- You should see 3 relic options appear
- Test selecting different relics:
  - **High Stakes**: +2 hearts, higher stakes
  - **Plan B**: 6 swaps for 2-card play
  - **Third Eye**: Reveals first swap
  - **Fair Game**: Negates all relics
  - **The Closer**: Bonus for 1 heart wins
  - **Fast Hand**: Reduces timer to 15 seconds

#### Phase 2: Card Playing
- Click "Test Card Playing"
- This simulates drawing and playing cards
- Watch for card animations and positioning

#### Phase 3: Card Swapping
- Click "Test Card Swap"
- Select two cards to swap positions
- Verify the swap animation works

#### Phase 4: Progressive Reveal
- Click "Test Initial Reveal"
- Cards 3 and 2 should reveal
- Then click "Test Final Reveal"
- Card 1 should reveal

#### Phase 5: Betting
- Test betting actions:
  - "Test Raise" - Increases bet
  - "Test Call" - Matches current bet
  - "Test Fold" - Forfeit round
  - "Test Reveal" - Show all cards

### 3. Integration Features to Test

#### Timer System
- Watch the timer countdown during card playing phase
- Test if Fast Hand relic reduces timer to 15 seconds
- Verify auto-lock when timer expires

#### Heart System
- Check heart display updates
- Test damage when folding
- Verify game ends at 0 hearts

#### Pot System
- Watch pot value increase with bets
- Verify rake calculation (5% to house)
- Check payout on win

### 4. Server Communication Test

Click "Test Full Game Flow" to run an automated test that:
1. Connects to server
2. Joins a game
3. Selects a relic
4. Plays cards
5. Makes swaps
6. Places bets
7. Completes a round

### 5. Console Output

Open the Unity Console (Window > General > Console) to see:
- Connection status messages
- Server responses
- Error messages (if any)
- Game state updates

## Troubleshooting

### If Server Connection Fails:
1. Check internet connection
2. Verify server is running at 174.138.42.117
3. Check firewall settings
4. Look for error messages in console

### If Wallet Connection Fails:
1. This is expected without full Solana SDK setup
2. The game can still run in test mode without blockchain

### If UI Elements Don't Appear:
1. Make sure you've assigned UI elements in Unity Inspector:
   - Select DoleroTestManager in hierarchy
   - Drag UI elements to their slots in Inspector
   - Save the scene

### If Game Flow Breaks:
1. Press F4 to open test panel
2. Click "Reset Game State"
3. Start testing from Phase 1 again

## What's Working Now

✅ Core game state management
✅ Phase transitions
✅ Timer system
✅ Betting logic
✅ Card swap system
✅ Progressive reveal
✅ Relic effects
✅ Server communication structure

## What Needs Additional Setup

⚠️ Actual Solana wallet connection (needs SDK configuration)
⚠️ Real blockchain transactions (needs wallet setup)
⚠️ Token transfers (needs SPL token setup)

## Next Steps

1. Test all features using F4 panel
2. Check console for any errors
3. Verify server responses
4. Once basic testing works, proceed to:
   - Configure Solana wallet adapter
   - Set up RPC endpoint
   - Test on Devnet
   - Deploy smart contracts

Press F4 now to start testing!
