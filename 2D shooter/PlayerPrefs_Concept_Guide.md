# PlayerPrefs in Unity: A Comprehensive Guide

## What is PlayerPrefs?

**PlayerPrefs** is a Unity API that allows you to save and retrieve player data persistently across game sessions. It stores simple key-value pairs (strings, integers, and floats) locally on the player's device, making it ideal for saving game progress, settings, high scores, and other persistent data.

Think of it as a lightweight database that survives even after the game closes and reopens.

---

## Core Concept: Persistent Data Storage

In the 2D Shooter game, PlayerPrefs is used to save the player's **score** and **high score** so they persist between play sessions.

### Why This Matters

Without PlayerPrefs:
- Every time the game restarts, the score resets to 0
- The player loses all progress and achievements
- There's no sense of progression or accomplishment

With PlayerPrefs:
- High scores are remembered forever (until manually reset)
- Current session scores can be saved and restored
- Players can track their best performance over time

---

## How GameManager Uses PlayerPrefs

### 1. **Loading Data on Startup** (HandleStartUp method)

```csharp
void HandleStartUp()
{
    if (PlayerPrefs.HasKey("highscore"))
    {
        highScore = PlayerPrefs.GetInt("highscore");
    }
    if (PlayerPrefs.HasKey("score"))
    {
        score = PlayerPrefs.GetInt("score");
    }
    UpdateUIElements();
    if (printDebugOfWinnableStatus)
    {
        FigureOutHowManyEnemiesExist();
    }
}
```

**What happens:**
- `PlayerPrefs.HasKey("highscore")` checks if a "highscore" key exists in storage
- `PlayerPrefs.GetInt("highscore")` retrieves the integer value associated with that key
- The same process repeats for the current "score"
- This runs in `Start()`, so data is loaded before gameplay begins

**Why check with HasKey first?**
- Prevents errors if the key doesn't exist yet (first time playing)
- Provides a safe way to handle missing data

---

### 2. **Saving the High Score** (SaveHighScore method)

```csharp
public static void SaveHighScore()
{
    if (score > instance.highScore)
    {
        PlayerPrefs.SetInt("highscore", score);
        instance.highScore = score;
    }
    UpdateUIElements();
}
```

**What happens:**
- Compares the current score against the stored high score
- If current score is higher, `PlayerPrefs.SetInt("highscore", score)` saves it
- Updates the in-memory `highScore` variable
- Refreshes the UI to display the new high score

**Key insight:** Only saves when a new high score is achieved, not every frame.

---

### 3. **Resetting the Score** (ResetScore method)

```csharp
public static void ResetScore()
{
    PlayerPrefs.SetInt("score", 0);
    score = 0;
}
```

**What happens:**
- Saves 0 to the "score" key in PlayerPrefs
- Resets the in-memory score variable
- Called in `OnApplicationQuit()` to clean up before the game closes

---

### 4. **Resetting the High Score** (ResetHighScore method)

```csharp
public static void ResetHighScore()
{
    PlayerPrefs.SetInt("highscore", 0);
    if (instance != null)
    {
        instance.highScore = 0;
    }
    UpdateUIElements();
}
```

**What happens:**
- Clears the high score by setting it to 0
- Useful for testing or when players want to start fresh
- Updates the UI immediately

---

### 5. **Saving on Level Complete** (LevelCleared method)

```csharp
public void LevelCleared()
{
    PlayerPrefs.SetInt("score", score);
    if (UIManager.instance != null)
    {
        player.SetActive(false);
        UIManager.instance.allowPause = false;
        UIManager.instance.GoToPage(gameVictoryPageIndex);
        if (victoryEffect != null)
        {
            Instantiate(victoryEffect, transform.position, transform.rotation, null);
        }
    }     
}
```

**What happens:**
- When the player wins, the current score is saved to PlayerPrefs
- This ensures the score persists even if the game crashes after victory
- The high score is automatically updated if this score is higher

---

## Event Chain: Score Persistence Flow

