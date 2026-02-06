using TMPro;
using UnityEditor;
using UnityEngine;

namespace IrakliChkuaseli.UI.Marquee.Editor
{
    [CustomEditor(typeof(MarqueeText))]
    public class MarqueeTextEditor : UnityEditor.Editor
    {
        private SerializedProperty _directionProp;
        private SerializedProperty _behaviorProp;
        private SerializedProperty _speedModeProp;
        private SerializedProperty _speedProp;
        private SerializedProperty _animationCurveProp;
        private SerializedProperty _animationDelayProp;
        private SerializedProperty _leadingBufferProp;
        private SerializedProperty _trailingBufferProp;
        private SerializedProperty _forceScrollingProp;
        private SerializedProperty _holdScrollingProp;
        private SerializedProperty _tapToScrollProp;
        private SerializedProperty _fadeEdgesProp;
        private SerializedProperty _fadeWidthProp;
        private SerializedProperty _previewInEditorProp;
        private TMP_Text _tmpText;

        private void OnEnable()
        {
            _tmpText = ((MarqueeText)target).GetComponent<TMP_Text>();
            _directionProp = serializedObject.FindProperty("direction");
            _behaviorProp = serializedObject.FindProperty("behavior");
            _speedModeProp = serializedObject.FindProperty("speedMode");
            _speedProp = serializedObject.FindProperty("speed");
            _animationCurveProp = serializedObject.FindProperty("animationCurve");
            _animationDelayProp = serializedObject.FindProperty("animationDelay");
            _leadingBufferProp = serializedObject.FindProperty("leadingBuffer");
            _trailingBufferProp = serializedObject.FindProperty("trailingBuffer");
            _forceScrollingProp = serializedObject.FindProperty("forceScrolling");
            _holdScrollingProp = serializedObject.FindProperty("holdScrolling");
            _tapToScrollProp = serializedObject.FindProperty("tapToScroll");
            _fadeEdgesProp = serializedObject.FindProperty("fadeEdges");
            _fadeWidthProp = serializedObject.FindProperty("fadeWidth");
            _previewInEditorProp = serializedObject.FindProperty("previewInEditor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var marquee = (MarqueeText)target;

            DrawPlaybackControls(marquee);
            EditorGUILayout.Space(8);

            DrawScrollSettings();
            EditorGUILayout.Space(5);

            DrawSpacingSettings();
            EditorGUILayout.Space(5);

            DrawBehaviorSettings();
            EditorGUILayout.Space(5);

            DrawFadeSettings();
            DrawValidationWarnings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPlaybackControls(MarqueeText marquee)
        {
            var isEditMode = !Application.isPlaying;

            if (isEditMode)
            {
                EditorGUILayout.PropertyField(_previewInEditorProp, new GUIContent("Live Preview"));

                if (!_previewInEditorProp.boolValue)
                    return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Playback");

            var isPlaying = marquee.IsPlaying;
            var buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = isPlaying ? FontStyle.Bold : FontStyle.Normal,
                fixedWidth = 70
            };

            var originalColor = GUI.backgroundColor;
            if (isPlaying)
                GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);

            if (GUILayout.Button(isPlaying ? "Playing" : "Paused", buttonStyle))
            {
                if (isPlaying) marquee.Pause();
                else marquee.Play();
                EditorUtility.SetDirty(target);
            }

            GUI.backgroundColor = originalColor;

            if (GUILayout.Button("Restart", EditorStyles.miniButton, GUILayout.Width(55)))
            {
                marquee.Restart();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Stop", EditorStyles.miniButton, GUILayout.Width(40)))
            {
                marquee.Stop();
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.EndHorizontal();

            var status = marquee.IsOverflowing ? "overflowing" : "fits";
            if (marquee.AwayFromHome)
                status += $" | progress: {marquee.ScrollProgress:P0}";
            EditorGUILayout.LabelField("Status", status, EditorStyles.miniLabel);
        }

        private void DrawScrollSettings()
        {
            EditorGUILayout.LabelField("Scroll", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_directionProp);
            EditorGUILayout.PropertyField(_behaviorProp);
            EditorGUILayout.Space(2);

            EditorGUILayout.PropertyField(_speedModeProp, new GUIContent("Speed Mode"));
            var speedLabel = _speedModeProp.enumValueIndex == (int)SpeedMode.Rate
                ? "Speed"
                : "Duration (sec)";
            EditorGUILayout.PropertyField(_speedProp, new GUIContent(speedLabel));
            EditorGUILayout.PropertyField(_animationDelayProp, new GUIContent("Delay at Edge (sec)"));

            if ((MarqueeBehavior)_behaviorProp.enumValueIndex != MarqueeBehavior.Continuous)
                EditorGUILayout.PropertyField(_animationCurveProp, new GUIContent("Easing Curve"));

            EditorGUI.indentLevel--;
        }

        private void DrawSpacingSettings()
        {
            EditorGUILayout.LabelField("Spacing", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_leadingBufferProp, new GUIContent("Leading Buffer"));
            EditorGUILayout.PropertyField(_trailingBufferProp, new GUIContent("Trailing Buffer"));
            EditorGUI.indentLevel--;
        }

        private void DrawBehaviorSettings()
        {
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_forceScrollingProp,
                new GUIContent("Force Scrolling", "Scroll even when text fits within bounds"));
            EditorGUILayout.PropertyField(_holdScrollingProp,
                new GUIContent("Hold Scrolling", "Prevent scrolling but keep edge fades visible"));
            EditorGUILayout.PropertyField(_tapToScrollProp,
                new GUIContent("Tap to Scroll", "Only scroll when clicked/tapped"));
            EditorGUI.indentLevel--;
        }

        private void DrawFadeSettings()
        {
            EditorGUILayout.LabelField("Fade Edges", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_fadeEdgesProp, new GUIContent("Enable"));

            if (_fadeEdgesProp.boolValue)
                EditorGUILayout.PropertyField(_fadeWidthProp, new GUIContent("Fade Width"));

            EditorGUI.indentLevel--;
        }

        private void DrawValidationWarnings()
        {
            if (!_tmpText) return;

            if (_tmpText.overflowMode != TextOverflowModes.Overflow)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "TMP overflow mode will be set to Overflow automatically when enabled.",
                    MessageType.Info);
            }

            if (_tmpText.enableWordWrapping)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Word wrapping will be disabled automatically. Marquee scrolls a single line.",
                    MessageType.Info);
            }
        }
    }
}
