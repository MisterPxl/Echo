using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Sets <see cref="AudioSource.bypassListenerEffects"/>. Bypasses any effect chain
    /// attached to the AudioListener — typical use case: UI sounds that must remain
    /// audible regardless of the camera being underwater / inside a tunnel filter.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "BypassListenerEffectsOption")]
    public sealed class BypassListenerEffectsOption : SoundOptionBase
    {
        [SerializeField] private bool _bypass = true;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.bypassListenerEffects = _bypass;
        }
    }
}
