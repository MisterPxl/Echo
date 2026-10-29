using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Overrides the source's distance rolloff: mode + min/max distance, with an optional
    /// custom <see cref="AnimationCurve"/> when <see cref="_mode"/> is set to
    /// <see cref="AudioRolloffMode.Custom"/>.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "RolloffOverrideOption")]
    public sealed class RolloffOverrideOption : SoundOptionBase
    {
        [SerializeField] private AudioRolloffMode _mode = AudioRolloffMode.Logarithmic;
        [SerializeField, Min(0f)] private float _minDistance = 1f;
        [SerializeField, Min(0f)] private float _maxDistance = 500f;

        [Tooltip("Only used when Mode == Custom. X = normalized distance [0..1], Y = volume.")]
        [SerializeField] private AnimationCurve _customCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        public override void Apply(AudioSource source, AudioPlaybackContext context)
        {
            source.rolloffMode = _mode;
            source.minDistance = _minDistance;
            source.maxDistance = _maxDistance;

            if (_mode == AudioRolloffMode.Custom && _customCurve != null && _customCurve.length > 0)
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, _customCurve);
        }
    }
}
