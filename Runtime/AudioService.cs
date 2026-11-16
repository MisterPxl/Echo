using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Astra.Echo
{
    /// <summary>
    /// Main implementation of <see cref="IAudioService"/>.
    /// Resolves <see cref="AudioId"/> through registered <see cref="IAudioDatabase"/> instances and plays
    /// the definition on the channel of its <see cref="AudioBusAsset"/>, keyed by <see cref="AudioBusAsset.VolumeKey"/>.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioService")]
    public sealed class AudioService : IAudioService, IAudioDatabaseRegistry, IRawClipAudioService, IDisposable
    {
        private readonly Dictionary<string, BusState> _states = new Dictionary<string, BusState>(StringComparer.Ordinal);
        private readonly List<AudioBusAsset> _buses = new List<AudioBusAsset>();
        private readonly List<IAudioDatabase> _databases = new List<IAudioDatabase>();
        private readonly Dictionary<AudioSource, AudioDefinition> _sourceDefinitions = new Dictionary<AudioSource, AudioDefinition>();
        private readonly IEchoLogger _logger;
        private readonly bool _verbose;

        private AudioRoot _root;
        private AudioBusAsset _rawClipBus;
        private float _master = 1f;

        public float MasterVolume
        {
            get => _master;
            set { _master = Mathf.Clamp01(value); ApplyChannelVolumes(); }
        }

        public IReadOnlyList<AudioBusAsset> Buses => _buses;

        public IReadOnlyList<IAudioDatabase> Databases => _databases;

        public AudioService(IEchoLogger logger = null, bool verbose = false)
        {
            _logger = logger;
            _verbose = verbose;
        }

        public void Initialize(AudioServiceSettings settings, Transform parent = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            DestroyRoot();
            _states.Clear();
            _buses.Clear();
            _databases.Clear();
            _sourceDefinitions.Clear();

            _root = AudioRoot.Create(settings.RootName, parent);
            _rawClipBus = settings.RawClipBus;
            _master = Mathf.Clamp01(settings.MasterVolume);

            if (settings.Databases != null)
            {
                for (int i = 0; i < settings.Databases.Count; i++)
                {
                    IAudioDatabase database = settings.Databases[i];
                    if (database != null && !_databases.Contains(database))
                        _databases.Add(database);
                }
            }

            if (settings.Buses != null)
            {
                for (int i = 0; i < settings.Buses.Count; i++)
                {
                    if (settings.Buses[i] != null) PrepareChannel(settings.Buses[i]);
                }
            }

            if (_rawClipBus != null) PrepareChannel(_rawClipBus);

            LogVerbose(() => $"Initialized — root: '{settings.RootName}', buses: {DescribeBuses()}, databases: {_databases.Count}.");
        }

        public bool AddDatabase(IAudioDatabase database)
        {
            if (database == null || _databases.Contains(database))
                return false;

            _databases.Add(database);
            return true;
        }

        public bool RemoveDatabase(IAudioDatabase database)
        {
            if (database == null || !_databases.Remove(database))
                return false;

            StopPlaybacksForDatabase(database);
            return true;
        }

        public void Dispose()
        {
            DestroyRoot();
            _databases.Clear();
        }

        public float GetVolume(AudioBusAsset bus)
        {
            BusState state = ResolveBusState(bus, createIfMissing: false);
            if (state != null)
                return state.Volume;

            return bus != null ? bus.DefaultVolume : 0f;
        }

        public void SetVolume(AudioBusAsset bus, float volume)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));

            BusState state = StateFor(bus);
            state.Volume = Mathf.Clamp01(volume);
            state.Channel?.OnVolumeChanged(EffectiveVolume(state));
        }

        public bool IsPlaying(AudioBusAsset bus)
        {
            BusState state = ResolveBusState(bus, createIfMissing: false);
            return state != null && state.Channel != null && state.Channel.IsPlaying;
        }

        public AudioRequest CreateRequest(AudioId id) => new AudioRequest(this, id);

        public AudioPlaybackResult Play(AudioId id) => Play(AudioPlaybackRequest.ForId(id));

        public AudioPlaybackResult Play(in AudioPlaybackRequest request)
        {
            if (_root == null)
                return AudioPlaybackResult.Fail("AudioService is not initialized (no AudioRoot).");

            AudioDefinition def = request.Definition;
            if (def == null)
            {
                if (!request.Id.IsValid)
                    return AudioPlaybackResult.Fail("AudioPlaybackRequest carries no id and no definition.");

                if (_databases.Count == 0)
                    return AudioPlaybackResult.Fail("No AudioDatabase registered — cannot resolve AudioId.");

                if (!TryResolve(request.Id, out def) || def == null)
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

            BusState state = ResolveBusState(bus, createIfMissing: false);
            if (state != null && state.Channel != null)
                state.Channel.Stop();

            string displayName = state != null ? state.Bus.DisplayName : bus.DisplayName;
            LogVerbose(() => $"Bus '{displayName}' stopped.");
            return AudioPlaybackResult.Succeed($"Bus '{displayName}' stopped.");
        }

        public AudioPlaybackResult PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return AudioPlaybackResult.Fail("PlaySfx: clip is null.");

            if (TryResolve(clip, out AudioDefinition def) && def != null)
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
            source.outputAudioMixerGroup = state.Bus.MixerGroup;
            source.Play();

            LogVerbose(() => $"{state.Bus.DisplayName} (raw clip): '{clip.name}' (vol={volume:F2}).");
            return AudioPlaybackResult.Succeed($"'{clip.name}' played on bus '{state.Bus.DisplayName}' (raw clip).", channel.HandleFor(source));
        }

        private bool TryResolve(AudioId id, out AudioDefinition definition)
        {
            for (int i = 0; i < _databases.Count; i++)
            {
                if (_databases[i].TryResolve(id, out definition) && definition != null)
                    return true;
            }

            definition = null;
            return false;
        }

        private bool TryResolve(AudioClip clip, out AudioDefinition definition)
        {
            for (int i = 0; i < _databases.Count; i++)
            {
                if (_databases[i].TryResolve(clip, out definition) && definition != null)
                    return true;
            }

            definition = null;
            return false;
        }

        private AudioPlaybackResult PlayOnBus(AudioDefinition def, in AudioPlaybackRequest request)
        {
            BusState state = StateFor(def.Bus);
            string busName = state.Bus.DisplayName;

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
                channel.DiscardAcquire(source);
                LogVerbose(() => $"{busName} '{def.name}' vetoed: {ctx.VetoReason}");
                return AudioPlaybackResult.Fail($"{busName} '{def.name}' vetoed: {ctx.VetoReason}");
            }

            if (ctx.ClipOverride != null) source.clip = ctx.ClipOverride;
            if (!ctx.SuppressDefaultPlay) source.Play();

            _sourceDefinitions[source] = def;

            LogVerbose(() => $"{busName}: '{def.name}' (vol={source.volume:F2}, pitch={source.pitch:F2}, loop={source.loop}).");
            return AudioPlaybackResult.Succeed($"{busName} '{def.name}' played.", channel.HandleFor(source));
        }

        private void StopPlaybacksForDatabase(IAudioDatabase database)
        {
            List<AudioSource> toStop = new List<AudioSource>();
            foreach (KeyValuePair<AudioSource, AudioDefinition> pair in _sourceDefinitions)
            {
                if (database.Contains(pair.Value.Id))
                    toStop.Add(pair.Key);
            }

            for (int i = 0; i < toStop.Count; i++)
            {
                AudioSource source = toStop[i];
                if (source != null && source.isPlaying)
                    source.Stop();
                _sourceDefinitions.Remove(source);
            }
        }

        private void PrepareChannel(AudioBusAsset bus)
        {
            if (ChannelFor(StateFor(bus), out string failure) == null)
                _logger?.Warn(failure);
        }

        private void DestroyRoot()
        {
            if (_root != null)
                UnityEngine.Object.Destroy(_root.gameObject);

            _root = null;
            _sourceDefinitions.Clear();
            foreach (BusState state in _states.Values)
                state.Channel = null;
        }

        private static string BusKey(AudioBusAsset bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));

            string key = bus.VolumeKey;
            return string.IsNullOrEmpty(key) ? bus.name : key;
        }

        private BusState ResolveBusState(AudioBusAsset bus, bool createIfMissing)
        {
            if (bus == null)
                return null;

            string key = BusKey(bus);
            if (_states.TryGetValue(key, out BusState state))
                return state;

            return createIfMissing ? StateFor(bus) : null;
        }

        private BusState StateFor(AudioBusAsset bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));

            string key = BusKey(bus);
            if (_states.TryGetValue(key, out BusState state))
                return state;

            state = new BusState(bus);
            _states.Add(key, state);
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
            source.rolloffMode = def.Rolloff;
            source.minDistance = def.MinDistance;
            source.maxDistance = def.MaxDistance;

            if (request.WorldPosition.HasValue)
                source.transform.position = request.WorldPosition.Value;
        }

        private static void ApplyModulatorsAndOptions(
            AudioSource source,
            AudioDefinition def,
            in AudioPlaybackRequest request,
            AudioPlaybackContext ctx)
        {
            IReadOnlyList<SoundModulatorBase> defMods = def.Modulators;
            if (defMods != null)
            {
                for (int i = 0; i < defMods.Count; i++)
                {
                    if (defMods[i] != null) defMods[i].Modulate(source, ctx);
                }
            }

            if (request.Modulators != null)
            {
                for (int i = 0; i < request.Modulators.Count; i++)
                {
                    if (request.Modulators[i] != null) request.Modulators[i].Modulate(source, ctx);
                }
            }

            IReadOnlyList<SoundOptionBase> defOptions = def.Options;
            if (defOptions != null)
            {
                for (int i = 0; i < defOptions.Count; i++)
                {
                    if (defOptions[i] != null) defOptions[i].Apply(source, ctx);
                }
            }

            if (request.Options != null)
            {
                for (int i = 0; i < request.Options.Count; i++)
                {
                    if (request.Options[i] != null) request.Options[i].Apply(source, ctx);
                }
            }
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