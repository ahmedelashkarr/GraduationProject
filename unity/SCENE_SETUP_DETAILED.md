# DETAILED SCENE SETUP — Smart Indoor Navigation
# Unity 6 · uGUI · AR Foundation 6

=============================================================
## GLOBAL SETTINGS (do once)
=============================================================

### Canvas Scaler (on every Canvas):
  UI Scale Mode      : Scale With Screen Size
  Reference Resolution: 1080 x 1920
  Screen Match Mode  : Match Width Or Height
  Match              : 0.5

### TextMeshPro Font:
  Import TMP Essentials (Window → TextMeshPro → Import TMP Essential Resources)
  Recommended font: "Nunito" from Google Fonts → import as TMP Font Asset

=============================================================
## SCENE 0 — SplashScene
=============================================================

Hierarchy:
───────────────────────────────────────────
Main Camera
  Clear Flags: Solid Color | Background: #1565C0
EventSystem
AppManagerBootstrap          ← Empty GO with AppManager.cs
Canvas
  └─ SplashRoot              (RectTransform: stretch full)
       ├─ Background
       │    Component: Image
       │    Color: #1565C0
       │    RectTransform: Stretch full (Left/Right/Top/Bottom = 0)
       │
       ├─ HeroSection
       │    RectTransform: Anchor top-center, Pivot 0.5/0.5
       │    Pos Y: -280,  Height: 380
       │    ─────────────────────────────
       │    ├─ PinContainer     (empty, 90×110)
       │    │    ├─ PinCircle   Image, sprite=circle, color=#FFFFFF33, size=90×90
       │    │    ├─ ARBadge     Image, sprite=circle, color=white, size=52×52
       │    │    │    └─ ARText TMP "AR", size=16, bold, color=#1565C0
       │    │    └─ PinTip      Image, use a triangle sprite pointing down, color=#FFFFFF33
       │    └─ AppTitle
       │         TMP Text: "Smart Indoor\nNavigation"
       │         Font size: 28 | Bold | Color: white
       │         Alignment: Center | Line spacing: 1.1
       │
       ├─ DotsIndicator
       │    HorizontalLayoutGroup, spacing=8, childAlignment=MiddleCenter
       │    Height: 16 | Pos Y: -490
       │    ├─ Dot_Active   Image, circle sprite, white, size=24×8, BorderRadius via 9-slice
       │    ├─ Dot2         Image, circle sprite, color=#FFFFFF66, size=8×8
       │    └─ Dot3         Image, circle sprite, color=#FFFFFF66, size=8×8
       │
       └─ BottomSheet
            RectTransform: Anchor bottom-stretch
            Height: 320 | Pos Y: 0
            Image: white | Source Image: rounded-rect sprite (corner radius ~24px)
            VerticalLayoutGroup: padding=20, spacing=0, childForceExpandWidth=true
            ─────────────────────────────
            ├─ SectionTitle  TMP "Popular Destinations", size=13, bold, color=#212121
            └─ DestinationsList
                 RectTransform: flexible height
                 VerticalLayoutGroup spacing=0, childForceExpandWidth=true
                 Component: SplashScreenUI.cs
                   destinationListParent = DestinationsList
                   destinationItemPrefab = [assign prefab]
                   destinationData       = [assign SO]
                   bottomSheet           = BottomSheet RectTransform
                   dots[0..2]            = Dot_Active, Dot2, Dot3

=============================================================
## SCENE 1 — MainScene
=============================================================

Hierarchy:
───────────────────────────────────────────
MapCamera                    ← Separate camera for floor plan
  Clear Flags: Solid Color | Background: #E8F0FE
  Culling Mask: MapLayer only
  Output Texture: [RenderTexture asset, 512×512]
  Transform: overhead view (pos 0,8,0 | rot 90,0,0 | ortho size 5)

Main Camera
  Clear Flags: Solid Color | Background: #FFFFFF
  Culling Mask: Everything except MapLayer

