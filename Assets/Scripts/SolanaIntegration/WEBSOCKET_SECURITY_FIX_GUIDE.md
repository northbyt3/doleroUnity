# 🔧 WebSocket Security Fix Guide

## 🚨 Problem
Unity is blocking `ws://` connections with the error: **"The interface does not use a secure connection"**

## ✅ Solution

### Step 1: Add Security Fix Component
1. **Create a GameObject** named `WebSocketSecurityFix`
2. **Add this component**: `WebSocketSecurityFix`
3. **Play the scene** - it will automatically apply the security fix

### Step 2: Update Unity Settings
1. **Open Unity Editor**
2. **Go to**: Edit → Project Settings → Player
3. **Find**: "Other Settings" → "Insecure HTTP Option"
4. **Set to**: "Always Allowed"
5. **Save the project**

### Step 3: Test the Connection
1. **Play the scene**
2. **Check Console** for these messages:
   ```
   🔧 Applying WebSocket security fixes...
   ✅ Unity HTTP settings updated
   ✅ Environment variables set
   ✅ .NET security configured
   ✅ WebSocket security fixes applied successfully!
   ```

## 🔧 Manual Fix Options

### Option 1: Use Context Menu
- **Right-click** on the `WebSocketSecurityFix` component
- **Select**: "Apply Security Fix"
- **Then**: "Test Security Fix"

### Option 2: Code Fix
Add this to your main game script:
```csharp
void Start()
{
    // Allow insecure connections
    System.Net.ServicePointManager.ServerCertificateValidationCallback = 
        (sender, certificate, chain, sslPolicyErrors) => true;
}
```

## 🎯 Expected Results

After applying the fix, you should see:
```
🔗 Connecting to WebSocket server at ws://174.138.42.117:3002
✅ WebSocket connected!
```

Instead of:
```
❌ WebSocket connection failed: The interface does not use a secure connection
```

## 🐛 If Still Not Working

### Check 1: Unity Version
- Make sure you're using Unity 2021.3 or later
- Check API Compatibility Level is set to ".NET Standard 2.1"

### Check 2: Server Status
- Verify your server at `174.138.42.117:3002` is running
- Test with: `telnet 174.138.42.117 3002`

### Check 3: Firewall
- Ensure port 3002 is open on your server
- Check if any antivirus is blocking the connection

## 🚀 Quick Test

1. **Add the `WebSocketSecurityFix` component**
2. **Play the scene**
3. **Check Console** for security fix messages
4. **Look for**: "✅ WebSocket connected!" message

The WebSocket should now connect successfully to your server! 🎉