```
Game Starts
    ↓
Awake() → GameManager singleton created
    ↓
Start() → HandleStartUp()
    ↓
PlayerPrefs.HasKey("highscore") → Check if data exists
    ↓
PlayerPrefs.GetInt("highscore") → Load saved high score
    ↓
PlayerPrefs.GetInt("score") → Load current session score
    ↓
UpdateUIElements() → Display scores to player
    ↓
[Gameplay happens, score increases via AddScore()]
    ↓
Enemy defeated → AddScore() called
    ↓
If new high score: SaveHighScore() → PlayerPrefs.SetInt("highscore", score)
    ↓
Level completed → LevelCleared() → PlayerPrefs.SetInt("score", score)
    ↓
Game closes → OnApplicationQuit()
    ↓
SaveHighScore() + ResetScore() → Data persisted to device
    ↓
Next game session → Data reloaded from PlayerPrefs
```

---

## PlayerPrefs API Reference

### Reading Data

| Method | Purpose | Example |
|--------|---------|---------|
| `PlayerPrefs.HasKey(key)` | Check if a key exists | `if (PlayerPrefs.HasKey("highscore"))` |
| `PlayerPrefs.GetInt(key)` | Get an integer value | `int score = PlayerPrefs.GetInt("highscore");` |
| `PlayerPrefs.GetFloat(key)` | Get a float value | `float volume = PlayerPrefs.GetFloat("volume");` |
| `PlayerPrefs.GetString(key)` | Get a string value | `string name = PlayerPrefs.GetString("playerName");` |

### Writing Data

| Method | Purpose | Example |
|--------|---------|---------|
| `PlayerPrefs.SetInt(key, value)` | Save an integer | `PlayerPrefs.SetInt("highscore", 1000);` |
| `PlayerPrefs.SetFloat(key, value)` | Save a float | `PlayerPrefs.SetFloat("volume", 0.8f);` |
| `PlayerPrefs.SetString(key, value)` | Save a string | `PlayerPrefs.SetString("playerName", "Hero");` |

### Cleanup

| Method | Purpose | Example |
|--------|---------|---------|
| `PlayerPrefs.DeleteKey(key)` | Delete one key | `PlayerPrefs.DeleteKey("highscore");` |
| `PlayerPrefs.DeleteAll()` | Delete all data | `PlayerPrefs.DeleteAll();` |
| `PlayerPrefs.Save()` | Force save to disk | `PlayerPrefs.Save();` |

---

## Important Considerations

### 1. **Platform-Specific Storage**

PlayerPrefs stores data differently on each platform:

| Platform | Storage Location |
|----------|------------------|
| **Windows** | Registry (HKEY_CURRENT_USER) |
| **macOS** | ~/Library/Preferences/com.Company.ProductName.plist |
| **Linux** | ~/.config/unity3d/Company/ProductName/prefs |
| **Android** | SharedPreferences |
| **iOS** | NSUserDefaults |
| **WebGL** | Browser LocalStorage |

### 2. **Data Types Limitation**

PlayerPrefs only supports three data types:
- **int** (integers)
- **float** (decimal numbers)
- **string** (text)

For complex data (arrays, custom objects), you must serialize to JSON or use other persistence methods.

### 3. **No Encryption**

PlayerPrefs data is **not encrypted**. Never store sensitive information like passwords or API keys. For the 2D Shooter, storing scores is safe because they're not sensitive.

### 4. **Performance**

- Reading is fast (cached in memory)
- Writing is slower (disk I/O)
- Call `PlayerPrefs.Save()` explicitly if you need guaranteed disk write (usually automatic)

### 5. **Key Naming Convention**

Use clear, descriptive key names:
- ✅ Good: `"highscore"`, `"playerLevel"`, `"volumeLevel"`
- ❌ Bad: `"hs"`, `"pl"`, `"vol"`

---

## Real-World Usage Pattern in GameManager

The GameManager demonstrates a **best practice pattern**:

```csharp
// 1. Load on startup
void HandleStartUp()
{
    if (PlayerPrefs.HasKey("highscore"))
        highScore = PlayerPrefs.GetInt("highscore");
}

// 2. Save when important events occur
public static void AddScore(int scoreAmount)
{
    score += scoreAmount;
    if (score > instance.highScore)
        SaveHighScore();  // Save immediately when beaten
    UpdateUIElements();
}

// 3. Save on application quit
private void OnApplicationQuit()
{
    SaveHighScore();
    ResetScore();
}
```

