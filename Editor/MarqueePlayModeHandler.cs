using UnityEditor;

namespace IrakliChkuaseli.UI.Marquee.Editor
{
    [InitializeOnLoad]
    internal static class MarqueePlayModeHandler
    {
        static MarqueePlayModeHandler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            foreach (var marquee in MarqueeText.ActiveInstances)
            {
                if (marquee && marquee.IsPlaying)
                    marquee.Stop();
            }
        }
    }
}
