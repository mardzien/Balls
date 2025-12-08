# Technical Context: Unity Ball Ring Game

## 🖥️ Core Technology Stack

### Runtime Environment
- **Unity**: 2D project z URP (Universal Render Pipeline)
- **Target Platforms**: Linux (primary), Windows, macOS
- **Input System**: New Unity Input System (nie legacy Input)

### Graphics & Rendering
- **Camera**: Orthographic, size 10 (20 jednostek wysokości)
- **Resolution**: 1080x1920 (YouTube Shorts 9:16)
- **LineRenderer**: Dla rysowania pierścienia
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
│   │   ├── GameSettings.cs     # ScriptableObject z konfiguracją + Trail settings
│   │   └── ScreenSetup.cs      # Konfiguracja ekranu 9:16
│   ├── Entities/
│   │   ├── Ring.cs             # Pierścień z luką + SetVisible()
│   │   └── Ball.cs             # Kulka z fizyką + BallTrailEffect integration
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
ringThickness = 0.2
ballRadius = 0.3
gapAngleDegrees = 30
rotationSpeed = 100     // stopni/s
bounciness = 1.0
gravity = -19.81        // podwójna grawitacja
ballFreezeTime = 3      // sekundy
escapeBuffer = 0.6

// Trail Effect settings (NOWE)
trailStyle = TrailStyle.FadingTrail
trailTime = 0.25f       // sekundy
trailWidthMultiplier = 0.8f

// Game Over Effects
ringParticleCount = 150  // cząsteczki pyłu
```

### Ball.cs - Kluczowe metody
```csharp
Initialize()          // Setup fizyki, wizualizacji i trail effect
Freeze()              // Zamrożenie (Static) + SetFrozen na trail
Unfreeze()            // Odmrożenie (Dynamic)
DisableFreezeTimer()  // Wyłącza auto-freeze (dla uciekającej piłki)
SetupTrailEffect()    // Konfiguracja ogonka
```

### BallTrailEffect.cs - System ogonków (NOWY)
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

// Position History System
Queue<PositionRecord>  // Historia pozycji piłki
GetTailPosition()      // Rzeczywista pozycja końca ogona
```

### Ring.cs - Kluczowe metody
```csharp
Initialize()          // Setup pierścienia
SetColor()            // Zmiana koloru
SetVisible()          // Ukryj/pokaż (dla efektu końcowego)
IsInGap()             // Czy kąt jest w luce
GetRandomSpawnPosition()  // Pozycja spawnu piłki
```

### GameOverEffect.cs - Efekty
```csharp
fragmentsPerBall = 16        // Fragmentów na kulkę
ringParticleCount = 150      // Z GameSettings (konfigurowalne)
explosionForce = 6           // Siła eksplozji
ringExplosionForce = 3       // Siła rozpadu pierścienia
BallTrailEffect.ClearAllCometParticles()  // Cleanup przy restarcie
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

## 🔄 Recent Technical Changes (2025-12-02)

### Trail Effect System (NOWE)
1. **BallTrailEffect.cs**: Nowy komponent zarządzający TrailRenderer
2. **TrailStyle enum**: None, Comet, FadingTrail, ThinUniform
3. **Position History**: Queue<PositionRecord> do śledzenia rzeczywistej ścieżki piłki
4. **Comet Particles**: Drobinki spawnujące się z końca ogona (nie z piłki!)
5. **Frozen State**: Zamrożone piłki nie emitują drobin

### Ball.cs Integration
1. **RequireComponent**: TrailRenderer + BallTrailEffect
2. **SetupTrailEffect()**: Inicjalizacja w SetupVisuals
3. **Freeze()**: Wywołuje trailEffect.SetFrozen(true)

### GameSettings Extensions
1. **trailStyle**: TrailStyle.FadingTrail (domyślnie)
2. **trailTime**: 0.25f sekundy
3. **trailWidthMultiplier**: 0.8f
4. **ringParticleCount**: 150 (Game Over particles)

## 🔄 Recent Technical Changes (2025-12-08)

### Batch Recording System (NOWE)
1. **BatchRecordingController.cs**: Automatyczne nagrywanie wielu rund
   - Filtrowanie po długości (15-40s domyślnie)
   - Automatyczne usuwanie za krótkich/długich nagrań
   - Limit czasu sesji (30 min domyślnie)
   - Sterowanie: F10 start/stop
   
2. **ParameterRandomizer.cs**: Randomizacja parametrów gry
   - ShapeType (Ring/Ellipse)
   - TrailStyle (Comet/FadingTrail/ThinUniform)
   - rotationSpeed (30-90)
   - gapAngleDegrees (20-45)
   - gravity (-25 do -15)
   - bounciness (0.7-1.0)
   - ringRadius, ellipseWidthRadius, ellipseHeightRadius

3. **GameManager Integration**
   - RecreateShape() - dynamiczne odtwarzanie kształtu przy zmianie typu
   - Wsparcie dla BatchRecordingController
   - Przekazywanie kolizji do obu kontrolerów nagrywania
