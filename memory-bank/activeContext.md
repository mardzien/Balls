# Active Context: Current Work Focus

## 🎯 Current Session Summary (2026-01-06)

### Co zostało zrobione w tej sesji:
1. **System zamrażania oparty na odbiciach i licznik UI**:
   - Dodano `FreezeMode` enum (Time / Bounces) - wybór trybu zamrażania
   - Nowe pola w GameSettings: `freezeMode`, `ballMaxBounces`, `enableFreezeIncrementation`, `freezeIncrementStep`
   - Ball.cs śledzi odbicia i czas, z properties `RemainingBounces` i `RemainingTime`
   - Nowy komponent `BallCounterUI` - wyświetla licznik pod piłką (TextMeshPro)
   - Licznik pokazuje wartość pozostałą (odlicza w dół)
   - Inkrementacja opcjonalna - każda kolejna piłka może mieć +1 do limitu
   - GameManager śledzi `ballSpawnIndex` i oblicza wartości dla każdej piłki
   - W trybie Battle obie piłki mają te same wartości
   - Dodano pakiet TextMeshPro (3.0.9) do projektu

### Poprzednie sesje:
1. **Refaktoryzacja randomizacji** (2026-01-04) - uproszczenie i dodanie koloru kształtu
   - Usunięto randomizację `ringRadius`, `ellipseWidthRadius`, `ellipseHeightRadius`, `gravity`
   - Dodano randomizację koloru pierścienia/elipsy (losowy jasny kolor HSV)
   - Dodano pola `shapeColorMinBrightness` i `shapeColorMinSaturation` do GameSettings

2. **Nowe parametry spawnu i luki**:
   - `fixedSpawnPosition` - gdy włączone, wszystkie piłki w rundzie startują z tego samego miejsca
   - `gapInitialAngle` - początkowa pozycja luki (0=góra, 90=prawo, 180=dół, 270=lewo)
   - Randomizacja `gapInitialAngle` w zakresie 0-360 stopni

3. **Tryb Bitwa (Battle Mode)**:
   - Nowy enum `GameMode` (Normal / Battle)
   - 2 piłki startują jednocześnie w symetrycznych pozycjach
   - Kolory komplementarne (Hue przesunięty o 180°)
   - Obie piłki zamarzają w tym samym momencie
   - Po zamrożeniu obie respawnują z pozycji początkowych
   - Game Over gdy którakolwiek piłka ucieknie
   - Pozycja startowa zawsze zafixowana w trybie Battle

4. **Poprzednie zmiany (2025-12-08)**:
   - System wędrującej luki - kształty nie rotują, luka wędruje po obwodzie
   - System Batch Recording z filtrowaniem po długości
   - Losowe kolory piłek (zawsze)

### Aktualny stan:
- ✅ **System zamrażania** - wybór między czasem a odbiciami (FreezeMode)
- ✅ **Licznik UI** - wyświetla wartość pozostałą pod piłką (TextMeshPro)
- ✅ **Inkrementacja** - opcjonalna, każda kolejna piłka +1 do limitu
- ✅ BatchRecordingController działa (F10 start/stop)
- ✅ ParameterRandomizer randomizuje parametry między rundami
- ✅ Losowy kolor kształtu (pierścienia/elipsy) przy każdej rundzie
- ✅ Stałe rozmiary kształtów i grawitacja
- ✅ Fixed spawn position - wszystkie piłki w rundzie startują z tego samego miejsca
- ✅ Gap initial angle - losowa pozycja początkowa luki
- ✅ **Tryb Bitwa** - 2 piłki symetryczne z kolorami komplementarnymi
- ✅ Filtrowanie nagrań po długości (15-40s)
- ✅ Automatyczne usuwanie niepoprawnych nagrań
- ✅ Dynamiczne odtwarzanie kształtu przy zmianie typu
- ✅ Losowe kolory piłek (zawsze)
- ✅ Kompilacja bez błędów

## 📁 Struktura plików

