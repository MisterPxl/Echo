using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Sets <see cref="AudioSource.dopplerLevel"/> (0 = no doppler, 1 = full doppler).
    /// Often disabled on UI / music to avoid pitch artifacts when the listener is moving.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "DopplerOption")]
    public sealed class DopplerOption : SoundOptionBase
    {
        [SerializeField, Range(0f, 5f)] private float _dopplerLevel;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.dopplerLevel = _dopplerLevel;
        }
    }
}
