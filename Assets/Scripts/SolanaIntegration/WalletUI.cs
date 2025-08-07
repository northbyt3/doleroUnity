using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for Solana wallet connection and management
/// </summary>
public class WalletUI : MonoBehaviour
{
    [Header("UI References")]
    public Button connectWalletButton;
    public Button disconnectWalletButton;
    public Button joinGameButton;
    public Button placeBetButton;
    public Button selectRelicButton;
    
    public TextMeshProUGUI walletStatusText;
    public TextMeshProUGUI walletAddressText;
    public TextMeshProUGUI walletBalanceText;
    public TextMeshProUGUI transactionStatusText;
    
    [Header("Betting UI")]
    public TMP_InputField betAmountInput;
    public TMP_Dropdown betActionDropdown;
    public TMP_Dropdown relicDropdown;
    
    [Header("Settings")]
    public bool showDebugInfo = true;
    
    private DoleroWalletManager walletManager;
    
    void Start()
    {
        // Get or create wallet manager
        walletManager = FindObjectOfType<DoleroWalletManager>();
        if (walletManager == null)
        {
            var walletObject = new GameObject("WalletManager");
            walletManager = walletObject.AddComponent<DoleroWalletManager>();
        }
        
        // Setup UI
        SetupUI();
        
        // Subscribe to wallet events
        DoleroWalletManager.OnWalletConnectionChanged += OnWalletConnectionChanged;
        DoleroWalletManager.OnWalletAddressChanged += OnWalletAddressChanged;
        DoleroWalletManager.OnBalanceChanged += OnBalanceChanged;
        DoleroWalletManager.OnTransactionCompleted += OnTransactionCompleted;
        DoleroWalletManager.OnTransactionFailed += OnTransactionFailed;
        
        // Update UI with current status
        UpdateUI();
    }
    
