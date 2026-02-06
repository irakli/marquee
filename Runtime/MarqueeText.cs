using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using System.Collections.Generic;
#endif

namespace IrakliChkuaseli.UI.Marquee
{
    public enum MarqueeDirection
    {
        Left,
        Right
    }

    public enum MarqueeBehavior
    {
        Continuous,
        PingPong,
        OneShot
    }

    public enum SpeedMode
    {
        Rate,
        Duration
    }

    /// <summary>
    /// Scrolling marquee effect for TextMeshPro. Add to the same GameObject as any
    /// TMP_Text to scroll text horizontally within its bounds.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("UI/Effects/Marquee Text")]
    public class MarqueeText : MonoBehaviour, IPointerClickHandler
    {
        private const float FallbackDeltaTime = 1f / 60f;

        // Scroll
        [SerializeField] private MarqueeDirection direction = MarqueeDirection.Left;
        [SerializeField] private MarqueeBehavior behavior = MarqueeBehavior.Continuous;
        [SerializeField] private SpeedMode speedMode = SpeedMode.Rate;
        [SerializeField] private float speed = 50f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private float animationDelay = 1f;

        // Spacing
        [SerializeField] private float leadingBuffer;
        [SerializeField] private float trailingBuffer;

        // Behavior
        [SerializeField] private bool forceScrolling;
        [SerializeField] private bool holdScrolling;
        [SerializeField] private bool tapToScroll;

        // Fade Edges
        [SerializeField] private bool fadeEdges;
        [SerializeField] [Range(0f, 100f)] private float fadeWidth = 20f;

#if UNITY_EDITOR
        // Editor
        [SerializeField] private bool previewInEditor = true;

        private static readonly HashSet<MarqueeText> _activeInstances = new();
        public static IReadOnlyCollection<MarqueeText> ActiveInstances => _activeInstances;
#endif

        // Cached components
        private TMP_Text _tmpText;
        private RectTransform _rectTransform;
        private CanvasRenderer _canvasRenderer;
        private Canvas _rootCanvas;

        // Scroll state
        private float _scrollOffset;
        private float _pauseTimer;
        private bool _isPingPongReversed;
        private bool _isPlaying = true;
        private bool _oneShotComplete;
        private bool _tapTriggered;
        private bool _wasScrolling;

        // Text cache
        private bool _hasTextChanged;
        private float _cachedPreferredWidth;
        private TMP_MeshInfo[] _cachedMeshInfo;

        public bool IsPlaying => _isPlaying;
        public bool IsOverflowing => GetPreferredWidth() > GetRectWidth();
        public bool AwayFromHome => Mathf.Abs(_scrollOffset) > 0.01f;

        /// <summary>
        /// Normalized scroll progress, 0 (home) to 1 (fully scrolled).
        /// </summary>
        public float ScrollProgress
        {
            get
            {
                var max = GetMaxScrollOffset();
                return max > 0f ? Mathf.Clamp01(Mathf.Abs(_scrollOffset) / max) : 0f;
            }
        }

        public event Action<bool> StateChanged;
        public event Action ScrollBegan;
        public event Action ScrollCompleted;

        public void Play()
        {
            if (_isPlaying) return;
            _isPlaying = true;
            StateChanged?.Invoke(true);
        }

        public void Pause()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            StateChanged?.Invoke(false);
        }

        public void Restart()
        {
            ResetState();
            _isPlaying = true;
            ApplyScroll();
            StateChanged?.Invoke(true);
        }

        public void Stop()
        {
            _isPlaying = false;
            ResetState();
            ApplyScroll();
            StateChanged?.Invoke(false);
        }

        public void SetSpeed(float newSpeed)
        {
            speed = Mathf.Max(0f, newSpeed);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!tapToScroll) return;
            if (_isPlaying && AwayFromHome) return;

