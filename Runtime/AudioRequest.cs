using System.Collections.Generic;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Fluent builder around <see cref="AudioPlaybackRequest"/>.
    /// <code>
    /// audioService
    ///   .CreateRequest(AudioIds.Explosion)
    ///   .WithVolume(0.8f)
    ///   .WithPitch(1.1f)
    ///   .WithMods(myRandomPitch)
    ///   .Play();
    /// </code>
    /// Builders are cheap to allocate (struct holding lists only when needed).
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioRequest")]
    public sealed class AudioRequest
    {
        private readonly IAudioService _service;
        private AudioPlaybackRequest _request;

        public AudioRequest(IAudioService service, AudioId id)
        {
            _service = service;
            _request = AudioPlaybackRequest.ForId(id);
        }

        public AudioRequest WithVolume(float volumeScale)
        {
            _request.VolumeScale = volumeScale;
            return this;
        }

        public AudioRequest WithPitch(float pitch)
        {
            _request.PitchOverride = pitch;
            return this;
        }

        public AudioRequest WithLoop(bool loop)
        {
            _request.LoopOverride = loop;
            return this;
        }

        public AudioRequest At(Vector3 worldPosition)
        {
            _request.WorldPosition = worldPosition;
            return this;
        }

        public AudioRequest With(ISoundOption option)
        {
            if (option == null) return this;
            _request.Options ??= new List<ISoundOption>(2);
            _request.Options.Add(option);
            return this;
        }

        public AudioRequest With(params ISoundOption[] options)
        {
            if (options == null) return this;
            _request.Options ??= new List<ISoundOption>(options.Length);
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null) _request.Options.Add(options[i]);
            }
            return this;
        }

        public AudioRequest WithMods(ISoundModulator modulator)
        {
            if (modulator == null) return this;
            _request.Modulators ??= new List<ISoundModulator>(2);
            _request.Modulators.Add(modulator);
            return this;
        }

        public AudioRequest WithMods(params ISoundModulator[] modulators)
        {
            if (modulators == null) return this;
            _request.Modulators ??= new List<ISoundModulator>(modulators.Length);
            for (int i = 0; i < modulators.Length; i++)
            {
                if (modulators[i] != null) _request.Modulators.Add(modulators[i]);
            }
            return this;
        }

        public AudioPlaybackResult Play()
        {
            return _service.Play(in _request);
        }
    }
}
