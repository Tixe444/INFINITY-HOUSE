# INFINITY HOUSE v6.0 — Build Process

**Security Layer Documentation**

---

## 🔑 Signing Key Setup (One-Time)

### Option 1: Local Secret File (Recommended for Solo Dev)

```bash
# Generate 32-character random key
openssl rand -hex 16 > .ih_secret

# Add to .gitignore (CRITICAL!)
echo ".ih_secret" >> .gitignore
git add .gitignore
git commit -m "Add .ih_secret to gitignore"
```

### Option 2: Environment Variable (Recommended for Teams/CI)

```bash
# Linux/Mac
export INFINITY_HOUSE_KEY="your-secret-key-here"

# Windows (PowerShell)
$env:INFINITY_HOUSE_KEY="your-secret-key-here"

# Add to CI/CD secrets (GitHub Actions, GitLab CI, etc.)
```

**⚠️ NEVER commit `.ih_secret` or expose `INFINITY_HOUSE_KEY` in logs!**

---

## 🏗️ Build Steps (3 Steps)

### Step 1: Generate Build Signature

```
Unity Editor → Menu Bar → Infinity House → Generate Build Signature
```

**What happens:**
- Retrieves git commit hash (`git rev-parse HEAD`)
- Generates Unix timestamp
- Computes HMAC-SHA256 signature: `HMAC(commitHash|timestamp|IH2025|TixeStudio, key)`
- Embeds signature in 3 locations:
  1. `AssemblyInfo.cs` (metadata attributes)
  2. `IH_BuildSignature.asset` (ScriptableObject in Resources/)
  3. Addressables catalog (if installed)

**Output:**
```
[BuildTool] ✅ Signature generated: a3f82b9c4d1e5f6a...
[BuildTool] Commit: 665786d | Timestamp: 1730678400
```

### Step 2: Build Release

```
File → Build Settings → Platform: Android/iOS/PC
Configuration Profile: Release
Scripting Backend: IL2CPP
Strip Engine Code: ON
Build
```

### Step 3: Automatic Verification (First Launch)

**Runtime behavior (Release builds only):**
- `IH_SignatureVerifier` loads all 3 signatures on first launch
- Compares for consistency (at least 2 must match)
- **PASS**: Silent success, game continues
- **FAIL**: Logs warning + sends telemetry, game continues (fail-soft)

**No blocking, no prompts, no user interaction.**

---

## 🔄 Key Rotation (Annual or on Compromise)

```bash
# Backup old key
mv .ih_secret .ih_secret_backup_$(date +%Y%m%d)

# Generate new key
openssl rand -hex 16 > .ih_secret

# Rebuild all releases with new key
Unity → Infinity House → Generate Build Signature
File → Build Settings → Build
```

**For teams:**
```bash
# Update CI/CD environment variable
# GitHub: Settings → Secrets → INFINITY_HOUSE_KEY → Update
# GitLab: Settings → CI/CD → Variables → INFINITY_HOUSE_KEY → Edit
```

---

## 🧪 Verification Test

### Test 1: Clean Build (Expected: ✅ Pass)

```bash
# Fresh clone
git clone <repo>
cd INFINITY-HOUSE

# Generate key
openssl rand -hex 16 > .ih_secret

# Build
Unity → Infinity House → Generate Build Signature
File → Build Settings → Build

# Run on device
# Expected: No warnings, game runs normally
```

### Test 2: Tampered Build (Expected: ⚠️ Fail-Soft)

```bash
# Build with valid signature
Unity → Generate Build Signature → Build

# Manually tamper with signature
# Edit: Assets/Resources/IH_BuildSignature.asset
# Change signature field to: "tampered_signature_000000..."

# Run on device
# Expected: Warning logged, telemetry sent, game continues
```

### Test 3: Missing Key (Expected: ❌ Build Fails)

```bash
# Remove key
rm .ih_secret
unset INFINITY_HOUSE_KEY

# Try to generate signature
Unity → Infinity House → Generate Build Signature

# Expected:
# [BuildTool] ❌ No signing key found! Set INFINITY_HOUSE_KEY env var or create .ih_secret file.
```

---

## 📊 Security Layer Architecture

### Signature Generation

