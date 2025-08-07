# 🚀 Quick Setup Guide - DOLERO Unity Integration

## ✅ Current Status
- **HTTP Connection**: ✅ Working (`Connected to Web2 server: http://174.138.42.117`)
- **WebSocket Connection**: ❌ Blocked by Unity security (use HTTP polling instead)

## 🎯 Quick Fix - Use HTTP Polling

### Step 1: Add the Working Component
1. **Create an empty GameObject** in your scene
2. **Name it**: `DoleroConnection`
3. **Add this component**: `SimpleDirectWebSocket`

### Step 2: Test the Connection
1. **Add this component**: `ConnectionTest` (to the same GameObject)
2. **Play the scene**
3. **Check the Console** - you should see:
   ```
   📡 Connecting to 174.138.42.117:3001 using HTTP polling...
   ✅ Connected via HTTP polling!
   ```

### Step 3: Verify Server Communication
The `ConnectionTest` component will automatically test:
- ✅ HTTP health check
- ✅ API connection
- ✅ Message sending

## 🔧 What's Fixed

### Disabled Problematic Components:
- ❌ `DoleroWebSocketClient` - WebSocket security issues
- ❌ `DirectWebSocket` - Unity restrictions
- ❌ `SimpleWebSocketClient` - Fallback issues

### Enabled Working Components:
- ✅ `SimpleDirectWebSocket` - HTTP polling (works!)
- ✅ `ConnectionTest` - Verify connectivity

## 🎮 Next Steps

### 1. Test Basic Connection
```csharp
// Add this to any GameObject
var testObject = new GameObject("TestConnection");
testObject.AddComponent<ConnectionTest>();
```

### 2. Use HTTP Polling for Game
```csharp
// Add this to your main game GameObject
var gameObject = new GameObject("DoleroGame");
gameObject.AddComponent<SimpleDirectWebSocket>();
```

### 3. Send Game Messages
```csharp
// Once connected, you can send messages:
SimpleDirectWebSocket.Instance.SendMessage("joinGame", new { gameId = "test123" });
SimpleDirectWebSocket.Instance.SendMessage("selectRelic", new { relicId = 1 });
```

## 🐛 Troubleshooting

### If HTTP Connection Fails:
1. **Check your server**: `http://174.138.42.117:3001/health`
2. **Verify firewall**: Port 3001 should be open
3. **Check server logs**: Look for connection attempts

### If You Still See WebSocket Errors:
- The old WebSocket components are now disabled
- Only `SimpleDirectWebSocket` should be active
- Check that you're not using multiple connection components

## 🎉 Success Indicators

You'll know it's working when you see:
```
📡 Connecting to 174.138.42.117:3001 using HTTP polling...
✅ Connected via HTTP polling!
📨 Polled messages: [server responses]
📤 Sent message: joinGame
```

## 📞 Next: Game Integration

Once the connection is working, we can integrate with your existing game components:
- `GameManager`
- `HorizontalCardHolder` 
- `PlayerHealth`
- `VisualCardsHandler`

The HTTP polling approach will work perfectly for real-time game communication! 🚀
