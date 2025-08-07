# DOLERO Web2 Server - Connection Details

**Server Successfully Deployed!** 🚀

## 📊 Server Information
- **Server IP**: `174.138.42.117`
- **Location**: Digital Ocean VPS (NYC1)
- **Status**: ✅ Live and Running
- **Deployment Date**: August 7, 2025
- **Environment**: Production (Solana Devnet)

## 🔗 Connection URLs

### HTTP Endpoints
- **Base URL**: `http://174.138.42.117`
- **API Base**: `http://174.138.42.117/api`
- **Health Check**: `http://174.138.42.117/health`

### WebSocket Connection
- **WebSocket URL**: `ws://174.138.42.117`
- **Socket.IO Endpoint**: `ws://174.138.42.117/socket.io`

## 🎮 Unity Client Configuration

### C# Configuration
```csharp
// Server Configuration
string serverUrl = "http://174.138.42.117";
string apiUrl = "http://174.138.42.117/api";
string websocketUrl = "ws://174.138.42.117";

// API Endpoints
string healthEndpoint = "http://174.138.42.117/health";
string matchmakingEndpoint = "http://174.138.42.117/api/matchmaking";
string gameStateEndpoint = "http://174.138.42.117/api/game-state";
```

### WebSocket Configuration
```csharp
// WebSocket Connection Details
string wsHost = "174.138.42.117";
int wsPort = 80; // Default HTTP port
string wsPath = "/socket.io";
string fullWsUrl = "ws://174.138.42.117/socket.io";
```

## 🔌 WebSocket Connection Details

**For real-time game coordination:**
- **Protocol**: WebSocket
- **Host**: `174.138.42.117`
- **Port**: `80` (default HTTP port)
- **Path**: `/socket.io`
- **Full URL**: `ws://174.138.42.117/socket.io`

## 📊 Server Status

### Current Server Health
```json
{
  "status": "healthy",
  "timestamp": "2025-08-07T01:57:06.683Z",
  "service": "dolero-web2-server",
  "version": "1.0.0",
  "uptime": 579.329135144
}
```

### Server Specifications
- **CPU**: 1 vCPU
- **RAM**: 1GB
- **Storage**: 35GB SSD
- **OS**: Ubuntu 24.04.3 LTS
- **Process Manager**: PM2
- **Reverse Proxy**: Nginx

## 🎯 Available API Endpoints

### Core Endpoints
1. **Health Check**: `GET /health`
   - Returns server status and uptime
   - Use for connection testing

2. **Matchmaking**: `POST /api/matchmaking`
   - Handle player matchmaking requests
   - Real-time game coordination

3. **Game State**: `GET /api/game-state`
   - Retrieve current game state
   - Player positions and game data

4. **WebSocket Events**: Real-time game coordination
   - Player movements
   - Game state updates
   - Real-time communication

### Example API Calls
```csharp
// Health check
UnityWebRequest healthRequest = UnityWebRequest.Get("http://174.138.42.117/health");
yield return healthRequest.SendWebRequest();

// Matchmaking request
WWWForm form = new WWWForm();
form.AddField("playerId", playerId);
UnityWebRequest matchRequest = UnityWebRequest.Post("http://174.138.42.117/api/matchmaking", form);
yield return matchRequest.SendWebRequest();
```

## 🔒 Security Configuration

- **CORS**: Configured to accept connections from any origin (`*`)
- **Rate Limiting**: 100 requests per 15 minutes
- **Environment**: Production (devnet for Solana integration)
- **Firewall**: Configured for HTTP/HTTPS traffic
- **SSL**: Ready for SSL certificate installation

## 🧪 Testing Commands

### Server Health Test
```bash
# Health check
curl http://174.138.42.117/health

# Expected response:
{"status":"healthy","service":"dolero-web2-server","version":"1.0.0"}
```

### WebSocket Test
```javascript
// Browser console test
const socket = io('ws://174.138.42.117');
socket.on('connect', () => {
    console.log('Connected to server!');
});
```

## 📱 Unity Integration Guide

### 1. HTTP Requests
```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ServerConnection : MonoBehaviour
{
    private string serverUrl = "http://174.138.42.117";
    
    IEnumerator TestConnection()
    {
        UnityWebRequest request = UnityWebRequest.Get(serverUrl + "/health");
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Server is online: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Connection failed: " + request.error);
        }
    }
}
```

### 2. WebSocket Connection
```csharp
// Using NativeWebSocket or similar library
using NativeWebSocket;

public class WebSocketManager : MonoBehaviour
{
    private WebSocket websocket;
    private string wsUrl = "ws://174.138.42.117/socket.io";
    
    async void ConnectToServer()
    {
        websocket = new WebSocket(wsUrl);
        
        websocket.OnOpen += () => {
            Debug.Log("Connected to server!");
        };
        
        websocket.OnMessage += (bytes) => {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received: " + message);
        };
        
        await websocket.Connect();
    }
}
```

### 3. Error Handling
```csharp
public class ConnectionManager : MonoBehaviour
{
    private bool isConnected = false;
    private float reconnectDelay = 5f;
    
    void Start()
    {
        StartCoroutine(ConnectToServer());
    }
    
    IEnumerator ConnectToServer()
    {
        while (!isConnected)
        {
            yield return StartCoroutine(TestConnection());
            
            if (!isConnected)
            {
                Debug.Log("Connection failed, retrying in " + reconnectDelay + " seconds...");
                yield return new WaitForSeconds(reconnectDelay);
            }
        }
    }
}
```

## 🚀 Deployment Information

### Server Infrastructure
- **Provider**: Digital Ocean
- **Droplet Type**: Basic ($6/month)
- **Region**: NYC1 (New York)
- **Process Manager**: PM2 (auto-restart on crash)
- **Reverse Proxy**: Nginx
- **Logs**: `/home/dolero/dolero-web2/logs/`

### Monitoring
- **PM2 Status**: `pm2 status`
- **Nginx Status**: `sudo systemctl status nginx`
- **System Resources**: `htop`
- **Logs**: `pm2 logs dolero-web2`

## 🔧 Maintenance Commands

### Server Management
```bash
# Restart application
pm2 restart dolero-web2

# View logs
pm2 logs dolero-web2

# Monitor resources
pm2 monit

# Update application
cd /home/dolero/dolero-web2
git pull origin master
npm ci --only=production
pm2 restart dolero-web2
```

### System Monitoring
```bash
# Check system resources
htop
free -h
df -h

# Check network
netstat -tlnp | grep :80
netstat -tlnp | grep :3000
```

## 📞 Support Information

### Server Details
- **IP Address**: `174.138.42.117`
- **SSH Access**: Available for maintenance
- **Uptime**: 24/7 with auto-restart
- **Backup**: Git repository backup

### Contact
- **Server Admin**: Available for technical support
- **Emergency**: PM2 auto-restart on crashes
- **Monitoring**: Real-time resource monitoring

## 🎯 Ready for Production

✅ **Server**: Live and running  
✅ **API**: All endpoints functional  
✅ **WebSocket**: Real-time communication ready  
✅ **Security**: Firewall and rate limiting configured  
✅ **Monitoring**: PM2 and system monitoring active  

**Your Unity client can now connect to the DOLERO Web2 server at `174.138.42.117`!** 🚀

---

**Last Updated**: August 7, 2025  
**Server Status**: ✅ Online  
**Version**: 1.0.0
