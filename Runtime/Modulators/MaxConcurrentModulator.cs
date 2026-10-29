using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Vetoes the playback if at least <see cref="_maxConcurrent"/> instances of this same
    /// modulator are still considered "active" (i.e. their estimated end time is in the future).
    ///
    /// <para>The modulator tracks playback end times locally — it does not hook into
    /// AudioSources. End time is approximated as <c>Time.unscaledTime + clip.length / pitch</c>
    /// at trigger, which is enough for typical short SFX. For looping sounds, this modulator
    /// behaves as if every active loop counts forever — use a different throttling strategy
    /// for loops.</para>
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "MaxConcurrentModulator")]
    public sealed class MaxConcurrentModulator : SoundModulatorBase
    {
        [SerializeField, Min(1)] private int _maxConcurrent = 3;

        [NonSerialized] private List<float> _endTimes;

        public override void Modulate(AudioSource source, AudioPlaybackContext context)
        {
            _endTimes ??= new List<float>(_maxConcurrent);

            float now = Time.unscaledTime;
            // Drop expired entries.
            for (int i = _endTimes.Count - 1; i >= 0; i--)
            {
                if (_endTimes[i] <= now) _endTimes.RemoveAt(i);
            }

            if (_endTimes.Count >= _maxConcurrent)
            {
                context.Veto($"max concurrent ({_maxConcurrent})");
                return;
            }

            AudioClip clip = context.ClipOverride != null ? context.ClipOverride : source.clip;
            float pitch = Mathf.Max(0.01f, source.pitch);
            float duration = clip != null ? clip.length / pitch : 0f;
            _endTimes.Add(now + duration);
        }
    }
}
