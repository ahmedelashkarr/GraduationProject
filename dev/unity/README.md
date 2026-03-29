# Smart Indoor Navigation — Unity 6 Mobile App

## Project Overview
A Unity 6 mobile app with 3 screens:
1. **Splash Screen** — Branding + Popular Destinations
2. **Main Screen** — Floor map + destination list + "Start AR Navigation"
3. **AR Navigation Screen** — AR Foundation camera + animated arrows + destination HUD

---

## Requirements
- Unity 6 (6000.x)
- AR Foundation 6.x (via Package Manager)
- ARCore XR Plugin 6.x (Android) and/or ARKit XR Plugin 6.x (iOS)
- Android SDK / Xcode installed

---

## Setup Instructions

### 1. Create a New Unity 6 Project
- Open Unity Hub → New Project → "3D (URP)" template
- Name it `SmartIndoorNav`

### 2. Install Required Packages
Open **Window → Package Manager → Add package by name**:
```
com.unity.xr.arfoundation
com.unity.xr.arcore        ← Android
com.unity.xr.arkit         ← iOS
com.unity.inputsystem
```

### 3. Configure XR Settings
- Go to **Edit → Project Settings → XR Plug-in Management**
- Android tab: ✅ ARCore
- iOS tab: ✅ ARKit

### 4. Import Scripts
Copy all files from `Assets/Scripts/` into your Unity project's `Assets/Scripts/` folder.

### 5. Create Scenes
Create 3 scenes in `Assets/Scenes/`:
- `SplashScene`
- `MainScene`
- `ARScene`

Add all 3 to **File → Build Settings** in this order (index 0, 1, 2).

### 6. Build Each Scene
Follow the per-scene setup in `SCENE_SETUP.md`.

### 7. Player Settings (Android)
- **Edit → Project Settings → Player**
- Minimum API Level: Android 8.0 (API 26)
- Scripting Backend: IL2CPP
- Target Architectures: ARM64
- Camera Usage Description: "Used for AR navigation"

### 8. Player Settings (iOS)
- Camera Usage Description: "Used for AR indoor navigation"
- Minimum iOS Version: 14.0

---

## Project Structure
```
Assets/
  Scripts/
    AppManager.cs          ← Scene navigation singleton
    SplashScreenUI.cs      ← Splash screen logic
    MainScreenUI.cs        ← Floor map + destinations
    ARNavigationUI.cs      ← AR screen HUD controller
    ARArrowController.cs   ← Animated 3D arrows on floor
    DestinationData.cs     ← ScriptableObject: destination list
    FloorMapRenderer.cs    ← Draws route on mini-map
  Scenes/
    SplashScene.unity
    MainScene.unity
    ARScene.unity
  Prefabs/
    DestinationItem.prefab ← Reusable destination row UI
    ARArrow.prefab         ← Glowing arrow prefab
  Resources/
    Destinations.asset     ← DestinationData ScriptableObject
```
