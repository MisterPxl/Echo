using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Sets <see cref="AudioSource.bypassReverbZones"/>. Useful to keep dialog or UI dry
    /// even when the listener stands inside a reverb zone designed for gameplay SFX.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "BypassReverbZonesOption")]
    public sealed class BypassReverbZonesOption : SoundOptionBase
    {
        [SerializeField] private bool _bypass = true;

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.bypassReverbZones = _bypass;
        }
    }
}