```
Assets/Scripts/
├── Core/
│   ├── GameManager.cs      # Główny kontroler + ballSpawnIndex tracking
│   ├── GameSettings.cs     # Konfiguracja (ScriptableObject) + FreezeMode
│   └── ScreenSetup.cs      # Setup ekranu 9:16
├── Entities/
│   ├── SpawnShape.cs       # Bazowa klasa dla kształtów
│   ├── Ring.cs             # Pierścień z wędrującą luką
│   ├── EllipseShape.cs     # Elipsa z wędrującą luką
│   └── Ball.cs             # Kulka z fizyką + tracking odbić/czasu
├── Effects/
│   ├── GameOverEffect.cs   # Efekty końcowe
│   └── BallTrailEffect.cs  # Efekty ogonków
├── UI/
│   └── BallCounterUI.cs    # Licznik wartości pod piłką (TextMeshPro)
├── Utils/
│   └── SpriteUtility.cs    # Proceduralne sprite'y
└── Recording/
    ├── RecordingController.cs      # Nagrywanie pojedyncze (F9)
    ├── BatchRecordingController.cs # Batch recording (F10)
    ├── ParameterRandomizer.cs      # Randomizacja parametrów
    └── CollisionRecorder.cs        # Zapis kolizji do JSON
```

## 🎮 Sterowanie

| Klawisz | Akcja |
|---------|-------|
| F9 | Start/Stop nagrywania pojedynczego (manualne) |
| F10 | Start/Stop batch recording (automatyczne wiele rund) |

## 📊 Ustawienia nagrywania (w GameConfig)

| Parametr | Domyślna wartość | Opis |
|----------|------------------|------|
| recordingMode | Batch | None / Single / Batch |
| autoStartRecording | true | Automatyczny start przy uruchomieniu gry |
| batchDuration | 1800s (30 min) | Limit czasu sesji batch |
| minRecordingLength | 15s | Minimalna długość rundy |
| maxRecordingLength | 40s | Maksymalna długość rundy |
| enableParameterRandomization | true | Czy randomizować parametry |

## 🎲 Randomizowane parametry (zakresy domyślne)

| Parametr | Min | Max | Włączony | Opis |
|----------|-----|-----|----------|------|
| gameMode | Normal | Battle | ❌ | Tryb gry (domyślnie wyłączone) |
| freezeMode | Time | Bounces | ❌ | Tryb zamrażania (opcjonalnie) |
| shapeType | Ring | Ellipse | ✅ | Typ kształtu |
| shapeColor | HSV random | - | ✅ | Losowy jasny kolor kształtu |
| trailStyle | Comet/Fading/Uniform | - | ✅ | Styl ogonka |
| rotationSpeed | 30 | 90 | ✅ | Prędkość wędrującej luki |
| gapAngleDegrees | 20 | 45 | ✅ | Kąt luki |
| gapInitialAngle | 0 | 360 | ✅ | Początkowa pozycja luki |
| ballRadius | 0.2 | 0.35 | ✅ | Promień piłki |
| ballFreezeTime | 2 | 5 | ❌ | Czas zamrażania (opcjonalnie) |
| ballMaxBounces | 3 | 6 | ❌ | Liczba odbić (opcjonalnie) |
| trailTime | 0.15 | 0.4 | ❌ | Czas ogonka |

## 🔄 Usunięte parametry z randomizacji

| Usunięty parametr | Powód |
|-------------------|-------|
| `ballColor` | Kolory są zawsze losowe |
| `useRandomBallColors` | Zawsze true, zbędny |
| `enableTravelingGap` | Zawsze true, uproszczenie |
| `gapTravelSpeed` | Używa teraz `rotationSpeed` |
| `legacyRing` | Legacy backwards compatibility |
| `autoRecording` | Legacy, nieużywany |
| `ringRadius` | Stały rozmiar pierścienia (4.5) |
| `ellipseWidthRadius` | Stały rozmiar elipsy (4.0) |
| `ellipseHeightRadius` | Stały rozmiar elipsy (7.0) |
| `gravity` | Stała grawitacja (-9.81) |

## 🎯 Następne kroki

1. **Testowanie batch recording** - uruchomić sesję i sprawdzić czy działa
2. **Dostrajanie zakresów randomizacji** - optymalizacja dla najlepszych nagrań
3. **Dźwięki** - efekty przy odbiciach
