using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Vetoes the playback if it occurs less than <see cref="_cooldownSeconds"/> after the
    /// previous successful trigger of the same modulator instance.
    /// Useful to prevent UI/SFX spam without rate-limiting the call site.
    ///
    /// <para>Time source: <see cref="Time.unscaledTime"/> — pause-friendly.</para>
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "CooldownModulator")]
    public sealed class CooldownModulator : SoundModulatorBase
    {
        [SerializeField, Min(0f)] private float _cooldownSeconds = 0.1f;

        [NonSerialized] private float _lastTriggerTime = float.NegativeInfinity;

        public override void Modulate(AudioSource source, AudioPlaybackContext context)
        {
            float now = Time.unscaledTime;
            if (now - _lastTriggerTime < _cooldownSeconds)
            {
                context.Veto($"cooldown ({_cooldownSeconds:F2}s)");
                return;
            }
            _lastTriggerTime = now;
        }
    }
}