FloorPlanRoot  (layer: MapLayer)
  ├─ FloorMesh      (Quad, scale 10×1×16, white/light gray material)
  ├─ Room_Lab       (Cube, scale 2×0.1×1.8, color #D6E8F9)
  ├─ Room_305       (Cube, scale 1.5×0.1×1.2, color #D6E8F9)
  ├─ Room_Cafeteria (Cube, scale 2.5×0.1×2, color #C8E6C9)
  ├─ Room_Exit      (Cube, scale 1.5×0.1×1.5, color #D6E8F9)
  ├─ YouMarker      (Sphere, scale 0.25, color #1976D2)
  └─ RouteRenderer  (Empty GO with LineRenderer + FloorMapRenderer.cs)

EventSystem

Canvas
  └─ MainRoot (VerticalLayoutGroup, spacing=0)
       │
       ├─ TopBar                 Image #1565C0 | height 88
       │    HorizontalLayoutGroup padding=(14,14,50,12) spacing=8
       │    ├─ LocationDot       Image, circle, white, 20×20
       │    └─ SearchBar         Image white, rounded, flex=1, height=38
       │         HorizontalLayoutGroup padding=(10,10,8,8) spacing=6
       │         ├─ SearchIcon   TMP "🔍" size=14 color=#888
       │         ├─ SearchInput  TMP_InputField transparent, placeholder="Search for room, lab, office…"
       │         └─ MicBtn       Button | TMP "🎤"
       │
       ├─ FloorBar               Image white, height=44, border-bottom line
       │    HorizontalLayoutGroup padding=(14,14,0,0) spacing=6
       │    ├─ BackBtn  Button TMP "‹" color=#2196F3 size=15 bold
       │    ├─ FloorTitleTMP "Floor 2" size=15 bold flex=1
       │    ├─ MenuBtn  Button TMP "☰" size=16
       │    └─ LockBtn  Button TMP "🔒" size=14
       │
       ├─ MapContainer          RawImage, height=200
       │    texture = [RenderTexture from MapCamera]
       │    └─ YouBadge          Image white rounded, anchored bottom-center
       │         HorizLayout padding=(10,10,5,5) spacing=5
       │         ├─ YouDot  Image circle #2196F3 size=8×8
       │         └─ YouText TMP "You" size=11 bold
       │
       ├─ ScrollView            Scroll Rect, flex height
       │    └─ Viewport → Content (VerticalLayoutGroup spacing=0, padding=14)
       │         ├─ SectionTitle1  TMP "Popular Destinations" size=13 bold
       │         ├─ [DestItem prefabs for top section]
       │         ├─ SectionTitle2  TMP "Popular Destinations" size=13 bold marginTop=12
       │         └─ [DestItem prefabs for bottom section]
       │
       └─ StartARBtn            Button, height=52, margin=14
            Image color=#1565C0, radius=12
            TMP "Start AR Navigation" white bold size=15
            Shadow: EffectColor=#1565C066 distance=(0,4)

Component on Canvas root: MainScreenUI.cs — wire all refs

=============================================================
## SCENE 2 — ARScene
=============================================================

Hierarchy:
───────────────────────────────────────────
AR Session                   ← GameObject → XR → AR Session
XR Origin (AR)               ← GameObject → XR → XR Origin (Mobile AR)
  Camera Offset
    └─ Main Camera
         Components (auto-added):
           AR Camera Manager
           AR Camera Background
           AR Raycast Manager (add manually if not present)
  Components on XR Origin:
    AR Plane Manager
      Plane Prefab: [AR Default Plane from AR Foundation samples]

ArrowSpawner                 ← Empty GO
  Component: ARArrowController.cs
    arRaycastManager  = [XR Origin → AR Raycast Manager]
    arPlaneManager    = [XR Origin → AR Plane Manager]
    arrowPrefab       = [ARArrow prefab]
    cameraTransform   = [Main Camera transform]
    arrowCount        = 5
    arrowSpacing      = 0.4
    arrowScale        = 0.18

EventSystem

Canvas (Screen Space - Overlay)
  └─ ARHUDRoot
       ├─ TopBar              Image white, height=88
       │    HorizLayout padding=(14,14,44,10) spacing=8
       │    ├─ BackBtn  Button TMP "‹" size=18 color=#333
       │    └─ SearchBar      [same as MainScene]
       │
       ├─ Spacer              FlexibleSpace (fills AR camera view gap)
       │
       ├─ ControlsRow         height=46, anchored to bottom at -118
       │    HorizLayout spacing=10, padding=(0,0,0,0) childAlignment=MiddleCenter
       │    ├─ StopBtn    Button, Image #1E1E28CC rounded, HorizLayout spacing=6
       │    │                ├─ RedDot Image circle #F44336 size=10
       │    │                └─ TMP "Stop" white bold size=12
       │    ├─ RecenterBtn Button, Image #1E1E28CC rounded
       │    │                TMP "↻  Recenter" white bold size=12
       │    └─ MapBtn     Button, Image #1E1E28CC rounded
       │                    TMP "📍 Map" white bold size=12
       │
       └─ DestinationCard     Image white, height=72, anchored bottom margin=16
            RectTransform: anchor bottom-stretch, Left=14 Right=14 Bottom=16
            Image: white, rounded radius=14
            Shadow: offset=(0,4) color=#00000040
            HorizLayout padding=(12,14,16,16) spacing=12
            ├─ DestIconBg    Image #1565C0, size=40×40, radius=10
            │    └─ DestIcon TMP "↑" white bold size=18 (or Image sprite)
            ├─ DestInfo      VerticalLayout spacing=2, flex=1
            │    ├─ DestNameTMP "Computer Lab" size=15 bold color=#111
            │    └─ DestMetaTMP "15 meters · 30 sec" size=11 color=#888
            └─ ChevronRight  TMP "›" size=20 color=#AAA

Component on ARHUDRoot: ARNavigationUI.cs — wire all refs
  arrowController = ArrowSpawner.ARArrowController

=============================================================
## ARArrow Prefab Setup
=============================================================

ARArrow (empty root GO)
  └─ ArrowMesh
       Components:
         MeshFilter     (mesh will be set by ARArrowMeshBuilder at runtime)
         MeshRenderer   (assign ArrowGlowMaterial)
         ARArrowMeshBuilder.cs
           width=1.0  depth=0.8  thickness=0.25  height=0.04
           arrowMaterial = ArrowGlowMaterial

ArrowGlowMaterial (URP/Lit or URP/Unlit):
  Surface Type: Transparent
  Base Color: #00E5FF with alpha ~200
  Emission: enabled, color #00E5FF × 2.5
  Render Face: Both (so visible from above and below)

=============================================================
## DestinationItem Prefab Setup
=============================================================

DestinationItem  (Button, HorizLayout padding=(4,4,9,9) spacing=12)
  MinHeight: 54  | LayoutElement: preferredHeight=54
  ├─ IconContainer   Image, size=36×36, radius=10
  │    Component: Image (color set at runtime by DestinationItemUI)
  │    └─ Icon       Image, size=22×22 (assign sprites per destination)
  ├─ InfoContainer   VerticalLayout spacing=2, flex=1
  │    ├─ NameText   TMP size=13 bold color=#222
  │    └─ MetaText   TMP size=11 color=#888
  └─ Chevron         TMP "›" size=14 color=#AAA, width=20

Component: DestinationItemUI.cs — wire iconBackground, iconImage, nameText, metaText

Separator line at bottom:
  Add a child Image, height=1, color=#F0F0F0, stretch horizontal
  (hide on last item via script or manually)
