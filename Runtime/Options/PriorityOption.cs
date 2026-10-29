using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Sets <see cref="AudioSource.priority"/>. Range is 0 (highest priority, never culled
    /// by the audio engine) to 256 (lowest, first to be dropped under voice limits).
    /// Music and critical UI typically sit at 0–32, ambient SFX at 128+.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "PriorityOption")]
    public sealed class PriorityOption : SoundOptionBase
    {
        [SerializeField, Range(0, 256)] private int _priority = 128;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.priority = _priority;
        }
    }
}
