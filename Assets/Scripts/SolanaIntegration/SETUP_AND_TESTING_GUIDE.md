# DOLERO Unity Integration - Setup & Testing Guide

## 🚨 FIXING CURRENT ERRORS

The errors you're seeing are due to missing dependencies. Here's how to fix them:

### **Step 1: Install Solana Unity SDK**

1. **Open Unity Package Manager**:
   - Window → Package Manager
   - Click the **+** button in top-left
   - Select **"Add package from git URL"**

2. **Add Solana Unity SDK**:
   ```
   https://github.com/magicblock-labs/Solana.Unity-SDK.git
   ```

3. **Wait for installation** (this will take a few minutes)

### **Step 2: Install Required Dependencies**

The Solana SDK requires these dependencies. Add them via Package Manager:

1. **Newtonsoft.Json**:
   - In Package Manager, switch to **"Unity Registry"**
   - Search for **"Newtonsoft Json"**
   - Install **"com.unity.nuget.newtonsoft-json"**

2. **If Newtonsoft.Json isn't available in Unity Registry**:
   - Add this git URL:
   ```
   https://github.com/jilleJr/Newtonsoft.Json-for-Unity.git#upm
   ```

### **Step 3: Import Solana SDK Samples**

1. In Package Manager, find **"Solana SDK"**
2. Expand **"Samples"** section
3. Click **"Import"** for **"Simple Wallet"**

### **Step 4: Configure Project Settings**

1. **Edit → Project Settings**
2. **Player → Configuration**
3. Set **"Api Compatibility Level"** to **".NET Standard 2.1"**
4. **XR Settings → Initialize XR on Startup** = **Unchecked**

---

## 🛠️ SIMPLIFIED TESTING SETUP

I've created a `DoleroTestManager.cs` that allows you to test the integration without SDK dependencies:

### **Step 5: Add Test Manager to Scene**

1. **Create Empty GameObject**:
   - Right-click in Hierarchy → Create Empty
   - Name it "DoleroTestManager"

2. **Add DoleroTestManager Script**:
   - Add Component → Search "DoleroTestManager"
   - Assign your existing GameManager, HorizontalCardHolder, PlayerHealth

3. **Create Test UI** (or use existing UI):
   ```
   Canvas
   ├── TestServerButton (Button)
   ├── ServerStatusText (TextMeshPro)
   ├── WalletButton (Button) 
   ├── WalletStatusText (TextMeshPro)
   ├── StartGameButton (Button)
   └── GameStatusText (TextMeshPro)
   ```

### **Step 6: Quick Test (F-Keys)**
- **F1**: Test server connection
- **F2**: Simulate wallet connection  
- **F3**: Start game flow test
- **F4**: Complete integration test
- **F5**: Test relic effects

---

## 🧪 TESTING PHASES

### **Phase 1: Server Connection Test** ✅
```csharp
// Tests your Web2 server at 174.138.42.117
DoleroTestManager.Instance.TestServerConnection();
```
**Expected Result**: Green checkmark if server responds

### **Phase 2: Wallet Simulation** ✅  
```csharp
// Simulates wallet connection without Solana SDK
DoleroTestManager.Instance.SimulateWalletConnection();
```
**Expected Result**: Wallet address shown as connected

### **Phase 3: Existing System Integration** ✅
```csharp
// Tests your existing GameManager, cards, health system
DoleroTestManager.Instance.TestExistingCardSystem();
```
**Expected Result**: Console shows existing systems are accessible

### **Phase 4: Game Flow Simulation** ✅
```csharp
// Simulates complete game flow through all phases
DoleroTestManager.Instance.StartTestGameFlow();
```
**Expected Result**: 
- Phases: Relic → Cards → Position → Reveal → Betting → Final
- Your existing card system works
- Timer integrations work
- Health system responds

### **Phase 5: Card Swap Testing** ✅
```csharp
// Tests card swapping with your existing card system
DoleroTestManager.Instance.TestSwapSystem();
```
**Expected Result**: 
- Selected cards get new values
- Card visuals update
- Swap count decreases

---

