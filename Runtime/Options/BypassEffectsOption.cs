using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Sets <see cref="AudioSource.bypassEffects"/>. Disabling the effect chain on a per-clip
    /// basis is useful for UI sounds that should never be processed by gameplay-side effects
    /// attached to the source (low-pass under water, distortion, etc.).
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "BypassEffectsOption")]
    public sealed class BypassEffectsOption : SoundOptionBase
    {
        [SerializeField] private bool _bypass = true;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.bypassEffects = _bypass;
        }
    }
}
