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
- **Rigidbody2D**: Fizyka kulki
- **CircleCollider2D**: Kolizja kulki
- **EdgeCollider2D**: Kolizja pierścienia
- **PhysicsMaterial2D**: Bounce i friction

### Recording
- **Unity Recorder**: Pakiet com.unity.recorder 5.1.3
- **Codec**: WebM (MP4 nie działa na Linux)
- **Quality**: High
- **FPS**: 60

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs      # Główny manager gry
│   │   ├── GameSettings.cs     # ScriptableObject z konfiguracją
│   │   └── ScreenSetup.cs      # Konfiguracja ekranu 9:16
│   ├── Entities/
│   │   ├── Ring.cs             # Obracający się pierścień
│   │   └── Ball.cs             # Kulka z fizyką
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

// Domyślne wartości
ringRadius = 4.5       // 80% szerokości ekranu
ringThickness = 0.3
ballRadius = 0.25
gapAngleDegrees = 30
rotationSpeed = 45     // stopni/s
bounciness = 0.8
gravity = -9.81
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

