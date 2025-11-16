# Console Snake (C#/.NET 8)

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Runs in Terminal](https://img.shields.io/badge/Console-Game-333)](#)

A fast, modern take on the classic Snake game, built as a pure C# console application targeting .NET 8. It features a polished terminal UI, two difficulty modes, dynamic speed/levels, items with effects, and persistent high scores.

---

## Features

- **Two modes:** Normal and Extreme (select with `1` or `2`).
- **Smooth gameplay:** Fixed-timestep movement with speed scaling by level.
- **Levels and speed:** Level increases every 5 foods; snake speed ramps up each level.
- **Items with effects:**
	- Normal food: `+10` points.
	- Rare fruit (Extreme only): `+20` points; flashes after a few seconds.
	- Poison (Extreme only): `-10` points; no growth.
- **Persistent high score:** Stored in `highscore.txt` (auto-created/updated).
- **Polished UI:** Unicode borders, distinct head/body/tail, blinking “NEW RECORD” banner, inline help.
- **Cross‑platform:** Works in any UTF‑8 capable terminal on Linux/macOS/Windows.

---

## Gameplay Preview

Normal mode (letters), sample frame:

```text
Score:   30  High:  120  Level:  02  Length:   06
╔══════════════════════════════════════╗
║                                      ║
║          ○ ○ ○ ○ ○                   ║
║        ●                             ║
║                         A            ║
║                                      ║
║                                      ║
║                                      ║
║                                      ║
╚══════════════════════════════════════╝
Select Mode: 1. Normal   2. Extreme
Selected: Normal    Press any direction to begin.
Controls: Arrow keys or WASD to move
```

Extreme mode (emoji), sample frame:

```text
Score:   40  High:  120  Level:  03  Length:   07
╔══════════════════════════════════════╗
║                                      ║
║   ● ○ ○ ○ ○ ○                        ║
║                            🍎        ║
║                  🍇                  ║
║                           💣         ║
║                                      ║
╚══════════════════════════════════════╝
🍎 Normal food = +10 score
🍇 Rare fruit = +20 score
💣 Poison = -10 score
```

Game Over overlay example:

```text
Score:   70  High:  120  Level:  04  Length:   09
╔══════════════════════════════════════╗
║                                      ║
║               GAME OVER !            ║
║                                      ║
╚══════════════════════════════════════╝
Final Score: 70
NEW RECORD!              ← blinks for a few seconds
Press R to restart or Q to quit.
```

---

## How to Run

Requirements: **.NET 8 SDK**

- Using `make` (convenience target):

```bash
make run
```

- Using the provided script:

```bash
./run.sh
```

- Using `dotnet` directly:

```bash
dotnet run --project ConsoleGame.csproj
```

Tip: Use a terminal that supports UTF‑8 so box‑drawing characters and emojis render properly.

---

## How to Play

- **Start screen:**
	- Press `1` for Normal, `2` for Extreme to select mode.
	- Press any direction, `Enter`, or `Space` to start.
- **Movement:** Arrow keys or `W/A/S/D`.
- **Restart:** `R` (available on Game Over).
- **Quit:** `Q` or `Esc` at any time.

---

## Scoring & Items

- **Normal food** (`A` in Normal, `🍎` in Extreme): `+10` points and grow by 1.
- **Rare fruit** (`G` / `🍇`, Extreme only): `+20` points and grow by 1. Starts flashing after a short delay.
- **Poison** (`X` / `💣`, Extreme only): `-10` points, no growth.
- **Leveling:** Every 5 foods eaten (Normal or Rare) increases level; movement interval decreases until a minimum threshold for a snappier feel.
- **High score:** Saved in `highscore.txt` and highlighted in yellow during play. When you set a new record, “NEW RECORD!” blinks briefly on the Game Over screen.

---

## Game Over Logic

- Hitting a wall or your own body ends the run.
- On Game Over:
	- Final score is shown below the board.
	- If it beats the previous high, it writes a new value to `highscore.txt` and shows a blinking “NEW RECORD!”.
	- Press `R` to restart or `Q`/`Esc` to exit.

---

## Project Structure

- `Program.cs` – entry point that launches the game loop.
- `Game.cs` – core loop, rendering, input, items, scoring, difficulty, and high score persistence.
- `Snake.cs` – snake body management, movement, and collisions.
- `Position.cs`, `Direction.cs` – small types for coordinates and movement.
- `Makefile`, `run.sh` – convenience for running the project.
- `highscore.txt` – persisted high score (auto-created/updated at runtime).

---

## Technologies

- **Runtime:** .NET 8
- **Language:** C# (single‑project console app)
- **UI:** Unicode/ASCII console rendering (no external UI libraries)

---

## Credits

Built with care by **Yaz and Anes**.

---

## License

This project is licensed under the [MIT License](LICENSE).