```
Input:
  commitHash = git rev-parse HEAD
  timestamp = Unix timestamp
  projectID = "IH2025" (const)
  producerID = "TixeStudio" (const)

Payload:
  "{commitHash}|{timestamp}|{projectID}|{producerID}"

Key:
  From env var INFINITY_HOUSE_KEY or .ih_secret file

Signature:
  HMAC-SHA256(payload, key) → 64-char hex string
```

### Embedding Locations

**1. AssemblyInfo.cs** (Code-level metadata)
```csharp
[assembly: AssemblyMetadata("IH_Signature", "a3f82b9c...")]
[assembly: AssemblyMetadata("IH_Commit", "665786d")]
[assembly: AssemblyMetadata("IH_Timestamp", "1730678400")]
```

**2. ScriptableObject** (Runtime data)
```
Assets/Resources/IH_BuildSignature.asset
- signature: "a3f82b9c..."
- commitHash: "665786d"
- buildTimestamp: "1730678400"
- projectID: "IH2025"
- producerID: "TixeStudio"
```

**3. Addressables Catalog** (Asset metadata)
```json
{
  "metadata": {
    "IH_Signature": "a3f82b9c..."
  }
}
```

### Runtime Verification (Release Only)

```
On Awake (Release builds):
  1. Load signature from AssemblyInfo → sig1
  2. Load signature from ScriptableObject → sig2
  3. Load signature from Addressables → sig3

  4. Validate consistency:
     - At least 2 signatures must match
     - All must be non-null

  5. PASS → Silent success
     FAIL → Log warning + send telemetry, continue (fail-soft)
```

---

## 🎯 Design Principles

1. **Invisible to Developers**
   - No workflow changes
   - Single menu click before build
   - No prompts, no UI

2. **Fail-Soft**
   - Never blocks gameplay
   - Mismatch = warning + telemetry, not crash
   - Protects IP without user impact

3. **Multi-Layer**
   - 3 signature locations for redundancy
   - Code + data + assets verification
   - Harder to tamper all locations

4. **Traceable**
   - Commit hash embedded → trace to source
   - Timestamp embedded → trace to build date
   - Telemetry on mismatch → detect piracy

---

## 🛡️ Legal Protections (Supplement to Technical)

1. **Copyright Headers**: Add to all `.cs` files
2. **License File**: `LICENSE.md` in repo root
3. **Repository Access Control**: Private repo, limited access
4. **CI/CD Signing**: Build server has exclusive key access
5. **Obfuscation**: Consider IL2CPP + code stripping (enabled by default)

**Technical signature ≠ legal protection**. Combine both for maximum effect.

---

## ❓ FAQ

**Q: What if I lose the `.ih_secret` file?**
A: Generate a new key and rebuild. Old builds will show signature mismatch (logged, but still work).

**Q: Can someone bypass this by patching the verifier?**
A: Yes, but it raises the effort bar. Combine with obfuscation + legal protections.

**Q: Does this affect Editor workflow?**
A: No. Verification only runs in Release builds (`#if !UNITY_EDITOR && !DEBUG`).

**Q: What if git is not installed?**
A: Build signature will use `commitHash = "unknown"`. Still secure, just less traceable.

**Q: How does telemetry work?**
A: Placeholder in `IH_SignatureVerifier.cs`. Integrate with your analytics service (Unity Analytics, Firebase, etc.).

---

## ✅ Checklist for First Build

- [ ] Generate signing key: `openssl rand -hex 16 > .ih_secret`
- [ ] Add to .gitignore: `echo ".ih_secret" >> .gitignore`
- [ ] Verify key file exists: `cat .ih_secret` (should show 32 hex chars)
- [ ] Generate signature: Unity → Infinity House → Generate Build Signature
- [ ] Check console: Look for `✅ Signature generated: a3f82b9c...`
- [ ] Verify files created:
  - [ ] `Assets/Scripts/AssemblyInfo.cs`
  - [ ] `Assets/Resources/IH_BuildSignature.asset`
- [ ] Build release: File → Build Settings → Build
- [ ] Test on device: Launch, check for no warnings
- [ ] Archive key securely (backup to encrypted storage)

---

**INFINITY HOUSE v6.0**
**Security Layer: Active**
**Fail-Soft: Enabled**
**Invisible: Yes**

🚀 Ready for production release.
