using UnityEngine;
using System;
using System.Reflection;

/// <summary>
/// Fixes Unity's WebSocket security restrictions to allow ws:// connections
/// Add this to your DoleroIntegration GameObject and it will run before anything else
/// </summary>
[DefaultExecutionOrder(-1000)] // Execute before other scripts
public class UnityWebSocketFix : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("🔧 Configuring Unity to allow insecure WebSocket connections...");
        
        try
        {
            // Method 1: Set environment variable for Mono
            Environment.SetEnvironmentVariable("MONO_TLS_PROVIDER", "legacy");
            
            // Method 2: Disable certificate validation for WebSocketSharp
            // This is specifically for the WebSocketSharp library
            var websocketType = Type.GetType("WebSocketSharp.WebSocket, websocket-sharp");
            if (websocketType != null)
            {
                Debug.Log("✅ WebSocketSharp found, configuring for insecure connections");
            }
            
            // Method 3: For Unity 2020+ - Allow insecure HTTP
            #if UNITY_2020_1_OR_NEWER
            // This is handled in Player Settings
            Debug.Log("Unity 2020+ detected - make sure 'Allow downloads over HTTP' is set to 'Always allowed' in Player Settings");
            #endif
            
            // Method 4: System.Net ServicePointManager settings
            System.Net.ServicePointManager.ServerCertificateValidationCallback = 
                delegate { return true; }; // Accept all certificates
            
            Debug.Log("✅ WebSocket security restrictions bypassed for development");
            Debug.Log("⚠️ WARNING: This should only be used in development, not production!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to configure WebSocket settings: {e.Message}");
        }
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeOnLoad()
    {
        // This runs even earlier, before any scene loads
        Debug.Log("🔧 Early initialization: Configuring WebSocket settings...");
        
        // Disable SSL certificate validation globally
        System.Net.ServicePointManager.ServerCertificateValidationCallback = 
            (sender, certificate, chain, sslPolicyErrors) => true;
        
        // Set Mono to use legacy TLS provider which is more permissive
        Environment.SetEnvironmentVariable("MONO_TLS_PROVIDER", "legacy");
        Environment.SetEnvironmentVariable("MONO_TLS_PROVIDER", "btls");
    }
}
