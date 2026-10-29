using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Runtime volume state. All values are clamped to [0, 1].
    /// Effective bus volumes are <c>master * channel</c>.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioVolumeSettings")]
    public sealed class AudioVolumeSettings
    {
        private float _master = 1f;
        private float _music = 1f;
        private float _sfx = 1f;
        private float _ambient = 1f;

        public float Master  { get => _master;  set => _master  = Mathf.Clamp01(value); }
        public float Music   { get => _music;   set => _music   = Mathf.Clamp01(value); }
        public float Sfx     { get => _sfx;     set => _sfx     = Mathf.Clamp01(value); }
        public float Ambient { get => _ambient; set => _ambient = Mathf.Clamp01(value); }

        public float EffectiveMusic   => _master * _music;
        public float EffectiveSfx     => _master * _sfx;
        public float EffectiveAmbient => _master * _ambient;

        public float EffectiveFor(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Music   => EffectiveMusic,
                AudioCategory.Sfx     => EffectiveSfx,
                AudioCategory.Ambient => EffectiveAmbient,
                AudioCategory.Master  => _master,
                _                     => _master
            };
        }
    }
}