## 🎯 WHAT TO TEST RIGHT NOW

### **1. IMMEDIATE TESTING (No SDK Required)**

1. **Add DoleroTestManager to scene**
2. **Press F4** for complete integration test
3. **Check console** for test results

**You Should See**:
```
✅ Server connection successful
✅ Wallet connected (simulated)  
✅ GameManager found and accessible
✅ Player deck found with X cards
✅ Card visual system accessible
✅ Player health system found
🎮 Starting test game flow...
```

### **2. MANUAL TESTING**

1. **Test Your Existing Card System**:
   - Select cards in your existing UI
   - Press F-keys to trigger tests
   - Watch console for integration feedback

2. **Test Server Connection**:
   - Click "Test Server" button
   - Should connect to `174.138.42.117`
   - Green status = success

3. **Test Game Flow**:
   - Click "Start Game Flow"
   - Watch phases progress automatically
   - See how it integrates with your systems

---

## 🔧 FIXING DEPENDENCY ERRORS

### **Temporary Fix (Until SDK Installs)**

1. **Comment Out Problem Lines**:
   In the main integration scripts, add `//` before these lines:
   ```csharp
   // using Newtonsoft.Json;
   // using Solana.Unity.SDK;
   // using Solana.Unity.Wallet;
   ```

2. **Use Test Manager Instead**:
   Use `DoleroTestManager` for all testing until SDK is properly installed

### **Complete Fix (After SDK Installation)**

1. **Verify SDK Installation**:
   - Package Manager shows "Solana SDK" 
   - No more red errors in console

2. **Uncomment Integration Scripts**:
   - Remove `//` from using statements
   - Scripts should compile without errors

3. **Configure Real Wallet**:
   - Follow Solana SDK wallet setup
   - Connect to real Solana wallet

---

## 🚀 NEXT STEPS AFTER FIXING ERRORS

### **1. SDK Installation Complete** ✅
- No more compilation errors
- Solana SDK visible in Package Manager

### **2. Basic Integration Test** ✅  
```csharp
// Real Solana wallet connection
await Web3.Instance.LoginWalletAdapter();
```

### **3. Smart Contract Integration** ✅
```csharp
// Real blockchain transactions
await DoleroSolanaManager.Instance.InitializeGame("micro", 0.01m);
```

### **4. Full Game Test** ✅
- Connect real wallet
- Test with real server
- Execute actual smart contract calls
- Full game flow with blockchain

---

## 📋 TESTING CHECKLIST

### **Before SDK Installation**:
- [ ] DoleroTestManager added to scene
- [ ] F1: Server connection works  
- [ ] F2: Wallet simulation works
- [ ] F3: Game flow simulation works
- [ ] F4: Complete test passes
- [ ] Console shows no integration errors

### **After SDK Installation**:
- [ ] No compilation errors
- [ ] Real wallet connection works
- [ ] Real server API calls work  
- [ ] Smart contract interaction works
- [ ] Full game flow with blockchain

### **Final Integration**:
- [ ] Cards draw from blockchain
- [ ] Swaps execute on blockchain
- [ ] Betting updates blockchain
- [ ] Reveals come from blockchain
- [ ] Hearts sync with blockchain
- [ ] Game state persists

---

## 🆘 TROUBLESHOOTING

### **"Newtonsoft.Json not found"**
1. Install from Package Manager (Unity Registry)
2. Or use git URL: `https://github.com/jilleJr/Newtonsoft.Json-for-Unity.git#upm`

### **"Solana namespace not found"**  
1. Install Solana SDK: `https://github.com/magicblock-labs/Solana.Unity-SDK.git`
2. Wait for import to complete
3. Import samples

### **"Server connection failed"**
1. Check server is running: `curl http://174.138.42.117/health`
2. Check Unity firewall permissions
3. Try different network

### **"Integration test failed"**
1. Assign GameManager in inspector
2. Assign PlayerHealth in inspector  
3. Assign HorizontalCardHolder in inspector
4. Check console for specific errors

---

The test manager allows you to verify the integration works with your existing systems before the full Solana SDK is installed!

