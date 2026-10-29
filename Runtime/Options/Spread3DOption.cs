using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Sets <see cref="AudioSource.spread"/> in degrees (0–360).
    /// Wider spreads make a 3D sound feel more enveloping (good for ambient sources),
    /// while a low spread sharpens its directional cue.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "Spread3DOption")]
    public sealed class Spread3DOption : SoundOptionBase
    {
        [SerializeField, Range(0f, 360f)] private float _spread;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.spread = _spread;
        }
    }
}
