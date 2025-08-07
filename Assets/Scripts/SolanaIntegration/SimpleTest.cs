using UnityEngine;

/// <summary>
/// Simple test to verify HTTP connection works
/// </summary>
public class SimpleTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🧪 SimpleTest: Starting HTTP connection test...");
        
        // Create a test GameObject with SimpleDirectWebSocket
        var testObject = new GameObject("HTTPTest");
        testObject.AddComponent<SimpleDirectWebSocket>();
        testObject.AddComponent<ConnectionTest>();
        
        Debug.Log("✅ SimpleTest: Components added successfully!");
    }
}
