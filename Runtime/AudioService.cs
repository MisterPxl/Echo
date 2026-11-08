using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Astra.Echo
{
    /// <summary>
    /// Main implementation of <see cref="IAudioService"/>.
    /// Resolves <see cref="AudioId"/> through an <see cref="IAudioDatabase"/> and plays the definition
    /// on the channel of its <see cref="AudioBusAsset"/>, built by the bus <see cref="AudioBusPlayback"/>
    /// (single replaced track, pooled one-shots, or a project behaviour). A bus is created the first
    /// time it is met; <see cref="AudioServiceSettings.Buses"/> only prepares channels ahead of time.
    /// Applies the definition's modulators, then any caller-side modulators / options.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioService")]
    public sealed class AudioService : IAudioService, IRawClipAudioService, IDisposable
    {
        private readonly Dictionary<AudioBusAsset, BusState> _states = new Dictionary<AudioBusAsset, BusState>();
        private readonly List<AudioBusAsset> _buses = new List<AudioBusAsset>();
        private readonly IEchoLogger _logger;
        private readonly bool _verbose;

        private IAudioDatabase _database;
        private AudioRoot _root;
        private AudioBusAsset _rawClipBus;
        private float _master = 1f;

        public float MasterVolume
        {
            get => _master;
            set { _master = Mathf.Clamp01(value); ApplyChannelVolumes(); }
        }

        public IReadOnlyList<AudioBusAsset> Buses => _buses;

        public AudioService(IEchoLogger logger = null, bool verbose = false)
        {
            _logger = logger;
            _verbose = verbose;
        }

        /// <summary>
        /// Builds the <see cref="AudioRoot"/> and the channel of every configured bus, wires the database
        /// and the raw clip bus, and resets volumes to Master and bus defaults. Replaces any root
        /// previously created by this service.
        /// </summary>
        /// <param name="settings">Initialisation settings; must not be null.</param>
        /// <param name="parent">Optional parent for the root. When null the root persists across scenes.</param>
        public void Initialize(AudioServiceSettings settings, Transform parent = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            DestroyRoot();
            _states.Clear();
            _buses.Clear();

            _root = AudioRoot.Create(settings.RootName, parent);
            _database = settings.Database;
            _rawClipBus = settings.RawClipBus;
            _master = Mathf.Clamp01(settings.MasterVolume);

            if (settings.Buses != null)
            {
                for (int i = 0; i < settings.Buses.Count; i++)
                {
                    if (settings.Buses[i] != null) PrepareChannel(settings.Buses[i]);
                }
            }

            if (_rawClipBus != null) PrepareChannel(_rawClipBus);

            LogVerbose(() => $"Initialized — root: '{settings.RootName}', buses: {DescribeBuses()}.");
        }

        private void PrepareChannel(AudioBusAsset bus)
        {
            if (ChannelFor(StateFor(bus), out string failure) == null)
                _logger?.Warn(failure);
        }

        /// <summary>Destroys the <see cref="AudioRoot"/> created by <see cref="Initialize"/>, if any.</summary>
        public void Dispose()
        {
            DestroyRoot();
            _database = null;
        }

        private void DestroyRoot()
        {
            if (_root != null)
                UnityEngine.Object.Destroy(_root.gameObject);

            _root = null;
            foreach (BusState state in _states.Values)
                state.Channel = null;
        }

        public float GetVolume(AudioBusAsset bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            return _states.TryGetValue(bus, out BusState state) ? state.Volume : bus.DefaultVolume;
        }

        public void SetVolume(AudioBusAsset bus, float volume)
        {
            BusState state = StateFor(bus);
            state.Volume = Mathf.Clamp01(volume);
            state.Channel?.OnVolumeChanged(EffectiveVolume(state));
        }

        public bool IsPlaying(AudioBusAsset bus)
        {
            return bus != null
                && _states.TryGetValue(bus, out BusState state)
                && state.Channel != null
                && state.Channel.IsPlaying;
        }

        public AudioRequest CreateRequest(AudioId id) => new AudioRequest(this, id);

        public AudioPlaybackResult Play(AudioId id)
        {
            return Play(AudioPlaybackRequest.ForId(id));
        }

        public AudioPlaybackResult Play(in AudioPlaybackRequest request)
        {
            if (_root == null)
                return AudioPlaybackResult.Fail("AudioService is not initialized (no AudioRoot).");

            AudioDefinition def = request.Definition;
            if (def == null)
            {
                if (!request.Id.IsValid)
                    return AudioPlaybackResult.Fail("AudioPlaybackRequest carries no id and no definition.");

                if (_database == null)
                    return AudioPlaybackResult.Fail("No AudioDatabase registered — cannot resolve AudioId.");

                if (!_database.TryResolve(request.Id, out def) || def == null)
                    return AudioPlaybackResult.Fail($"AudioId {request.Id} not found in database.");
            }

            if (def.Clip == null)
                return AudioPlaybackResult.Fail($"Definition '{def.name}' has no clip.");

            if (def.Bus == null)
                return AudioPlaybackResult.Fail($"Definition '{def.name}' has no bus.");

            try
            {
                return PlayOnBus(def, request);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Audio playback failed for '{def.name}': {ex.Message}");
                return AudioPlaybackResult.Fail($"Audio playback failed: {ex.Message}", ex);
            }
        }

        public AudioPlaybackResult Stop(AudioBusAsset bus)
        {
            if (bus == null) return AudioPlaybackResult.Fail("Stop: bus is null.");
            if (_root == null) return AudioPlaybackResult.Fail("AudioService not initialized.");

            if (_states.TryGetValue(bus, out BusState state) && state.Channel != null)
                state.Channel.Stop();

            LogVerbose(() => $"Bus '{bus.DisplayName}' stopped.");
            return AudioPlaybackResult.Succeed($"Bus '{bus.DisplayName}' stopped.");
        }

        public AudioPlaybackResult PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return AudioPlaybackResult.Fail("PlaySfx: clip is null.");

            // Prefer the full pipeline when a registered definition exists for this clip.
            if (_database != null && _database.TryResolve(clip, out AudioDefinition def) && def != null)
            {
                AudioPlaybackRequest byDef = AudioPlaybackRequest.ForDefinition(def);
                byDef.VolumeScale = volumeScale;
                return Play(in byDef);
            }

            if (_root == null) return AudioPlaybackResult.Fail("AudioService not initialized.");
            if (_rawClipBus == null) return AudioPlaybackResult.Fail("PlaySfx: no raw clip bus configured (AudioServiceSettings.RawClipBus).");

            BusState state = StateFor(_rawClipBus);
            AudioBusChannel channel = ChannelFor(state, out string failure);
            AudioSource source = channel != null ? channel.Acquire(out failure) : null;
            if (source == null) return AudioPlaybackResult.Fail(failure);

            float volume = Mathf.Clamp01(volumeScale) * EffectiveVolume(state);
            source.clip = clip;
            source.loop = false;
            source.volume = volume;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = _rawClipBus.MixerGroup;
            source.Play();

            LogVerbose(() => $"{_rawClipBus.DisplayName} (raw clip): '{clip.name}' (vol={volume:F2}).");
            return AudioPlaybackResult.Succeed($"'{clip.name}' played on bus '{_rawClipBus.DisplayName}' (raw clip).", channel.HandleFor(source));
        }

        private AudioPlaybackResult PlayOnBus(AudioDefinition def, in AudioPlaybackRequest request)
        {
            BusState state = StateFor(def.Bus);
            string busName = def.Bus.DisplayName;

            AudioBusChannel channel = ChannelFor(state, out string failure);
            AudioSource source = channel != null ? channel.Acquire(out failure) : null;
            if (source == null)
            {
                _logger?.Warn($"Dropping playback of '{def.name}': {failure}");
                return AudioPlaybackResult.Fail(failure);
            }

            AudioPlaybackContext ctx = ConfigureSource(source, def, request, state, channel.IsSpatial);

            if (ctx.IsVetoed)
            {
                LogVerbose(() => $"{busName} '{def.name}' vetoed: {ctx.VetoReason}");
                return AudioPlaybackResult.Fail($"{busName} '{def.name}' vetoed: {ctx.VetoReason}");
            }

            if (ctx.ClipOverride != null) source.clip = ctx.ClipOverride;
            if (!ctx.SuppressDefaultPlay) source.Play();

            LogVerbose(() => $"{busName}: '{def.name}' (vol={source.volume:F2}, pitch={source.pitch:F2}, loop={source.loop}).");
            return AudioPlaybackResult.Succeed($"{busName} '{def.name}' played.", channel.HandleFor(source));
        }

        private AudioPlaybackContext ConfigureSource(
            AudioSource source,
            AudioDefinition def,
            in AudioPlaybackRequest request,
            BusState state,
            bool applySpatial)
        {
            float scale = request.VolumeScale ?? 1f;
            float effectiveVolume = Mathf.Clamp01(def.BaseVolume * Mathf.Clamp01(scale)) * EffectiveVolume(state);
            float pitch = request.PitchOverride ?? def.BasePitch;
            bool loop = request.LoopOverride ?? def.BaseLoop;

            source.clip = def.Clip;
            source.loop = loop;
            source.volume = effectiveVolume;
            source.pitch = pitch;
            source.outputAudioMixerGroup = ResolveMixerGroup(def, state.Bus);

            if (applySpatial) ApplySpatial(source, def, request);

            AudioPlaybackContext ctx = new AudioPlaybackContext(def, state.Bus, effectiveVolume, pitch);
            ApplyModulatorsAndOptions(source, def, request, ctx);
            return ctx;
        }

        private static AudioMixerGroup ResolveMixerGroup(AudioDefinition def, AudioBusAsset bus)
        {
            return def.MixerGroupOverride != null ? def.MixerGroupOverride : bus.MixerGroup;
        }

        private static void ApplySpatial(AudioSource source, AudioDefinition def, in AudioPlaybackRequest request)
        {
            source.spatialBlend = def.SpatialBlend;
            source.rolloffMode  = def.Rolloff;
            source.minDistance  = def.MinDistance;
            source.maxDistance  = def.MaxDistance;

            if (request.WorldPosition.HasValue)
                source.transform.position = request.WorldPosition.Value;
        }

        private static void ApplyModulatorsAndOptions(
            AudioSource source,
            AudioDefinition def,
            in AudioPlaybackRequest request,
            AudioPlaybackContext ctx)
        {
            // 1. Modulators on the definition.
            IReadOnlyList<SoundModulatorBase> defMods = def.Modulators;
            if (defMods != null)
            {
                for (int i = 0; i < defMods.Count; i++)
                {
                    if (defMods[i] != null) defMods[i].Modulate(source, ctx);
                }
            }

            // 2. Modulators on the request (caller-side overrides / additions).
            if (request.Modulators != null)
            {
                for (int i = 0; i < request.Modulators.Count; i++)
                {
                    if (request.Modulators[i] != null) request.Modulators[i].Modulate(source, ctx);
                }
            }

            // 3. Options on the definition.
            IReadOnlyList<SoundOptionBase> defOptions = def.Options;
            if (defOptions != null)
            {
                for (int i = 0; i < defOptions.Count; i++)
                {
                    if (defOptions[i] != null) defOptions[i].Apply(source, ctx);
                }
            }

            // 4. Options on the request.
            if (request.Options != null)
            {
                for (int i = 0; i < request.Options.Count; i++)
                {
                    if (request.Options[i] != null) request.Options[i].Apply(source, ctx);
                }
            }
        }

        private BusState StateFor(AudioBusAsset bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            if (_states.TryGetValue(bus, out BusState state)) return state;

            state = new BusState(bus);
            _states.Add(bus, state);
            _buses.Add(bus);
            return state;
        }

        private AudioBusChannel ChannelFor(BusState state, out string failure)
        {
            failure = null;
            if (state.Channel != null) return state.Channel;

            if (state.Bus.Playback == null)
            {
                failure = $"Bus '{state.Bus.DisplayName}' has no playback behaviour.";
                return null;
            }

            state.Channel = state.Bus.Playback.CreateChannel(state.Bus, _root.transform);
            return state.Channel;
        }

        private float EffectiveVolume(BusState state) => _master * state.Volume;

        private void ApplyChannelVolumes()
        {
            foreach (BusState state in _states.Values)
                state.Channel?.OnVolumeChanged(EffectiveVolume(state));
        }

        private string DescribeBuses()
        {
            string[] names = new string[_buses.Count];
            for (int i = 0; i < _buses.Count; i++)
                names[i] = _buses[i].DisplayName;
            return names.Length == 0 ? "none" : string.Join(", ", names);
        }

        private void LogVerbose(Func<string> messageFactory)
        {
            if (!_verbose || _logger == null) return;
            if (!_logger.IsInfoEnabled) return;
            _logger.Info(messageFactory());
        }

        private sealed class BusState
        {
            public readonly AudioBusAsset Bus;
            public float Volume;
            public AudioBusChannel Channel;

            public BusState(AudioBusAsset bus)
            {
                Bus = bus;
                Volume = bus.DefaultVolume;
            }
        }
    }
}
