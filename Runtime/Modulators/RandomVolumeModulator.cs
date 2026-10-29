using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Multiplies the source volume by a random factor in <c>[Min, Max]</c>.
    /// Combine multiplicatively with the resolved <see cref="AudioPlaybackContext.EffectiveVolume"/>:
    /// useful to humanize repeating SFX (footsteps, hits, UI clicks) without changing the mix balance.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "RandomVolumeModulator")]
    public sealed class RandomVolumeModulator : SoundModulatorBase
    {
        [SerializeField, Range(0f, 2f)] private float _min = 0.85f;
        [SerializeField, Range(0f, 2f)] private float _max = 1f;

        public override void Modulate(AudioSource source, AudioPlaybackContext context)
        {
            float min = Mathf.Min(_min, _max);
            float max = Mathf.Max(_min, _max);
            float multiplier = UnityEngine.Random.Range(min, max);
            source.volume = Mathf.Clamp(source.volume * multiplier, 0f, 1f);
        }
    }
}
