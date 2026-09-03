# Technical Context: Unity Ball Ring Game

## 🖥️ Core Technology Stack

### Runtime Environment
- **Unity**: 2D project z URP (Universal Render Pipeline)
- **Target Platforms**: Linux (primary), Windows, macOS
- **Input System**: New Unity Input System (nie legacy Input)

### Graphics & Rendering
- **Camera**: Orthographic, size 10 (20 jednostek wysokości)
- **Resolution**: 1080x1920 (YouTube Shorts 9:16)
- **LineRenderer**: Dla rysowania pierścienia/elipsy
- **SpriteRenderer**: Dla kulki (proceduralna tekstura)
- **TrailRenderer**: Dla efektów ogonków piłek

### Physics Engine
- **Rigidbody2D**: Fizyka kulki (Dynamic/Static)
- **CircleCollider2D**: Kolizja kulki (radius = 0.5f, jednostkowe koło)
- **EdgeCollider2D**: Kolizja pierścienia (na środkowym promieniu)
- **PhysicsMaterial2D**: Bounce i friction
- **CollisionDetectionMode2D.Continuous**: Zapobiega tunneling

### Recording
- **Unity Recorder**: Pakiet com.unity.recorder 5.1.3
- **Codec**: WebM (MP4 nie działa na Linux)
- **Quality**: High
- **FPS**: 60
- **Single Recording**: F9, auto-start z grą (RecordingController)
- **Batch Recording**: F10, wiele rund z filtrowaniem (BatchRecordingController)

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs      # Główny manager, auto-create components
│   │   ├── GameSettings.cs     # ScriptableObject z konfiguracją
│   │   └── ScreenSetup.cs      # Konfiguracja ekranu 9:16
│   ├── Entities/
│   │   ├── SpawnShape.cs       # Bazowa klasa abstrakcyjna dla kształtów
│   │   ├── Ring.cs             # Pierścień z wędrującą luką
│   │   ├── EllipseShape.cs     # Elipsa z wędrującą luką
│   │   └── Ball.cs             # Kulka z fizyką + BallTrailEffect
│   ├── Effects/
│   │   ├── GameOverEffect.cs   # Efekty końcowe (fragmenty + pył)
│   │   └── BallTrailEffect.cs  # Efekty ogonków (TrailRenderer + particles)
│   ├── Utils/
│   │   └── SpriteUtility.cs    # Proceduralne sprite'y (koła)
│   └── Recording/
│       ├── RecordingController.cs      # Nagrywanie pojedyncze (F9)
│       ├── BatchRecordingController.cs # Batch recording (F10)
│       ├── ParameterRandomizer.cs      # Randomizacja parametrów
│       └── CollisionRecorder.cs        # Zapis kolizji do JSON
├── Settings/
│   └── (URP settings)
├── Scenes/
│   └── SampleScene.unity
└── GameConfig.asset            # Konfiguracja gry
```

## 🎮 Key Components

### GameSettings (ScriptableObject)
```csharp
// Stałe dla YouTube Shorts
SCREEN_WIDTH = 1080
SCREEN_HEIGHT = 1920
ASPECT_RATIO = 9/16 = 0.5625
WORLD_HEIGHT = 20
WORLD_WIDTH = 11.25

// Core settings
ringRadius = 4.5
ringThickness = 0.3
ballRadius = 0.25
gapAngleDegrees = 30
rotationSpeed = 45      // prędkość wędrującej luki (stopni/s)
bounciness = 0.8
gravity = -9.81
ballFreezeTime = 3      // sekundy
escapeBuffer = 0.6

// Ball color settings (zawsze losowe)
ballColorMinBrightness = 0.8
ballColorMinSaturation = 0.7

// Trail Effect settings
trailStyle = TrailStyle.FadingTrail
trailTime = 0.25f       // sekundy
trailWidthMultiplier = 0.8f

// Game Over Effects
ringParticleCount = 150  // cząsteczki pyłu
```

### SpawnShape.cs - Bazowa klasa kształtów
```csharp
// Właściwości abstrakcyjne
CurrentAngle          // Nieużywany - kształty nie rotują
GapAngle              // Pozycja wędrującej luki
InnerRadius           // Wewnętrzny promień
OuterRadius           // Zewnętrzny promień
GapStartAngle         // Początek luki
GapEndAngle           // Koniec luki

