#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DoleroEditor
{
    [InitializeOnLoad]
    public class WebSocketSettingsFix
    {
        static WebSocketSettingsFix()
        {
            try
            {
                // Allow insecure HTTP in Unity Editor
                PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
                
                Debug.Log("✅ Unity configured to allow insecure WebSocket connections (ws://)");
                Debug.Log("PlayerSettings.insecureHttpOption set to AlwaysAllowed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Unity WebSocket settings: {e.Message}");
            }
        }
        
        [MenuItem("DOLERO/Fix WebSocket Settings")]
        static void FixSettings()
        {
            try
            {
                PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
                EditorUtility.DisplayDialog("Settings Fixed", 
                    "Unity is now configured to allow ws:// connections.\n\n" +
                    "The game should now connect to your WebSocket server.", 
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to set WebSocket settings: {e.Message}", 
                    "OK");
            }
        }
        
        [MenuItem("DOLERO/Test HTTP Connection")]
        static void TestConnection()
        {
            Debug.Log("🧪 Testing HTTP connection to server...");
            
            var testObject = new GameObject("TempConnectionTest");
            var testClient = testObject.AddComponent<SimpleDirectWebSocket>();
            
            // The component will automatically try to connect when it starts
        }
    }
}
#endif
