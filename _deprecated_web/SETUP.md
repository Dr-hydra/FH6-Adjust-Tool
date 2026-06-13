# TuneLab — Android Build Guide

## Prerequisites
- Node.js 18+ installed
- Android Studio installed (for building the APK)
- Java 17+ (comes with Android Studio)
- Your S25 Ultra with USB debugging enabled

---

## Step 1: Project Setup

```bash
# Clone or create the project folder
cd tunelab-android

# Install dependencies
npm install

# Copy your ForzaTuner.jsx into src/
cp ForzaTuner.jsx src/App.jsx
```

---

## Step 2: Initialize Capacitor

```bash
# Init Capacitor (only needed once)
npx cap init TuneLab com.tunelab.forzah6 --web-dir dist

# Add Android platform
npx cap add android
```

---

## Step 3: Copy Android resource files

After `npx cap add android`, copy the resource files:

```bash
# Manifest
cp android-resources/AndroidManifest.xml android/app/src/main/AndroidManifest.xml

# Styles
cp android-resources/styles.xml android/app/src/main/res/values/styles.xml

# Colors
cp android-resources/colors.xml android/app/src/main/res/values/colors.xml

# MainActivity
cp android-resources/MainActivity.java \
   android/app/src/main/java/com/tunelab/forzah6/MainActivity.java
```

---

## Step 4: Build and sync

```bash
# Build React app and sync to Android
npm run sync
# This runs: vite build && npx cap sync android
```

---

## Step 5: Open in Android Studio

```bash
npm run open
# Or: npx cap open android
```

In Android Studio:
1. Wait for Gradle sync to finish
2. Plug in your S25 Ultra via USB
3. Run → Run 'app' — it installs directly to your phone

---

## Step 6: Generate a signed APK for Play Store

### 6a. Create your keystore (ONE TIME — back this file up forever)
```bash
keytool -genkey -v \
  -keystore tunelab.keystore \
  -alias tunelab \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```
**⚠ WARNING: If you lose this keystore you CANNOT update the app on Play Store. Back it up to Google Drive, iCloud, everywhere.**

### 6b. Add keystore config to capacitor.config.json
Fill in the passwords in `capacitor.config.json`:
```json
"buildOptions": {
  "keystorePath": "tunelab.keystore",
  "keystoreAlias": "tunelab",
  "keystorePassword": "YOUR_PASSWORD",
  "keystoreAliasPassword": "YOUR_PASSWORD"
}
```

### 6c. Build release AAB (preferred over APK for Play Store)
In Android Studio:
- Build → Generate Signed Bundle/APK
- Choose Android App Bundle
- Select your keystore
- Build Type: release

Output: `android/app/release/app-release.aab`

---

## Step 7: Play Store submission

1. Go to https://play.google.com/console
2. Create new app: "TuneLab"
3. Upload your AAB
4. Fill out:
   - Store listing (use description below)
   - Screenshots (take from your S25 Ultra)
   - Content rating (Everyone)
   - Data safety (no data collected)
   - Privacy policy URL: https://YOUR_GITHUB.github.io/tunelab/privacy
5. Submit for review (typically 3–7 days for new developer account)

---

## Play Store Description

### Short description (80 chars)
```
AI-powered Forza Horizon 6 tuning calculator. Free, offline, no ads.
```

### Full description
```
TuneLab is the free Forza Horizon 6 tuning assistant built for FH6's
Japanese setting — from the Shuto Expressway to Mt. Haruna's hairpins.

INSTANT OFFLINE TUNES
Every tune calculates in under 100ms using real suspension physics —
spring natural frequencies, traction circle brake bias, geometric gear
ratios. No internet required. No account needed. Always free.

AI ENHANCEMENT (OPTIONAL)
Connect your own Gemini (free), Grok, OpenAI, or Claude API key for
car-specific analysis and smarter wizard diagnoses. Your key stays on
your device — we never see it.

TUNE MODES
🏁 Race · 💨 Drift · ⚡ Drag · 🪨 Rally · ⛰ Touge · 🌃 Wangan · 🔧 General · 🌧 Rain

TUNING WIZARD
After your test drive, describe any handling issue — understeer,
oversteer, braking instability, twitchiness — and get specific fixes
for your exact car and setup.

FEATURES
• 70+ cars with auto-filled stock specs
• Engine swap support (2JZ, RB26, LS, and more)
• Aspiration-aware tuning (NA, turbo, supercharged, electric)
• Class scaling (D through X)
• Touge mode: softened ARBs for mountain pass compliance
• Wangan mode: long gearing for Shuto Expressway top speed
• Imperial and Metric units with automatic value conversion
• Save, export, and share your tunes
• Works on phone, tablet, and Samsung DeX

NO ADS. NO SUBSCRIPTIONS. EVER.
Support TuneLab on Ko-fi if it helps your racing.

Not affiliated with Xbox, Turn 10, or Playground Games.
Forza Horizon is a trademark of Microsoft Corporation.
```

---

## Discord & Ko-fi Setup

### Discord Server
1. Create server at discord.com
2. Name: TuneLab Community
3. Channels: #tune-sharing #bug-reports #feature-requests #fh6-news
4. Get invite link → paste into `AboutScreen` in App.jsx

### Ko-fi
1. Create page at ko-fi.com
2. Set your goal description: "Keeping TuneLab free and the API running"
3. Get your page URL → paste into `AboutScreen` in App.jsx

Both take about 5 minutes. Do them before launch so the links work day one.

---

## 120Hz Verification

On your S25 Ultra after installing:
1. Settings → Developer Options → Show refresh rate
2. Open TuneLab — should show 120Hz while animating
3. Scroll the tune modes row — should be butter smooth
4. Generate a tune — the loading dots animation should run at full refresh rate

---

## Pre-launch test checklist

- [ ] All 5 tabs navigate correctly
- [ ] Units sheet opens and closes
- [ ] Car auto-fill works (select Nissan GT-R)
- [ ] Generate produces a full tune on each class (D, A, S1, X)
- [ ] Quick Adjust nudges values
- [ ] Reset sheet shows 3 options
- [ ] Save and load a tune
- [ ] Share card downloads
- [ ] Wizard diagnoses understeer offline (no AI key)
- [ ] Wizard diagnoses oversteer with Gemini (if you have a key)
- [ ] Back button closes overlay → closes overlay → navigates tab
- [ ] Works in landscape
- [ ] Works in DeX (connect to monitor)
- [ ] Works with large font size (Accessibility → Font Size → Largest)
- [ ] No crash on any flow
