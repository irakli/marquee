# Marquee

Smooth scrolling marquee text for TextMeshPro in Unity UI.

## Installation

### Via Git URL

Add this line to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.iraklichkuaseli.marquee": "https://github.com/irakli/marquee.git"
  }
}
```

Or install via Package Manager:
1. Open Window → Package Manager
2. Click the `+` button
3. Select "Add package from git URL"
4. Enter: `https://github.com/irakli/marquee.git`

### Manual

Clone or download this repo into your project's `Packages/` folder.

### Requirements

- Unity 2022.3 or later
- TextMeshPro (included in Unity)

## Features

- **Single component** - Add `MarqueeText` alongside any `TMP_Text`, no hierarchy changes
- **Three behaviors** - Continuous loop, PingPong bounce, OneShot
- **Edge fading** - Soft alpha fade via TMP shader clipping softness, zero overhead
- **Edit-mode preview** - See scrolling without entering Play mode

## Setup

**Add Component > UI > Effects > Marquee Text** on any GameObject with a `TextMeshProUGUI` or `TextMeshPro` component.

Text scrolls automatically when it overflows the rect. Enable **Force Scrolling** to always scroll.

## Usage

### Basic

```csharp
var marquee = GetComponent<MarqueeText>();

marquee.Play();
marquee.Pause();
marquee.Restart();   // reset to home and start scrolling
marquee.Stop();      // reset to home and stop
```

### Runtime Configuration

```csharp
marquee.SetSpeed(100f);

// Read-only state
bool overflows = marquee.IsOverflowing;
bool playing   = marquee.IsPlaying;
float progress = marquee.ScrollProgress; // 0..1

// Events
marquee.StateChanged   += isPlaying => Debug.Log(isPlaying);
marquee.ScrollBegan    += () => Debug.Log("started scrolling");
marquee.ScrollCompleted += () => Debug.Log("cycle complete");
```

## Inspector

### Scroll

| Property | Description |
|----------|-------------|
| **Direction** | Left or Right |
| **Behavior** | Continuous, PingPong, or OneShot |
| **Speed Mode** | Rate (units/sec) or Duration (seconds per full cycle) |
| **Speed / Duration** | The speed value (meaning depends on Speed Mode) |
| **Delay at Edge** | Seconds to pause before looping/reversing |
| **Easing Curve** | AnimationCurve for PingPong/OneShot movement (not shown for Continuous) |

### Spacing

| Property | Description |
|----------|-------------|
| **Leading Buffer** | Extra space before the text starts (in scroll direction) |
| **Trailing Buffer** | Extra space after text ends before it wraps |

### Behavior

| Property | Description |
|----------|-------------|
| **Force Scrolling** | Scroll even when text fits within bounds |
| **Hold Scrolling** | Prevent scrolling but keep edge fades visible |
| **Tap to Scroll** | Only begin scrolling when clicked/tapped |

### Fade Edges

| Property | Description |
|----------|-------------|
| **Enable** | Turn on soft alpha fade at edges |
| **Fade Width** | Width of the fade zone |

**Live Preview** - Toggle at the top of the inspector to enable edit-mode animation and playback controls.

## API Reference

### Methods

| Method | Description |
|--------|-------------|
| `Play()` | Start or resume scrolling |
| `Pause()` | Pause at current position |
| `Restart()` | Reset to home and start scrolling |
| `Stop()` | Reset to home and stop |
| `SetSpeed(float)` | Set scroll speed at runtime |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsPlaying` | `bool` | Whether the marquee animation is active |
| `IsOverflowing` | `bool` | Whether text exceeds the visible area |
| `AwayFromHome` | `bool` | Whether text is away from its starting position |
| `ScrollProgress` | `float` | Normalized scroll progress from 0 (home) to 1 (fully scrolled) |

### Events

| Event | Description |
|-------|-------------|
| `StateChanged` | Fired when play state changes (parameter: bool isPlaying) |
| `ScrollBegan` | Fired when a scroll cycle begins |
| `ScrollCompleted` | Fired when a scroll cycle completes |

## Advanced Usage

### How It Works

MarqueeText operates directly on TMP's vertex data in `LateUpdate`:

1. Forces overflow mode so all characters render beyond the rect
2. Clips overflowing text at the rect bounds via `CanvasRenderer` rect clipping (respects TMP margins)
3. Offsets all character vertices by the current scroll position
4. In Continuous mode, wraps characters that leave one edge back to the other side
5. When edge fading is enabled, sets clipping softness for a smooth alpha fade

### Subclassing

Override virtual methods for custom behavior:

```csharp
public class CustomMarquee : MarqueeText
{
    protected override void OnScrollBegan() { /* ... */ }
    protected override void OnScrollCompleted() { /* ... */ }
}
```

### Performance

If the marquee lives inside a complex UI, place it under its own nested `Canvas`. This prevents per-frame vertex updates from forcing a batch rebuild on sibling elements.

For world-space canvases, prefer `TextMeshPro` (3D) over `TextMeshProUGUI` to avoid Canvas overhead. Both work with MarqueeText.

## Package Structure

```
com.iraklichkuaseli.marquee/
├── package.json                # UPM package manifest
├── README.md                   # This file
├── CHANGELOG.md                # Version history
├── LICENSE.md                  # MIT License
├── Runtime/
│   ├── MarqueeText.cs          # Main marquee component
│   └── IrakliChkuaseli.Marquee.asmdef
├── Editor/
│   ├── MarqueeTextEditor.cs        # Custom inspector
│   ├── MarqueePlayModeHandler.cs   # Play mode state handler
│   └── IrakliChkuaseli.Marquee.Editor.asmdef
├── Icons/
└── Tests/
    ├── Editor/
    └── Runtime/
```

## Troubleshooting

### Text not scrolling

**Problem:** MarqueeText is added but the text doesn't move.

**Solution:** Text only scrolls when it overflows the rect bounds. Either make the text longer than the rect, or enable **Force Scrolling** in the Inspector.

### Word wrapping or overflow mode warnings

**Problem:** Inspector shows info messages about overflow mode or word wrapping.

**Solution:** MarqueeText requires overflow mode set to Overflow and word wrapping disabled. Both are enforced automatically when the component enables.

## License

MIT