    void SetupUI()
    {
        // Setup buttons
        if (connectWalletButton != null)
        {
            connectWalletButton.onClick.AddListener(OnConnectWalletClicked);
        }
        
        if (disconnectWalletButton != null)
        {
            disconnectWalletButton.onClick.AddListener(OnDisconnectWalletClicked);
        }
        
        if (joinGameButton != null)
        {
            joinGameButton.onClick.AddListener(OnJoinGameClicked);
        }
        
        if (placeBetButton != null)
        {
            placeBetButton.onClick.AddListener(OnPlaceBetClicked);
        }
        
        if (selectRelicButton != null)
        {
            selectRelicButton.onClick.AddListener(OnSelectRelicClicked);
        }
        
        // Setup dropdowns
        if (betActionDropdown != null)
        {
            betActionDropdown.ClearOptions();
            betActionDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "RAISE", "CALL", "FOLD", "REVEAL"
            });
        }
        
        if (relicDropdown != null)
        {
            relicDropdown.ClearOptions();
            relicDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "High Stakes", "Plan B", "Third Eye", "Fair Game", "The Closer", "Fast Hand"
            });
        }
        
        // Setup input field
        if (betAmountInput != null)
        {
            betAmountInput.text = "0.1";
        }
    }
    
    void OnConnectWalletClicked()
    {
        Debug.Log("🔗 Connecting wallet...");
        walletManager.ConnectWallet();
    }
    
    void OnDisconnectWalletClicked()
    {
        Debug.Log("🔌 Disconnecting wallet...");
        walletManager.DisconnectWallet();
    }
    
    void OnJoinGameClicked()
    {
        if (!walletManager.isWalletConnected)
        {
            ShowTransactionStatus("❌ Wallet not connected!", Color.red);
            return;
        }
        
        Debug.Log("🎮 Joining game...");
        walletManager.JoinGame();
        ShowTransactionStatus("🎮 Joining game...", Color.yellow);
    }
    
    void OnPlaceBetClicked()
    {
        if (!walletManager.isWalletConnected)
        {
            ShowTransactionStatus("❌ Wallet not connected!", Color.red);
            return;
        }
        
        if (betActionDropdown == null || betAmountInput == null)
        {
            ShowTransactionStatus("❌ Betting UI not configured!", Color.red);
            return;
        }
        
        string action = betActionDropdown.options[betActionDropdown.value].text;
        double amount = 0.0;
        
        if (!double.TryParse(betAmountInput.text, out amount))
        {
            ShowTransactionStatus("❌ Invalid bet amount!", Color.red);
            return;
        }
        
        Debug.Log($"💰 Placing bet: {action} {amount} SOL");
        walletManager.PlaceBet(action, amount);
        ShowTransactionStatus($"💰 Placing bet: {action} {amount} SOL", Color.yellow);
    }
    
    void OnSelectRelicClicked()
    {
        if (!walletManager.isWalletConnected)
        {
            ShowTransactionStatus("❌ Wallet not connected!", Color.red);
            return;
        }
        
        if (relicDropdown == null)
        {
            ShowTransactionStatus("❌ Relic UI not configured!", Color.red);
            return;
        }
        
        int relicId = relicDropdown.value + 1; // Relic IDs are 1-based
        string relicName = relicDropdown.options[relicDropdown.value].text;
        
        Debug.Log($"🔮 Selecting relic: {relicName} (ID: {relicId})");
        walletManager.SelectRelic(relicId);
        ShowTransactionStatus($"🔮 Selecting relic: {relicName}", Color.yellow);
    }
    
    void OnWalletConnectionChanged(bool connected)
    {
        UpdateUI();
        
        if (connected)
        {
            ShowTransactionStatus("✅ Wallet connected!", Color.green);
        }
        else
        {
            ShowTransactionStatus("❌ Wallet disconnected", Color.red);
        }
    }
    
    void OnWalletAddressChanged(string address)
    {
        UpdateUI();
    }
    
    void OnBalanceChanged(double balance)
    {
        UpdateUI();
    }
    
    void OnTransactionCompleted(string transactionType)
    {
        ShowTransactionStatus($"✅ Transaction completed: {transactionType}", Color.green);
    }
    
    void OnTransactionFailed(string error)
    {
        ShowTransactionStatus($"❌ Transaction failed: {error}", Color.red);
    }
    
    void UpdateUI()
    {
        var status = walletManager.GetWalletStatus();
        
        // Update status text
        if (walletStatusText != null)
        {
            walletStatusText.text = $"Wallet: {status.status}";
            walletStatusText.color = status.isConnected ? Color.green : Color.red;
        }
        
        // Update address text
        if (walletAddressText != null)
        {
            if (status.isConnected)
            {
                string shortAddress = status.address.Length > 10 
                    ? status.address.Substring(0, 6) + "..." + status.address.Substring(status.address.Length - 4)
                    : status.address;
                walletAddressText.text = $"Address: {shortAddress}";
            }
            else
            {
                walletAddressText.text = "Address: Not connected";
            }
        }
        
        // Update balance text
        if (walletBalanceText != null)
        {
            if (status.isConnected)
            {
                walletBalanceText.text = $"Balance: {status.balance:F4} SOL";
            }
            else
            {
                walletBalanceText.text = "Balance: --";
            }
        }
        
        // Update button states
        UpdateButtonStates(status.isConnected);
    }
    
    void UpdateButtonStates(bool walletConnected)
    {
        if (connectWalletButton != null)
        {
            connectWalletButton.interactable = !walletConnected;
        }
        
        if (disconnectWalletButton != null)
        {
            disconnectWalletButton.interactable = walletConnected;
        }
        
        if (joinGameButton != null)
        {
            joinGameButton.interactable = walletConnected;
        }
        
        if (placeBetButton != null)
        {
            placeBetButton.interactable = walletConnected;
        }
        
        if (selectRelicButton != null)
        {
            selectRelicButton.interactable = walletConnected;
        }
    }
    
    void ShowTransactionStatus(string message, Color color)
    {
        if (transactionStatusText != null)
        {
            transactionStatusText.text = message;
            transactionStatusText.color = color;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Wallet UI: {message}");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        DoleroWalletManager.OnWalletConnectionChanged -= OnWalletConnectionChanged;
        DoleroWalletManager.OnWalletAddressChanged -= OnWalletAddressChanged;
        DoleroWalletManager.OnBalanceChanged -= OnBalanceChanged;
        DoleroWalletManager.OnTransactionCompleted -= OnTransactionCompleted;
        DoleroWalletManager.OnTransactionFailed -= OnTransactionFailed;
    }
}
