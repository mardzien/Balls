# Przewodnik: System zamrażania i licznik

## 🎯 Przegląd

Nowy system zamrażania pozwala na wybór między dwoma trybami:
- **Time** - piłki zamarzają po określonym czasie
- **Bounces** - piłki zamarzają po określonej liczbie odbić

Dodatkowo każda kolejna piłka może mieć zwiększony limit (inkrementacja).

## ⚙️ Konfiguracja (GameConfig.asset)

### Podstawowe ustawienia

W inspektorze znajdziesz nową sekcję **Ball Freeze & Spawn**:

1. **Freeze Mode** - wybierz tryb:
   - `Time` - zamrażanie po czasie
   - `Bounces` - zamrażanie po odbiciach

2. **Ball Freeze Time** (dla trybu Time)
   - Czas w sekundach przed zamrożeniem
   - Domyślnie: 3s

3. **Ball Max Bounces** (dla trybu Bounces)
   - Liczba odbić przed zamrożeniem
   - Domyślnie: 4

4. **Enable Freeze Incrementation**
   - Czy włączyć inkrementację dla kolejnych piłek
   - Domyślnie: wyłączone

5. **Freeze Increment Step**
   - O ile zwiększyć wartość dla każdej kolejnej piłki
   - Domyślnie: 1

6. **Show Counter**
   - Czy wyświetlać licznik pod kształtem
   - Domyślnie: włączone (✓)

## 📊 Przykłady użycia

### Przykład 1: Tryb Time z inkrementacją

**Ustawienia:**
- Freeze Mode: `Time`
- Ball Freeze Time: `1`
- Enable Freeze Incrementation: `✓`
- Freeze Increment Step: `1`

**Rezultat:**
- Piłka 1: zamraża się po 1 sekundzie (licznik: 1.0, 0.9, 0.8, ..., 0.1, 0.0)
- Piłka 2: zamraża się po 2 sekundach (licznik: 2.0, 1.9, 1.8, ..., 0.1, 0.0)
- Piłka 3: zamraża się po 3 sekundach (licznik: 3.0, 2.9, 2.8, ..., 0.1, 0.0)
- itd.

### Przykład 2: Tryb Bounces bez inkrementacji

**Ustawienia:**
- Freeze Mode: `Bounces`
- Ball Max Bounces: `5`
- Enable Freeze Incrementation: `☐`

**Rezultat:**
- Wszystkie piłki zamarzają po 5 odbiciach
- Licznik pokazuje: 5, 4, 3, 2, 1, 0

### Przykład 3: Tryb Bounces z inkrementacją

**Ustawienia:**
- Freeze Mode: `Bounces`
- Ball Max Bounces: `3`
- Enable Freeze Incrementation: `✓`
- Freeze Increment Step: `2`

**Rezultat:**
- Piłka 1: 3 odbicia (licznik: 3, 2, 1, 0)
- Piłka 2: 5 odbić (licznik: 5, 4, 3, 2, 1, 0)
- Piłka 3: 7 odbić (licznik: 7, 6, 5, 4, 3, 2, 1, 0)
- itd.

## 🎨 Licznik UI

**Globalny licznik** znajduje się pod pierścieniem/elipsą i pokazuje wartość dla aktywnej piłki:
- **W trybie Time**: pozostały czas z **1 cyfrą po przecinku** (np. 3.7, 2.1, 0.5)
- **W trybie Bounces**: pozostałą liczbę odbić (liczba całkowita)

### Włączanie/wyłączanie:
- W `GameConfig.asset` znajdziesz pole **Show Counter** (domyślnie: włączone)
- Odznacz, aby ukryć licznik całkowicie

### Właściwości:
- **Rozmiar**: Duży (12.0) dla doskonałej widoczności
- **Pozycja**: Dynamicznie obliczona - 1.5 jednostki pod dolną krawędzią pierścienia/elipsy
- **Pozycjonowanie**: Automatycznie dostosowuje się do typu kształtu (Ring/Ellipse)
- **Kolory**:
  - **Biały**: wartość > 2
  - **Żółty**: wartość = 2
  - **Czerwony**: wartość ≤ 1
- **Ukrywanie**: Licznik znika gdy piłka jest zamrożona

## 🎮 Tryb Battle

W trybie Battle obie piłki mają **te same wartości** (ten sam limit czasu/odbić).
Inkrementacja działa normalnie - kolejne pary piłek mają zwiększone limity.

## 🎲 Randomizacja (opcjonalna)

Możesz dodać randomizację nowych parametrów w `ParameterRandomizer.cs`:
- `freezeMode` - losowy wybór między Time/Bounces
- `ballFreezeTime` - losowy czas (np. 2-5s)
- `ballMaxBounces` - losowa liczba odbić (np. 3-6)

## 💡 Wskazówki

1. **Dla trudniejszej gry**: Ustaw niskie wartości bazowe (np. 2s lub 3 odbicia)
2. **Dla łatwiejszej gry**: Ustaw wysokie wartości (np. 5s lub 8 odbić)
3. **Dla progresywnej trudności**: Włącz inkrementację
4. **Dla stałej trudności**: Wyłącz inkrementację
5. **Dla YouTube Shorts**: Wypróbuj różne kombinacje i zobacz, co daje najlepsze nagrania!

## 🔧 Techniczne

### Nowe pliki:
- `Assets/Scripts/UI/BallCounterUI.cs` - globalny licznik UI
- `GameSettings.cs` - rozszerzony o FreezeMode i nowe pola
- `Ball.cs` - tracking odbić i czasu
- `GameManager.cs` - zarządzanie globalnym licznikiem

### Nowe zależności:
- TextMeshPro (3.0.9) - dla wysokiej jakości tekstu licznika

### Uwagi techniczne:
- Licznik jest tworzony w `GameManager.SetupGame()`
- Pozycja obliczana dynamicznie: `shape.Center.y - shape.OuterRadius - 1.5`
- Automatyczna aktualizacja pozycji przy zmianie kształtu (Ring↔Ellipse)
- Aktualizacja przy każdym spawnie piłki (SetActiveBall)
- W trybie Battle śledzi pierwszą piłkę
- Font size: 12.0 (outline: 0.4)