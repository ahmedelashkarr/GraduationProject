# SCENE SETUP GUIDE

## SplashScene

### Hierarchy:
```
Main Camera
Canvas (Screen Space - Overlay)
  └─ SplashRoot
       ├─ Background          (Image, full-screen, color #1565C0)
       ├─ GradientOverlay     (Image, full-screen gradient sprite or CanvasGroup)
       ├─ HeroSection
       │    ├─ PinIcon        (Image — use a location-pin sprite)
       │    └─ AppTitle       (TextMeshPro — "Smart Indoor Navigation")
       ├─ DotsIndicator
       │    ├─ Dot1           (Image, circle, active)
       │    ├─ Dot2           (Image, circle)
       │    └─ Dot3           (Image, circle)
       └─ BottomSheet         (Image, white, rounded top corners)
            ├─ SectionTitle   (TextMeshPro — "Popular Destinations")
            └─ DestinationsList (Vertical Layout Group)
                 ├─ DestItem_ComputerLab   (DestinationItem prefab)
                 ├─ DestItem_Room305       (DestinationItem prefab)
                 ├─ DestItem_Cafeteria     (DestinationItem prefab)
                 └─ DestItem_Exit          (DestinationItem prefab)
EventSystem

Components:
- Canvas: Add SplashScreenUI.cs
- Set Canvas Scaler: Scale With Screen Size, 1080x1920, Match 0.5
```

---

## MainScene

### Hierarchy:
```
Main Camera
Canvas (Screen Space - Overlay)
  └─ MainRoot
       ├─ TopBar             (Image, blue #1565C0)
       │    ├─ LocationIcon  (Image)
       │    ├─ SearchBar     (Image, white, rounded)
       │    │    ├─ SearchIcon    (Image)
       │    │    ├─ PlaceholderText (TMP)
       │    │    └─ MicButton     (Button + Image)
       │    └─ SearchInputField   (TMP_InputField, transparent, over SearchBar)
       ├─ FloorBar
       │    ├─ BackButton    (Button)
       │    ├─ FloorTitle    (TMP — "Floor 2")
       │    ├─ MenuButton    (Button)
       │    └─ LockButton    (Button)
       ├─ MapContainer       (RawImage — render texture from MapCamera)
       │    └─ YouBadge      (Image + TMP "You")
       ├─ ScrollView         (Scroll Rect)
       │    └─ Content       (Vertical Layout Group)
       │         ├─ SectionTitle1
       │         ├─ DestItem_ComputerLab
       │         ├─ SectionTitle2
       │         ├─ DestItem_ComputerLab2
       │         ├─ DestItem_Cafeteria
       │         └─ DestItem_Exit
       └─ StartARButton      (Button, blue, full-width)

MapCamera (separate Camera, Culling Mask: "MapLayer", renders to RenderTexture)
  └─ FloorPlanRoot  (3D floor plan meshes on layer "MapLayer")
       ├─ FloorMesh
       ├─ RoomMeshes...
       └─ RouteRenderer (LineRenderer)

EventSystem

Components:
- Canvas root: Add MainScreenUI.cs
```

---

## ARScene

### Hierarchy:
```
XR Origin (AR)                    ← Added by AR Foundation
  └─ Camera Offset
       └─ Main Camera             ← Has AR Camera Manager + AR Camera Background
AR Session                        ← Required for AR Foundation
AR Plane Manager (on XR Origin)   ← Detects floor planes
AR Raycast Manager (on XR Origin) ← For placing arrows on floor

ArrowSpawner                      ← Empty GO, attach ARArrowController.cs

Canvas (Screen Space - Overlay)
  └─ ARHUDRoot
       ├─ TopBar
       │    ├─ BackButton
       │    └─ SearchBar
       ├─ ControlsRow
       │    ├─ StopButton         (Button, red dot)
       │    ├─ RecenterButton     (Button)
       │    └─ MapButton          (Button)
       └─ DestinationCard         (Image, white, rounded, bottom)
            ├─ DestIcon           (Image, blue bg, arrow up)
            ├─ DestInfo
            │    ├─ DestName      (TMP — "Computer Lab")
            │    └─ DestMeta      (TMP — "15 meters · 30 sec")
            └─ ChevronRight       (Image / TMP "›")

EventSystem

Components:
- ARHUDRoot Canvas: Add ARNavigationUI.cs
- ArrowSpawner: Add ARArrowController.cs
```

---

## DestinationItem Prefab Structure:
```
DestinationItem (Horizontal Layout Group, Button)
  ├─ IconContainer  (Image, rounded square, colored bg)
  │    └─ Icon      (Image — assign sprite per destination)
  ├─ InfoContainer  (Vertical Layout Group)
  │    ├─ NameText  (TMP)
  │    └─ MetaText  (TMP, gray)
  └─ ChevronRight   (TMP "›" or Image)
```

## ARArrow Prefab Structure:
```
ARArrow
  ├─ ArrowMesh     (MeshFilter + MeshRenderer, custom V-shape mesh)
  │    └─ Material: ArrowGlowMaterial (URP/Lit, emission enabled, cyan #00E5FF)
  └─ GlowLight     (Point Light, cyan, intensity animated)
```
