using System;
using System.Collections.Generic;
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
        public IReadOnlyList<AudioBusAsset> Buses => _service.Buses;
        public float GetVolume(AudioBusAsset bus) => _service.GetVolume(bus);
        public void SetVolume(AudioBusAsset bus, float volume) => _service.SetVolume(bus, volume);
        public bool IsPlaying(AudioBusAsset bus) => _service.IsPlaying(bus);
        public AudioPlaybackResult Play(AudioId id) => _service.Play(id);
        public AudioPlaybackResult Play(in AudioPlaybackRequest request) => _service.Play(in request);
        public AudioRequest CreateRequest(AudioId id) => _service.CreateRequest(id);
        public AudioPlaybackResult PlaySfx(AudioClip clip, float volumeScale = 1f) => _playSfx(clip, volumeScale);
        public AudioPlaybackResult Stop(AudioBusAsset bus) => _service.Stop(bus);
    }
}
