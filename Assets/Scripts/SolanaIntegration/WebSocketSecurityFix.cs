using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Fixes Unity's WebSocket security restrictions to allow ws:// connections
/// This script should be added to a GameObject in your scene
/// </summary>
public class WebSocketSecurityFix : MonoBehaviour
{
    [Header("Security Settings")]
    public bool enableInsecureConnections = true;
    public bool autoApplyOnStart = true;
    
    void Start()
    {
        if (autoApplyOnStart)
        {
            ApplySecurityFix();
        }
    }
    
    /// <summary>
    /// Apply security fixes to allow ws:// connections
    /// </summary>
    [ContextMenu("Apply Security Fix")]
    public void ApplySecurityFix()
    {
        Debug.Log("🔧 Applying WebSocket security fixes...");
        
        try
        {
            // Method 1: Set Unity's HTTP settings
            #if UNITY_EDITOR
            UnityEditor.PlayerSettings.insecureHttpOption = UnityEditor.InsecureHttpOption.AlwaysAllowed;
            Debug.Log("✅ Unity HTTP settings updated");
            #endif
            
            // Method 2: Set environment variables
            System.Environment.SetEnvironmentVariable("UNITY_DISABLE_GRAPHICS_API_VALIDATION", "1");
            System.Environment.SetEnvironmentVariable("UNITY_DISABLE_GRAPHICS_JOBS", "1");
            Debug.Log("✅ Environment variables set");
            
            // Method 3: Configure .NET security
            ConfigureDotNetSecurity();
            
            Debug.Log("✅ WebSocket security fixes applied successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to apply security fixes: {e.Message}");
        }
    }
    
    void ConfigureDotNetSecurity()
    {
        try
        {
            // Allow all SSL/TLS protocols
            System.Net.ServicePointManager.SecurityProtocol = 
                System.Net.SecurityProtocolType.Tls | 
                System.Net.SecurityProtocolType.Tls11 | 
                System.Net.SecurityProtocolType.Tls12;
            
            // Disable certificate validation for development
            System.Net.ServicePointManager.ServerCertificateValidationCallback = 
                (sender, certificate, chain, sslPolicyErrors) => true;
            
            Debug.Log("✅ .NET security configured");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ .NET security configuration failed: {e.Message}");
        }
    }
    
    /// <summary>
    /// Test if the security fix is working
    /// </summary>
    [ContextMenu("Test Security Fix")]
    public void TestSecurityFix()
    {
        Debug.Log("🧪 Testing WebSocket security fix...");
        
        // Check if we can create a WebSocket connection
        try
        {
            var testUrl = "ws://174.138.42.117:3002";
            Debug.Log($"🔗 Testing connection to {testUrl}");
            
            // This will be handled by the actual WebSocket client
            if (DoleroWebSocketClient.Instance != null)
            {
                DoleroWebSocketClient.Instance.Connect();
                Debug.Log("✅ Security fix test initiated");
            }
            else
            {
                Debug.LogError("❌ DoleroWebSocketClient not found!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Security fix test failed: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        // Clean up environment variables if needed
        try
        {
            System.Environment.SetEnvironmentVariable("UNITY_DISABLE_GRAPHICS_API_VALIDATION", null);
            System.Environment.SetEnvironmentVariable("UNITY_DISABLE_GRAPHICS_JOBS", null);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
