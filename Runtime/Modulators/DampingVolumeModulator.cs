using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Attenuates the source volume when the same modulator is triggered repeatedly within a short window.
    /// Each call within <see cref="_windowSeconds"/> increases the damping factor (volume *= damp^n).
    /// After the window passes without triggering, the count resets.
    ///
    /// <para>Use case: rapid-fire SFX (gunfire, particle hits, UI spam) that should taper off naturally
    /// instead of stacking into a wall of noise.</para>
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "DampingVolumeModulator")]
    public sealed class DampingVolumeModulator : SoundModulatorBase
    {
        [SerializeField] private float _windowSeconds = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _dampPerHit = 0.85f;
        [SerializeField, Min(1)] private int _maxStacks = 6;

        [NonSerialized] private int _currentStacks;
        [NonSerialized] private float _lastTriggerTime;

        public override void Modulate(AudioSource source, AudioPlaybackContext context)
        {
            float now = Time.unscaledTime;
            if (now - _lastTriggerTime > _windowSeconds)
                _currentStacks = 0;

            // damp = _dampPerHit^_currentStacks (1 → no change, then attenuates).
            float damp = Mathf.Pow(_dampPerHit, _currentStacks);
            source.volume = Mathf.Clamp01(source.volume * damp);

            _lastTriggerTime = now;
            if (_currentStacks < _maxStacks) _currentStacks++;
        }
    }
}
