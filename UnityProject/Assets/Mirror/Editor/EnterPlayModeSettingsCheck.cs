// Unity 2019.3 has an experimental 'disable domain reload on play'
// feature. keeping any global state between sessions will break
// Mirror and most of our user's projects. don't allow it for now.
// https://blogs.unity3d.com/2019/11/05/enter-play-mode-faster-in-unity-2019-3/
using UnityEditor;
using UnityEngine;

namespace Mirror
{
    public class EnterPlayModeSettingsCheck : MonoBehaviour
    {
        [InitializeOnLoadMethod]
        static void OnInitializeOnLoad()
        {
#if UNITY_2019_3_OR_NEWER
            // We can't support experimental "Enter Play Mode Options" mode
            // Check immediately on load, and before entering play mode, and warn the user
            CheckPlayModeOptions();
#endif

            // Hook this event to see if we have a good weave every time
            // user attempts to enter play mode or tries to do a build
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Per Unity docs, this fires "when exiting edit mode before the Editor is in play mode".
            // This doesn't fire when closing the editor.
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                CheckSuccessfulWeave();

#if UNITY_2019_3_OR_NEWER
                // We can't support experimental "Enter Play Mode Options" mode
                // Check and prevent entering play mode if enabled
                CheckPlayModeOptions();
#endif
            }
        }

        static void CheckSuccessfulWeave()
        {
            // Check if last weave result was successful
            if (!SessionState.GetBool("MIRROR_WEAVE_SUCCESS", false))
            {
                // Last weave result was a failure...try to weave again
                // Faults will show in the console that may have been cleared by "Clear on Play"
                SessionState.SetBool("MIRROR_WEAVE_SUCCESS", true);
                Weaver.CompilationFinishedHook.WeaveExistingAssemblies();

                // Did that clear things up for us?
                if (!SessionState.GetBool("MIRROR_WEAVE_SUCCESS", false))
                {
                    // Nope, still failed, and console has the issues logged
                    Debug.LogError("Can't enter play mode until weaver issues are resolved.");
                    EditorApplication.isPlaying = false;
                }
            }
        }

#if UNITY_2019_3_OR_NEWER
        static void CheckPlayModeOptions()
        {
            // enabling the checkbox is enough. it controls all the other settings.
            // if (EditorSettings.enterPlayModeOptionsEnabled)
            // {
            //     Debug.LogError("Enter Play Mode Options are not supported by Mirror. Please disable 'ProjectSettings -> Editor -> Enter Play Mode Settings (Experimental)'.");
            //     EditorApplication.isPlaying = false;
            // }
        }
#endif
    }
}
