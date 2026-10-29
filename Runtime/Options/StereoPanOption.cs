using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Sets <see cref="AudioSource.panStereo"/>: -1 = full left, 0 = center, 1 = full right.
    /// Only meaningful for 2D / mono sources.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "StereoPanOption")]
    public sealed class StereoPanOption : SoundOptionBase
    {
        [SerializeField, Range(-1f, 1f)] private float _pan;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.panStereo = _pan;
        }
    }
}
