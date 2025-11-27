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
- **Auto-start**: Nagrywanie startuje z grą

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs      # Główny manager, auto-create components
│   │   ├── GameSettings.cs     # ScriptableObject z konfiguracją
│   │   └── ScreenSetup.cs      # Konfiguracja ekranu 9:16
│   ├── Entities/
│   │   ├── Ring.cs             # Pierścień z luką + SetVisible()
│   │   └── Ball.cs             # Kulka z fizyką + DisableFreezeTimer()
│   ├── Effects/
│   │   └── GameOverEffect.cs   # Efekty końcowe (fragmenty + pył)
│   ├── Utils/
│   │   └── EscapeDetector.cs   # Detekcja ucieczki kulki
│   └── Recording/
│       └── RecordingController.cs  # Sterowanie nagrywaniem
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

// Aktualne wartości w GameConfig
ringRadius = 4.5
ringThickness = 0.2
ballRadius = 0.3
gapAngleDegrees = 30
rotationSpeed = 100     // stopni/s
bounciness = 1.0
gravity = -19.81        // podwójna grawitacja
ballFreezeTime = 3      // sekundy
escapeBuffer = 0.6
```

### Ball.cs - Kluczowe metody
```csharp
Initialize()          // Setup fizyki i wizualizacji
Freeze()              // Zamrożenie (Static)
Unfreeze()            // Odmrożenie (Dynamic)
DisableFreezeTimer()  // Wyłącza auto-freeze (dla uciekającej piłki)
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
fragmentsPerBall = 16    // Fragmentów na kulkę
ringParticleCount = 100  // Cząsteczek pyłu pierścienia
explosionForce = 6       // Siła eksplozji
ringExplosionForce = 3   // Siła rozpadu pierścienia
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

## 🔄 Recent Technical Changes (2025-11-27)

1. **Ball Collider**: `circleCollider.radius = 0.5f` (jednostkowe koło skalowane przez transform)
2. **Ball Sprite**: `radius = size / 2f` (pełny promień dla zgodności z colliderem)
3. **Ring Collider**: EdgeCollider na `ringRadius` (środek) zamiast `InnerRadius`
4. **Ball Freeze Control**: Flaga `canFreeze` + metoda `DisableFreezeTimer()`
5. **Ring Visibility**: Metoda `SetVisible()` dla efektu końcowego
6. **Auto Components**: GameManager automatycznie tworzy GameOverEffect i RecordingController
