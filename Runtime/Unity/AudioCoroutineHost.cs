using System.Collections;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Lazy-initialized MonoBehaviour singleton hosting coroutines used by audio options
    /// (FadeIn, future fade-out, ducking helpers...). Lives under the persistent app root
    /// when one exists, otherwise as its own DontDestroyOnLoad object.
    ///
    /// <para>This is intentionally NOT the audio service entry point — it's a tiny utility
    /// strictly for coroutine plumbing. End users should never reference it directly.</para>
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioCoroutineHost")]
    public sealed class AudioCoroutineHost : MonoBehaviour
    {
        private static AudioCoroutineHost _instance;

        public static AudioCoroutineHost Instance
        {
            get
            {
                if (_instance != null) return _instance;
                GameObject go = new GameObject("AudioCoroutineHost");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<AudioCoroutineHost>();
                return _instance;
            }
        }

        public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);
        public void Cancel(Coroutine routine) { if (routine != null) StopCoroutine(routine); }
    }
}
