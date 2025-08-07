using UnityEngine;
using System.Collections;

/// <summary>
/// Simple ping test for WebSocket connection
/// </summary>
public class WebSocketPingTest : MonoBehaviour
{
    [Header("Ping Test Settings")]
    public float pingInterval = 5f; // Send ping every 5 seconds
    public int maxPings = 10; // Send 10 pings total
    public bool autoStart = true;
    
    private WebSocketTEST webSocketTest;
    private int pingCount = 0;
    private bool isTestRunning = false;
    
    void Start()
    {
        // Find the WebSocketTEST component
        webSocketTest = FindObjectOfType<WebSocketTEST>();
        
        if (webSocketTest == null)
        {
            Debug.LogError("❌ WebSocketTEST component not found! Please add it to a GameObject first.");
            return;
        }
        
        Debug.Log("✅ WebSocketPingTest started");
        Debug.Log($"✅ Ping interval: {pingInterval}s");
        Debug.Log($"✅ Max pings: {maxPings}");
        
        if (autoStart)
        {
            StartPingTest();
        }
    }
    
    /// <summary>
    /// Start the ping test
    /// </summary>
    [ContextMenu("Start Ping Test")]
    public void StartPingTest()
    {
        if (webSocketTest == null)
        {
            Debug.LogError("❌ WebSocketTEST not found!");
            return;
        }
        
        if (isTestRunning)
        {
            Debug.LogWarning("⚠️ Ping test already running!");
            return;
        }
        
        Debug.Log("🏓 Starting ping test...");
        isTestRunning = true;
        pingCount = 0;
        
        StartCoroutine(PingTestCoroutine());
    }
    
    /// <summary>
    /// Stop the ping test
    /// </summary>
    [ContextMenu("Stop Ping Test")]
    public void StopPingTest()
    {
        isTestRunning = false;
        Debug.Log("🛑 Ping test stopped");
    }
    
    /// <summary>
    /// Ping test coroutine
    /// </summary>
    private IEnumerator PingTestCoroutine()
    {
        Debug.Log("🏓 Waiting for WebSocket connection...");
        
        // Wait for connection to be established
        float timeout = 30f;
        float elapsed = 0f;
        
        while (!webSocketTest.IsConnectionEstablished && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            
            if (elapsed % 5f < 0.5f) // Log every 5 seconds
            {
                Debug.Log($"⏳ Waiting for connection... ({elapsed:F1}s elapsed)");
            }
        }
        
        if (!webSocketTest.IsConnectionEstablished)
        {
            Debug.LogError("❌ Connection not established within timeout!");
            isTestRunning = false;
            yield break;
        }
        
        Debug.Log("✅ Connection established! Starting ping sequence...");
        
        // Send pings at regular intervals
        while (isTestRunning && pingCount < maxPings)
        {
            pingCount++;
            Debug.Log($"🏓 Sending ping #{pingCount}/{maxPings}...");
            
            try
            {
                webSocketTest.SendPing();
                Debug.Log($"✅ Ping #{pingCount} sent successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to send ping #{pingCount}: {e.Message}");
            }
            
            // Wait for next ping
            yield return new WaitForSeconds(pingInterval);
        }
        
        if (pingCount >= maxPings)
        {
            Debug.Log($"✅ Ping test completed! Sent {pingCount} pings");
        }
        else
        {
            Debug.Log($"🛑 Ping test stopped after {pingCount} pings");
        }
        
        isTestRunning = false;
    }
    
    /// <summary>
    /// Get current test status
    /// </summary>
    [ContextMenu("Get Test Status")]
    public void GetTestStatus()
    {
        if (webSocketTest == null)
        {
            Debug.LogError("❌ WebSocketTEST not found!");
            return;
        }
        
        Debug.Log($"📊 Ping Test Status:");
        Debug.Log($"   - Test Running: {isTestRunning}");
        Debug.Log($"   - Pings Sent: {pingCount}/{maxPings}");
        Debug.Log($"   - WebSocket Connected: {webSocketTest.IsConnected}");
        Debug.Log($"   - Connection Established: {webSocketTest.IsConnectionEstablished}");
        Debug.Log($"   - Messages Received: {webSocketTest.MessageCount}");
        Debug.Log($"   - Last Error: {webSocketTest.LastError ?? "None"}");
    }
    
    void OnDestroy()
    {
        StopPingTest();
    }
}