// Kluczowe metody
Initialize()          // Setup kształtu
UpdateGapPosition()   // Aktualizacja pozycji luki
GenerateShape()       // Generowanie geometrii
ResetRotation()       // Reset pozycji luki
SetVisible()          // Ukryj/pokaż
```

### Ball.cs - Kluczowe metody
```csharp
Initialize(settings)  // Setup fizyki, wizualizacji, losowy kolor
Freeze()              // Zamrożenie (Static) + SetFrozen na trail
DisableFreezeTimer()  // Wyłącza auto-freeze (dla uciekającej piłki)
SetupTrailEffect()    // Konfiguracja ogonka

// Kolory są ZAWSZE losowe (usunięto ballColor i useRandomBallColors)
GenerateRandomBrightColor()  // HSV z min brightness i saturation
```

### BallTrailEffect.cs - System ogonków
```csharp
// Enum TrailStyle
None           // Bez ogonka
Comet          // Cienki->gruby + drobinki z końca ogona
FadingTrail    // Jasny->przezroczysty
ThinUniform    // Jednolita szerokość i jasność

// Kluczowe metody
Initialize()           // Setup stylu, koloru, czasu
SetFrozen()            // Zatrzymuje emisję drobin
UpdateColor()          // Aktualizacja koloru przy zamrożeniu
ClearAllCometParticles() // Czyści wszystkie drobinki (static)
```

### Ring.cs / EllipseShape.cs - Kształty z wędrującą luką
```csharp
// Wspólne cechy (dziedziczą z SpawnShape)
- Kształt NIE obraca się wokół własnej osi
- Luka wędruje po obwodzie z prędkością rotationSpeed
- GenerateShape() regeneruje geometrię przy każdej aktualizacji luki
- GapAngle przechowuje aktualną pozycję luki
```

### Input System
- **Nowy Input System** (nie legacy UnityEngine.Input)
- Użycie `Keyboard.current.f9Key.wasPressedThisFrame`
- Pakiet: com.unity.inputsystem

## 🔧 Development Setup

### Wymagane pakiety (Packages/manifest.json)
- com.unity.recorder (5.1.3)
- com.unity.inputsystem
- com.unity.render-pipelines.universal

### Git
- `.gitignore` wykluczający Library/, Temp/, Logs/
- Recordings/ - pliki ignorowane, folder zachowany (.gitkeep)

## ⚠️ Platform-Specific Notes

### Linux (Primary)
- WebM codec zamiast MP4 (MP4 nie obsługiwany)
- Testowane na Fedora

### Windows/macOS
- MP4 powinien działać (do zweryfikowania)

## 🎯 Performance Targets
- **Rendering**: Stabilne 60 FPS
- **Recording**: 60 FPS bez frame drops
- **Memory**: Niskie zużycie (prosta gra 2D)

## 🔄 Recent Technical Changes (2025-12-08)

### Refaktoryzacja - uproszczenie kodu
1. **Usunięte parametry**:
   - `ballColor` - kolory zawsze losowe
   - `useRandomBallColors` - zawsze true
   - `enableTravelingGap` - zawsze włączona wędrująca luka
   - `gapTravelSpeed` - teraz używa `rotationSpeed`
   - `legacyRing` - usunięty legacy kod
   - `autoRecording` - usunięty legacy kod

2. **Uproszczony system wędrującej luki**:
   - Kształty (Ring, Ellipse) nie obracają się wokół osi
   - Luka wędruje po obwodzie z prędkością `rotationSpeed`
   - Jeden parametr dla obu typów kształtów

3. **Ball.cs**:
   - `Initialize()` bez parametru randomColor
   - Kolory zawsze generowane losowo

### Batch Recording System
1. **BatchRecordingController.cs**: Automatyczne nagrywanie wielu rund
   - Filtrowanie po długości (15-40s domyślnie)
   - Automatyczne usuwanie za krótkich/długich nagrań
   - Limit czasu sesji (30 min domyślnie)
   - Sterowanie: F10 start/stop
   
2. **ParameterRandomizer.cs**: Randomizacja parametrów gry
   - ShapeType (Ring/Ellipse)
   - TrailStyle (Comet/FadingTrail/ThinUniform)
   - rotationSpeed (30-90) - prędkość wędrującej luki
   - gapAngleDegrees (20-45)
   - gravity (-25 do -15)
   - bounciness (0.7-1.0)
   - ringRadius, ellipseWidthRadius, ellipseHeightRadius

3. **GameManager Integration**
   - RecreateShape() - dynamiczne odtwarzanie kształtu przy zmianie typu
   - Wsparcie dla BatchRecordingController
   - Przekazywanie kolizji do obu kontrolerów nagrywania
