using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Incremental pitch modulator: each successive playback raises the source pitch
    /// by <see cref="_step"/> until <see cref="_maxSteps"/> is reached, then resets.
    /// Useful for combo / repeated UI feedback (button mash, climbing arpeggios…).
    ///
    /// <para>State is per-modulator-instance — share the same modulator instance
    /// across calls if you want the increment to persist.</para>
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "IncrementalPitchModulator")]
    public sealed class IncrementalPitchModulator : SoundModulatorBase
    {
        [SerializeField, Range(0.01f, 0.5f)] private float _step = 0.05f;
        [SerializeField, Min(1)] private int _maxSteps = 8;
        [SerializeField] private float _resetCooldownSeconds = 0.4f;

        [NonSerialized] private int _currentStep;
        [NonSerialized] private float _lastPlayTime;

        public override void Modulate(AudioSource source, AudioPlaybackContext context)
        {
            float now = Time.unscaledTime;
            if (now - _lastPlayTime > _resetCooldownSeconds)
                _currentStep = 0;

            float multiplier = 1f + _step * _currentStep;
            source.pitch = context.BasePitch * multiplier;

            _currentStep++;
            if (_currentStep >= _maxSteps)
                _currentStep = 0;

            _lastPlayTime = now;
        }
    }
}
