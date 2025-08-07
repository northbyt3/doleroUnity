# 🔗 Solana Wallet Integration Guide - DOLERO

## 🎯 Overview

This guide shows how to integrate Solana wallet functionality with your DOLERO game using the Solana Unity SDK.

## 🚀 Quick Setup

### Step 1: Add Wallet Components
1. **Create a GameObject** named `DoleroWallet`
2. **Add these components**:
   - `DoleroWalletManager` - Core wallet functionality
   - `WalletUI` - User interface for wallet operations
   - `SimpleDirectWebSocket` - Server communication

### Step 2: Configure Wallet Settings
In the `DoleroWalletManager` component:
- **RPC URL**: `https://api.devnet.solana.com` (for testing)
- **Auto Connect**: `false` (recommended for manual control)

### Step 3: Test Wallet Connection
1. **Play the scene**
2. **Click "Connect Wallet"** in the UI
3. **Check Console** for connection status

## 🔧 Wallet Manager Features

### Core Functions
```csharp
// Connect to wallet
DoleroWalletManager.Instance.ConnectWallet();

// Disconnect wallet
DoleroWalletManager.Instance.DisconnectWallet();

// Get wallet status
var status = DoleroWalletManager.Instance.GetWalletStatus();
```

### Game Transactions
```csharp
// Join a game (requires wallet)
DoleroWalletManager.Instance.JoinGame("game-123");

// Place a bet
DoleroWalletManager.Instance.PlaceBet("RAISE", 0.1);

// Select a relic
DoleroWalletManager.Instance.SelectRelic(1);
```

## 🎮 Integration with Your Game

### 1. Connect to Existing Game Components
```csharp
// In your GameManager or main game script
public class GameManager : MonoBehaviour
{
    private DoleroWalletManager walletManager;
    
    void Start()
    {
        walletManager = FindObjectOfType<DoleroWalletManager>();
        
        // Subscribe to wallet events
        DoleroWalletManager.OnWalletConnectionChanged += OnWalletConnectionChanged;
        DoleroWalletManager.OnTransactionCompleted += OnTransactionCompleted;
    }
    
    void OnWalletConnectionChanged(bool connected)
    {
        if (connected)
        {
            // Enable game features that require wallet
            EnableWalletFeatures();
        }
        else
        {
            // Disable wallet-dependent features
            DisableWalletFeatures();
        }
    }
    
    void OnTransactionCompleted(string transactionType)
    {
        switch (transactionType)
        {
            case "JoinGame":
                StartGame();
                break;
            case "PlaceBet":
                UpdateBettingUI();
                break;
            case "SelectRelic":
                ApplyRelicEffect();
                break;
        }
    }
}
```

### 2. UI Integration
```csharp
// Add wallet UI to your existing UI
public class GameUI : MonoBehaviour
{
    public Button connectWalletButton;
    public Button joinGameButton;
    public TextMeshProUGUI walletStatusText;
    
    void Start()
    {
        // Setup wallet UI
        if (connectWalletButton != null)
        {
            connectWalletButton.onClick.AddListener(() => {
                DoleroWalletManager.Instance.ConnectWallet();
            });
        }
        
        if (joinGameButton != null)
        {
            joinGameButton.onClick.AddListener(() => {
                if (DoleroWalletManager.Instance.isWalletConnected)
                {
                    DoleroWalletManager.Instance.JoinGame();
                }
            });
        }
    }
}
```

## 🔐 Wallet Security

### Best Practices
1. **Always verify wallet connection** before transactions
2. **Use Devnet for testing** (not Mainnet)
3. **Handle transaction failures** gracefully
4. **Validate user input** before sending transactions

### Error Handling
```csharp
// Subscribe to transaction events
DoleroWalletManager.OnTransactionFailed += OnTransactionFailed;

void OnTransactionFailed(string error)
{
    Debug.LogError($"Transaction failed: {error}");
    // Show error to user
    ShowErrorMessage($"Transaction failed: {error}");
}
```

## 🧪 Testing Setup

### Demo Mode
The wallet manager includes a **demo mode** for testing:
- ✅ Simulates wallet connection
- ✅ Simulates transactions
- ✅ Works without actual Solana SDK
- ✅ Perfect for development and testing

