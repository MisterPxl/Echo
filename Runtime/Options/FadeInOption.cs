using System;
using System.Collections;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Starts the source at volume 0 and fades up to its resolved volume over
    /// <see cref="_durationSeconds"/>. The fade is driven by a coroutine running
    /// on <see cref="AudioCoroutineHost"/> — no MonoBehaviour required at the call site.
    ///
    /// <para>If the source is stopped or its clip is replaced before the fade ends,
    /// the coroutine self-cancels (it polls <c>source.isPlaying</c> each frame).</para>
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "FadeInOption")]
    public sealed class FadeInOption : SoundOptionBase
    {
        [SerializeField, Min(0.01f)] private float _durationSeconds = 0.5f;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            float targetVolume = source.volume;
            source.volume = 0f;
            AudioCoroutineHost.Instance.Run(FadeRoutine(source, targetVolume, _durationSeconds));
        }

        private static IEnumerator FadeRoutine(AudioSource source, float targetVolume, float duration)
        {
            float elapsed = 0f;
            // Wait one frame so the source.Play() called by AudioService has actually started.
            yield return null;

            while (elapsed < duration && source != null)
            {
                if (!source.isPlaying) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            if (source != null) source.volume = targetVolume;
        }
    }
}
