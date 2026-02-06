# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-07

### Added
- MarqueeText component - add alongside any TMP_Text for scrolling
- Continuous, PingPong, and OneShot scroll behaviors
- Left/Right direction
- Speed as rate (px/sec) or duration (seconds per cycle)
- AnimationCurve easing for PingPong and OneShot
- Leading and trailing buffer spacing
- Behavior flags: forceScrolling, holdScrolling, tapToScroll
- Edge fading via CanvasRenderer clipping softness
- Delay at edge before looping/reversing
- Edit-mode live preview
- Auto stop/restart on application pause/resume
- Custom inspector with playback controls, status, and validation
- Runtime API: Play(), Pause(), Stop(), Restart(), SetSpeed()
- Read-only state: IsPlaying, IsOverflowing, AwayFromHome, ScrollProgress
- Events: StateChanged, ScrollBegan, ScrollCompleted
- Virtual callbacks: OnScrollBegan(), OnScrollCompleted() for subclassing

### Known Limitations
- Horizontal scrolling only (vertical marquee is not supported)
- Single-line text only (word wrapping is disabled automatically)
- Overflow mode is forced to Overflow when the component is enabled
