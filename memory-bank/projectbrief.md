# Project Brief: Unity Ball Ring Game

## 🎯 Core Mission
Gra fizyczna w Unity - kulka odbija się wewnątrz obracającego się pierścienia z luką. Gdy kulka ucieknie przez lukę - gra się kończy i restartuje. Projekt zoptymalizowany pod nagrywanie dla YouTube Shorts (format 9:16).

## 📋 Główne Cele
- **Viral Content Creation**: Proste, satysfakcjonujące gameplay do nagrywania short-form videos
- **YouTube Shorts Ready**: Pionowy format wideo (9:16, 1080x1920)
- **Automatic Gameplay Loop**: Gra automatycznie restartuje po ucieczce kulki
- **Easy Recording**: Wbudowany system nagrywania (klawisz F9)

## 🎮 Podstawowa Mechanika
- **Pierścień**: Obracający się ring z luką, generowany proceduralnie
- **Kulka**: Fizyka 2D (Rigidbody2D), odbija się od pierścienia
- **Escape Detection**: Gdy kulka przekroczy promień pierścienia - Game Over
- **Auto Restart**: Automatyczny restart rundy po ucieczce

## 🎬 Recording Pipeline
- **Unity Recorder**: Wbudowane nagrywanie do WebM
- **Format**: 1080x1920 @ 60 FPS
- **Codec**: WebM (kompatybilny z Linux)
- **Control**: F9 start/stop, auto-stop po 60s

## 🎯 Target Audience
- Content creators tworzący YouTube Shorts
- TikTok creators szukający gaming content
- Social media channels z satisfying gameplay

## 📊 Success Metrics
- **Performance**: Stabilny 60 FPS
- **Quality**: Czyste nagrania bez frame drops
- **Simplicity**: Prosty setup i obsługa

## 🔧 Technical Philosophy
- **Unity 2D**: Wykorzystanie Unity physics i rendering
- **ScriptableObject Config**: Łatwa konfiguracja przez Inspector
- **Modular Design**: Rozdzielone komponenty (Ring, Ball, GameManager)
- **New Input System**: Zgodność z najnowszym Unity Input System