### Real Wallet Integration
To connect to real wallets (Phantom, Solflare, etc.):

1. **Install Solana Unity SDK**:
   ```
   Package Manager → Add package from git URL
   https://github.com/magicblock-labs/Solana.Unity-SDK.git
   ```

2. **Update wallet connection code**:
   ```csharp
   // Replace demo code with actual SDK calls
   // This will be implemented based on the actual SDK structure
   ```

## 📱 UI Components

### Required UI Elements
- **Connect Wallet Button** - Connect to Solana wallet
- **Disconnect Button** - Disconnect wallet
- **Wallet Status Text** - Show connection status
- **Wallet Address Text** - Display wallet address
- **Balance Text** - Show SOL balance
- **Transaction Status** - Show transaction progress

### Optional UI Elements
- **Bet Amount Input** - Enter bet amount
- **Bet Action Dropdown** - Select bet action (RAISE/CALL/FOLD)
- **Relic Dropdown** - Select relic
- **Join Game Button** - Join a game
- **Place Bet Button** - Place a bet

## 🔄 Event System

### Wallet Events
```csharp
// Subscribe to wallet events
DoleroWalletManager.OnWalletConnectionChanged += OnWalletConnectionChanged;
DoleroWalletManager.OnWalletAddressChanged += OnWalletAddressChanged;
DoleroWalletManager.OnBalanceChanged += OnBalanceChanged;
DoleroWalletManager.OnTransactionCompleted += OnTransactionCompleted;
DoleroWalletManager.OnTransactionFailed += OnTransactionFailed;
```

### Event Parameters
- `OnWalletConnectionChanged(bool connected)` - Wallet connection status
- `OnWalletAddressChanged(string address)` - Wallet address
- `OnBalanceChanged(double balance)` - SOL balance
- `OnTransactionCompleted(string transactionType)` - Successful transaction
- `OnTransactionFailed(string error)` - Failed transaction

## 🎯 Game Integration Examples

### 1. Betting System Integration
```csharp
public class BettingSystem : MonoBehaviour
{
    public void PlaceBet(string action, double amount)
    {
        if (DoleroWalletManager.Instance.isWalletConnected)
        {
            DoleroWalletManager.Instance.PlaceBet(action, amount);
        }
        else
        {
            Debug.LogError("Wallet not connected!");
        }
    }
}
```

### 2. Relic System Integration
```csharp
public class RelicSystem : MonoBehaviour
{
    public void SelectRelic(int relicId)
    {
        if (DoleroWalletManager.Instance.isWalletConnected)
        {
            DoleroWalletManager.Instance.SelectRelic(relicId);
        }
        else
        {
            Debug.LogError("Wallet not connected!");
        }
    }
}
```

### 3. Game State Integration
```csharp
public class GameStateManager : MonoBehaviour
{
    void Start()
    {
        // Subscribe to wallet events
        DoleroWalletManager.OnTransactionCompleted += OnTransactionCompleted;
    }
    
    void OnTransactionCompleted(string transactionType)
    {
        switch (transactionType)
        {
            case "JoinGame":
                // Start the game
                StartGame();
                break;
                
            case "PlaceBet":
                // Update betting UI
                UpdateBettingDisplay();
                break;
                
            case "SelectRelic":
                // Apply relic effects
                ApplyRelicEffects();
                break;
        }
    }
}
```

## 🚀 Next Steps

1. **Test the demo mode** - Verify everything works
2. **Add UI elements** - Connect buttons and text fields
3. **Integrate with your game** - Connect to existing systems
4. **Test transactions** - Verify betting and relic selection
5. **Add real wallet support** - Install Solana SDK when ready

## 🐛 Troubleshooting

### Common Issues
1. **"Wallet not connected"** - Call `ConnectWallet()` first
2. **Transaction fails** - Check wallet balance and network
3. **UI not updating** - Verify event subscriptions
4. **Demo mode not working** - Check console for errors

### Debug Tips
- Enable `showDebugInfo` in WalletUI
- Check Console for detailed logs
- Verify all UI references are assigned
- Test with demo mode first

The wallet integration is now ready to use! 🎉