**Why this works:**
- Data loads once at startup (efficient)
- Data saves only when necessary (not every frame)
- Data persists when the game closes
- UI stays synchronized with saved data

---

## Common Mistakes to Avoid

### ❌ Mistake 1: Not Checking HasKey

```csharp
// WRONG - crashes if key doesn't exist
int score = PlayerPrefs.GetInt("score");
```

```csharp
// RIGHT - safe approach
if (PlayerPrefs.HasKey("score"))
    score = PlayerPrefs.GetInt("score");
```

### ❌ Mistake 2: Saving Every Frame

```csharp
// WRONG - massive performance hit
void Update()
{
    PlayerPrefs.SetInt("score", score);  // Called 60+ times per second!
}
```

```csharp
// RIGHT - save only when needed
public static void AddScore(int amount)
{
    score += amount;
    if (score > highScore)
        SaveHighScore();  // Save only when high score beaten
}
```

### ❌ Mistake 3: Forgetting to Update In-Memory Variables

```csharp
// WRONG - UI shows old value
PlayerPrefs.SetInt("highscore", newScore);
// highScore variable still has old value!
```

```csharp
// RIGHT - keep both in sync
PlayerPrefs.SetInt("highscore", newScore);
instance.highScore = newScore;
UpdateUIElements();
```

---

## Extension Ideas

### 1. **Save Multiple Scores**

```csharp
// Save top 5 scores
for (int i = 0; i < topScores.Count; i++)
{
    PlayerPrefs.SetInt("topScore_" + i, topScores[i]);
}
```

### 2. **Save Game Settings**

```csharp
PlayerPrefs.SetFloat("masterVolume", 0.8f);
PlayerPrefs.SetInt("difficulty", 2);  // 0=Easy, 1=Normal, 2=Hard
PlayerPrefs.SetString("playerName", "Hero");
```

### 3. **Save Progress with Timestamps**

```csharp
PlayerPrefs.SetString("lastPlayDate", System.DateTime.Now.ToString());
PlayerPrefs.SetInt("totalPlayTime", totalSeconds);
```

### 4. **Add a Reset Button in Settings**

```csharp
public void ResetAllData()
{
    PlayerPrefs.DeleteAll();
    GameManager.ResetHighScore();
    GameManager.ResetScore();
    Debug.Log("All data reset!");
}
```

---

## Why PlayerPrefs Matters in Game Development

1. **Player Retention**: Players return to beat their high scores
2. **Progression Feeling**: Visible improvement over time
3. **Settings Persistence**: Volume, difficulty, controls remembered
4. **Analytics**: Track player behavior across sessions
5. **Accessibility**: Simple API, no database setup needed

---

## Summary

| Concept | Key Point |
|---------|-----------|
| **What** | PlayerPrefs stores key-value data persistently on device |
| **When** | Load in Start(), save when important events occur |
| **Where** | Platform-specific (Registry, plist, SharedPreferences, etc.) |
| **Why** | Preserves player progress and settings between sessions |
| **How** | Use HasKey(), GetInt/Float/String(), SetInt/Float/String() |

In the 2D Shooter, PlayerPrefs ensures that your high score is never lost, creating a sense of progression and achievement that keeps players engaged.

---

## Sources

- [Unity Manual: PlayerPrefs](https://docs.unity3d.com/Manual/class-PlayerPrefs.html)
- [Unity Scripting API: PlayerPrefs](https://docs.unity3d.com/ScriptReference/PlayerPrefs.html)
- [Unity Manual: Saving and Loading Data](https://docs.unity3d.com/Manual/SavingandLoadingData.html)
- GameManager.cs implementation in 2D Shooter project
- ScreenshotUtility.cs (additional PlayerPrefs usage example)

---

## Reflection

The most important thing to understand about PlayerPrefs is that **it bridges the gap between runtime data and persistent storage**. While your game runs, scores exist in memory. PlayerPrefs ensures they survive when the game closes. This simple concept is fundamental to creating games that feel rewarding and keep players coming back.

The GameManager demonstrates this perfectly: load once, save strategically, and always keep in-memory and stored data synchronized.
