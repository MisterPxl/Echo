using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Defers the source playback by <see cref="_delaySeconds"/> using
    /// <see cref="AudioSource.PlayDelayed"/>. Sets <see cref="AudioPlaybackContext.SuppressDefaultPlay"/>
    /// so the AudioService does not call <see cref="AudioSource.Play"/> right after, which
    /// would override the scheduled delay.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "DelayedPlayOption")]
    public sealed class DelayedPlayOption : SoundOptionBase
    {
        [SerializeField, Min(0f)] private float _delaySeconds = 0.1f;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.PlayDelayed(_delaySeconds);
            context.SuppressDefaultPlay = true;
        }
    }
}
