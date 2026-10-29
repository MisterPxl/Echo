using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>Adapts an existing Game audio implementation without changing its class declaration.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "RawClipAudioServiceAdapter")]
    public sealed class RawClipAudioServiceAdapter : IAudioService, IRawClipAudioService
    {
        private readonly IAudioService _service;
        private readonly Func<AudioClip, float, AudioPlaybackResult> _playSfx;
        public RawClipAudioServiceAdapter(IAudioService service, Func<AudioClip, float, AudioPlaybackResult> playSfx)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _playSfx = playSfx ?? throw new ArgumentNullException(nameof(playSfx));
        }
        public float MasterVolume { get => _service.MasterVolume; set => _service.MasterVolume = value; }
        public float MusicVolume { get => _service.MusicVolume; set => _service.MusicVolume = value; }
        public float SfxVolume { get => _service.SfxVolume; set => _service.SfxVolume = value; }
        public float AmbientVolume { get => _service.AmbientVolume; set => _service.AmbientVolume = value; }
        public bool IsMusicPlaying => _service.IsMusicPlaying;
        public bool IsAmbientPlaying => _service.IsAmbientPlaying;
        public void SetVolume(AudioCategory category, float volume) => _service.SetVolume(category, volume);
        public float GetVolume(AudioCategory category) => _service.GetVolume(category);
        public AudioPlaybackResult Play(AudioId id) => _service.Play(id);
        public AudioPlaybackResult Play(in AudioPlaybackRequest request) => _service.Play(in request);
        public AudioRequest CreateRequest(AudioId id) => _service.CreateRequest(id);
        public AudioPlaybackResult PlaySfx(AudioClip clip, float volumeScale = 1f) => _playSfx(clip, volumeScale);
        public AudioPlaybackResult StopMusic() => _service.StopMusic();
        public AudioPlaybackResult StopAmbient() => _service.StopAmbient();
        public void StopAllSfx() => _service.StopAllSfx();
    }
}
