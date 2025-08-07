using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Automatically creates test UI for DoleroTestManager
/// Add this to any GameObject and run once
/// </summary>
public class AutoCreateTestUI : MonoBehaviour
{
    void Start()
    {
        CreateTestUI();
    }
    
    void CreateTestUI()
    {
        Debug.Log("Creating Test UI...");
        
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("Created Canvas");
        }
        
        // Create Test Panel
        GameObject panelGO = new GameObject("TestPanel");
        panelGO.transform.SetParent(canvas.transform, false);
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.9f);
        
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.7f, 0.1f);
        panelRect.anchorMax = new Vector2(0.95f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Add Vertical Layout Group
        VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        
        // Create Title
        CreateText(panelGO.transform, "DOLERO TEST PANEL", 20, true);
        CreateText(panelGO.transform, "Press F4 to Toggle", 12, false);
        CreateSeparator(panelGO.transform);
        
        // Server Testing Section
        CreateText(panelGO.transform, "SERVER TESTING", 16, true);
        GameObject serverBtn = CreateButton(panelGO.transform, "Test Server Connection", "TestServerButton");
        GameObject serverStatus = CreateText(panelGO.transform, "Status: Not Connected", 12, false);
        serverStatus.name = "ServerStatusText";
        CreateSeparator(panelGO.transform);
        
        // Wallet Testing Section
        CreateText(panelGO.transform, "WALLET TESTING", 16, true);
        GameObject walletBtn = CreateButton(panelGO.transform, "Simulate Wallet", "SimulateWalletButton");
        GameObject walletStatus = CreateText(panelGO.transform, "Wallet: Not Connected", 12, false);
        walletStatus.name = "WalletStatusText";
        CreateSeparator(panelGO.transform);
        
        // Game Flow Section
        CreateText(panelGO.transform, "GAME FLOW TESTING", 16, true);
        GameObject startBtn = CreateButton(panelGO.transform, "Start Game Flow", "StartGameFlowButton");
        GameObject swapBtn = CreateButton(panelGO.transform, "Test Card Swap", "TestSwapButton");
        GameObject betBtn = CreateButton(panelGO.transform, "Test Betting", "TestBettingButton");
        GameObject revealBtn = CreateButton(panelGO.transform, "Test Reveal", "TestRevealButton");
        GameObject flowStatus = CreateText(panelGO.transform, "Game: Idle", 12, false);
        flowStatus.name = "GameFlowStatusText";
        
        // Now assign to DoleroTestManager if it exists
        DoleroTestManager testManager = FindObjectOfType<DoleroTestManager>();
        if (testManager != null)
        {
            // Use reflection or make fields public to assign
            Debug.Log("Found DoleroTestManager - Manual assignment needed in Inspector");
            Debug.Log("Please assign the created UI elements to DoleroTestManager:");
            Debug.Log("- TestServerButton → Test Server Button");
            Debug.Log("- ServerStatusText → Server Status Text");
            Debug.Log("- SimulateWalletButton → Simulate Wallet Button");
            Debug.Log("- WalletStatusText → Wallet Status Text");
            Debug.Log("- StartGameFlowButton → Start Game Flow Button");
            Debug.Log("- TestSwapButton → Test Swap Button");
            Debug.Log("- TestBettingButton → Test Betting Button");
            Debug.Log("- TestRevealButton → Test Reveal Button");
            Debug.Log("- GameFlowStatusText → Game Flow Status Text");
        }
        
        // Start with panel hidden
        panelGO.SetActive(false);
        
        Debug.Log("✅ Test UI Created! Now assign the elements to DoleroTestManager in the Inspector");
        Debug.Log("The panel is hidden by default. Press F4 to show it.");
    }
    
    GameObject CreateButton(Transform parent, string text, string name)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.2f);
        
        Button button = buttonGO.AddComponent<Button>();
        
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(200, 30);
        
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 14;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return buttonGO;
    }
    
    GameObject CreateText(Transform parent, string text, int fontSize, bool bold)
    {
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(parent, false);
        
        TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = bold ? Color.yellow : Color.white;
        if (bold) tmpText.fontStyle = FontStyles.Bold;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(200, 25);
        
        return textGO;
    }
    
    void CreateSeparator(Transform parent)
    {
        GameObject sep = new GameObject("Separator");
        sep.transform.SetParent(parent, false);
        
        Image sepImage = sep.AddComponent<Image>();
        sepImage.color = new Color(1, 1, 1, 0.2f);
        
        RectTransform sepRect = sep.GetComponent<RectTransform>();
        sepRect.sizeDelta = new Vector2(180, 1);
    }
}