            _tapTriggered = true;
            if (!_isPlaying)
            {
                _isPlaying = true;
                StateChanged?.Invoke(true);
            }
        }

        protected virtual void OnScrollBegan() { }
        protected virtual void OnScrollCompleted() { }

        private void OnEnable()
        {
#if UNITY_EDITOR
            _activeInstances.Add(this);
#endif
            CacheComponents();
            EnsureOverflowMode();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            _hasTextChanged = true;
            _cachedMeshInfo = null;
            UpdateClipping();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            _activeInstances.Remove(this);
#endif
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            ResetState();
            _cachedMeshInfo = null;

            if (_canvasRenderer)
                _canvasRenderer.DisableRectClipping();

            if (_tmpText)
                _tmpText.ForceMeshUpdate();
        }

        private void LateUpdate()
        {
            if (!ShouldAnimate())
            {
                if (_scrollOffset != 0f)
                {
                    _scrollOffset = 0f;
                    ApplyScroll();
                }
                UpdateClipping();
                return;
            }

            var dt = GetDeltaTime();
            if (dt <= 0f) return;

            UpdateScroll(dt);
            ApplyScroll();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Stop();
            else
                Restart();
        }

        private bool ShouldAnimate()
        {
            if (!_tmpText || !_rectTransform) return false;
            if (!_isPlaying) return false;
            if (holdScrolling) return false;
            if (_oneShotComplete) return false;

#if UNITY_EDITOR
            if (!Application.isPlaying && !previewInEditor) return false;
#endif

            if (tapToScroll && !_tapTriggered) return false;
            if (!forceScrolling && !IsOverflowing) return false;

            return true;
        }

        private void UpdateScroll(float dt)
        {
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= dt;
                return;
            }

            if (!_wasScrolling)
            {
                _wasScrolling = true;
                OnScrollBegan();
                ScrollBegan?.Invoke();
            }

            var preferredWidth = GetPreferredWidth();
            var rectWidth = GetRectWidth();

            switch (behavior)
            {
                case MarqueeBehavior.PingPong:
                    UpdatePingPong(dt, preferredWidth, rectWidth);
                    break;
                case MarqueeBehavior.OneShot:
                    UpdateOneShot(dt, preferredWidth);
                    break;
                default:
                    UpdateContinuous(dt, preferredWidth);
                    break;
            }
        }

        private void UpdateContinuous(float dt, float preferredWidth)
        {
            var loopDistance = preferredWidth + GetLoopGap();
            var effectiveSpeed = GetEffectiveSpeed(loopDistance);
            var scrollDir = direction == MarqueeDirection.Left ? 1f : -1f;

            _scrollOffset += effectiveSpeed * dt * scrollDir;

            if (Mathf.Abs(_scrollOffset) >= loopDistance)
            {
                _scrollOffset -= loopDistance * scrollDir;
                _pauseTimer = animationDelay;
                FireScrollCompleted();
            }
        }

        private void UpdatePingPong(float dt, float preferredWidth, float rectWidth)
        {
            var overflow = preferredWidth - rectWidth;
            if (overflow <= 0f) return;

            var effectiveSpeed = GetEffectiveSpeed(overflow);

            // Offset always travels 0..overflow..0 regardless of direction.
            // Direction is applied visually in GetVisualOffset().
            _scrollOffset += effectiveSpeed * dt * (_isPingPongReversed ? -1f : 1f);

            if (_scrollOffset >= overflow)
            {
                _scrollOffset = overflow;
                _isPingPongReversed = true;
                _pauseTimer = animationDelay;
            }
            else if (_scrollOffset <= 0f)
            {
                _scrollOffset = 0f;
                _isPingPongReversed = false;
                _pauseTimer = animationDelay;
                FireScrollCompleted();

                if (tapToScroll)
                    _tapTriggered = false;
            }
        }

        private void UpdateOneShot(float dt, float preferredWidth)
        {
            var totalDistance = preferredWidth + trailingBuffer;
            var effectiveSpeed = GetEffectiveSpeed(totalDistance);
            var scrollDir = direction == MarqueeDirection.Left ? 1f : -1f;

            _scrollOffset += effectiveSpeed * dt * scrollDir;

            if (Mathf.Abs(_scrollOffset) >= totalDistance)
            {
                _oneShotComplete = true;
                FireScrollCompleted();

                if (tapToScroll)
                    _tapTriggered = false;
            }
        }

        private void FireScrollCompleted()
        {
            _wasScrolling = false;
            OnScrollCompleted();
            ScrollCompleted?.Invoke();
        }

        private void ApplyScroll()
        {
            if (!_tmpText) return;

            if (_cachedMeshInfo == null)
            {
                _tmpText.ForceMeshUpdate();
                var info = _tmpText.textInfo;
                if (info == null || info.characterCount == 0) return;
                _cachedMeshInfo = info.CopyMeshInfoVertexData();
            }

            var textInfo = _tmpText.textInfo;
            if (textInfo.characterCount == 0) return;

            var visualOffset = GetVisualOffset();
            var offset = new Vector3(-visualOffset, 0f, 0f);

            for (var i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var sourceVertices = _cachedMeshInfo[i].vertices;
                var destVertices = textInfo.meshInfo[i].vertices;

                for (var j = 0; j < sourceVertices.Length; j++)
                    destVertices[j] = sourceVertices[j] + offset;
            }

            if (behavior == MarqueeBehavior.Continuous)
                ApplyContinuousWrap(textInfo, GetRectWidth(), GetPreferredWidth());

            _tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            UpdateClipping();
        }

        private float GetVisualOffset()
        {
            if (behavior == MarqueeBehavior.Continuous)
                return _scrollOffset;

            var max = GetMaxScrollOffset();
            if (max <= 0f)
                return _scrollOffset;

            var progress = Mathf.Clamp01(Mathf.Abs(_scrollOffset) / max);
            var curved = animationCurve.Evaluate(progress);

            if (behavior == MarqueeBehavior.PingPong)
            {
                // Left: 0..max (starts showing beginning, scrolls left to reveal end)
                // Right: max..0 (starts showing end, first visible movement is rightward)
                return direction == MarqueeDirection.Right
                    ? (1f - curved) * max
                    : curved * max;
            }

            // OneShot encodes direction in the offset sign.
            return curved * max * Mathf.Sign(_scrollOffset);
        }

        private void ApplyContinuousWrap(TMP_TextInfo textInfo, float rectWidth, float preferredWidth)
        {
            var loopDistance = preferredWidth + GetLoopGap();

            var scrollingLeft = direction == MarqueeDirection.Left;
            var leftBuffer = scrollingLeft ? leadingBuffer : trailingBuffer;
            var rightBuffer = scrollingLeft ? trailingBuffer : leadingBuffer;

            for (var i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                var meshIndex = charInfo.materialReferenceIndex;
                var vertexIndex = charInfo.vertexIndex;
                var vertices = textInfo.meshInfo[meshIndex].vertices;

                var minX = float.MaxValue;
                var maxX = float.MinValue;
                for (var v = 0; v < 4; v++)
                {
                    var x = vertices[vertexIndex + v].x;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                }

                float wrapSign;
                if (maxX < -leftBuffer)
                    wrapSign = 1f;
                else if (minX > rectWidth + rightBuffer)
                    wrapSign = -1f;
                else
                    continue;

                var wrapX = loopDistance * wrapSign;
                for (var v = 0; v < 4; v++)
                    vertices[vertexIndex + v].x += wrapX;
            }
        }

        private void UpdateClipping()
        {
            if (!_canvasRenderer || !_rootCanvas) return;

            var rect = _rectTransform.rect;
            var margin = _tmpText ? _tmpText.margin : Vector4.zero;

            // Horizontal clip only - expand vertical bounds to avoid clipping descenders/ascenders.
            var bottomLeft = new Vector3(rect.xMin + margin.x, rect.yMin - rect.height, 0f);
            var topRight = new Vector3(rect.xMax - margin.z, rect.yMax + rect.height, 0f);

            var canvasTransform = _rootCanvas.transform;
            var blCanvas = canvasTransform.InverseTransformPoint(
                _rectTransform.TransformPoint(bottomLeft));
            var trCanvas = canvasTransform.InverseTransformPoint(
                _rectTransform.TransformPoint(topRight));

            _canvasRenderer.EnableRectClipping(new Rect(
                blCanvas.x, blCanvas.y,
                trCanvas.x - blCanvas.x,
                trCanvas.y - blCanvas.y));

            _canvasRenderer.clippingSoftness = new Vector2(fadeEdges ? fadeWidth : 0f, 0f);
        }

        private void ResetState()
        {
            _scrollOffset = 0f;
            _pauseTimer = 0f;
            _isPingPongReversed = false;
            _oneShotComplete = false;
            _tapTriggered = false;
            _wasScrolling = false;
        }

        private void CacheComponents()
        {
            if (!_tmpText)
                _tmpText = GetComponent<TMP_Text>();
            if (!_rectTransform)
                _rectTransform = GetComponent<RectTransform>();
            if (!_canvasRenderer)
                _canvasRenderer = GetComponent<CanvasRenderer>();
            if (!_rootCanvas)
                _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        }

        private void EnsureOverflowMode()
        {
            if (!_tmpText) return;

            if (_tmpText.overflowMode != TextOverflowModes.Overflow)
                _tmpText.overflowMode = TextOverflowModes.Overflow;

            if (_tmpText.enableWordWrapping)
                _tmpText.enableWordWrapping = false;

            if (_tmpText.vertexBufferAutoSizeReduction)
                _tmpText.vertexBufferAutoSizeReduction = false;
        }

        private void OnTextChanged(UnityEngine.Object obj)
        {
            if (obj != _tmpText) return;
            _hasTextChanged = true;
            _cachedMeshInfo = null;
        }

        private float GetEffectiveSpeed(float totalDistance)
        {
            if (speedMode == SpeedMode.Duration && speed > 0f)
                return totalDistance / speed;
            return speed;
        }

        private float GetMaxScrollOffset()
        {
            var preferredWidth = GetPreferredWidth();

            return behavior switch
            {
                MarqueeBehavior.PingPong => Mathf.Max(0f, preferredWidth - GetRectWidth()),
                MarqueeBehavior.OneShot => preferredWidth + trailingBuffer,
                _ => preferredWidth + GetLoopGap()
            };
        }

        private float GetLoopGap()
        {
            return leadingBuffer + trailingBuffer;
        }

        private float GetPreferredWidth()
        {
            if (_hasTextChanged)
            {
                _cachedPreferredWidth = _tmpText ? _tmpText.preferredWidth : 0f;
                _hasTextChanged = false;
            }
            return _cachedPreferredWidth;
        }

        private float GetRectWidth()
        {
            return _rectTransform ? _rectTransform.rect.width : 0f;
        }

        private float GetDeltaTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return Time.deltaTime > 0f ? Time.deltaTime : FallbackDeltaTime;
#endif
            return Time.deltaTime;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            speed = Mathf.Max(0f, speed);
            leadingBuffer = Mathf.Max(0f, leadingBuffer);
            trailingBuffer = Mathf.Max(0f, trailingBuffer);
            animationDelay = Mathf.Max(0f, animationDelay);

            CacheComponents();
            _hasTextChanged = true;
            _cachedMeshInfo = null;
        }
#endif
    }
}
