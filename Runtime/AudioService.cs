using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Astra.Echo
{
    /// <summary>
    /// Main implementation of <see cref="IAudioService"/>.
    /// Resolves <see cref="AudioId"/> through an <see cref="IAudioDatabase"/>,
    /// routes playback to the appropriate channel based on <see cref="AudioType"/>:
    /// <list type="bullet">
    ///   <item><see cref="AudioType.Music"/> → single music channel (replaces current track).</item>
    ///   <item><see cref="AudioType.Ambient"/> → single ambient channel (replaces current track).</item>
    ///   <item><see cref="AudioType.Sfx"/> → pooled one-shot.</item>
    /// </list>
    /// Applies the definition's modulators, then any caller-side modulators / options.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioService")]
    public sealed class AudioService : IAudioService, IRawClipAudioService, IDisposable
    {
        private readonly AudioVolumeSettings _volumes = new AudioVolumeSettings();
        private readonly IEchoLogger _logger;
        private readonly bool _verbose;

        private IAudioDatabase _database;
        private AudioRoot _root;
        private AudioMixerGroup _musicGroup;
        private AudioMixerGroup _sfxGroup;
        private AudioMixerGroup _ambientGroup;
        private bool _ownsRoot;

        public bool IsMusicPlaying => _root != null && _root.MusicPlayer.IsPlaying;
        public bool IsAmbientPlaying => _root != null && _root.AmbientPlayer.IsPlaying;

        public float MasterVolume
        {
            get => _volumes.Master;
            set { _volumes.Master = value; ApplyChannelVolumes(); }
        }

        public float MusicVolume
        {
            get => _volumes.Music;
            set { _volumes.Music = value; ApplyChannelVolumes(); }
        }

        public float SfxVolume
        {
            get => _volumes.Sfx;
            set => _volumes.Sfx = value;
        }

        public float AmbientVolume
        {
            get => _volumes.Ambient;
            set { _volumes.Ambient = value; ApplyChannelVolumes(); }
        }

        public AudioService(IEchoLogger logger = null, bool verbose = false)
        {
            _logger = logger;
            _verbose = verbose;
        }

        internal void SetRoot(AudioRoot root)
        {
            _root = root;
            _ownsRoot = false;
        }

        internal void SetDatabase(IAudioDatabase database)
        {
            _database = database;
        }

        internal void SetMixerRouting(AudioMixerGroup music, AudioMixerGroup sfx, AudioMixerGroup ambient)
        {
            _musicGroup = music;
            _sfxGroup = sfx;
            _ambientGroup = ambient;
        }

        internal void ApplyDefaults(in AudioVolumesDefaults defaults)
        {
            _volumes.Master  = defaults.Master;
            _volumes.Music   = defaults.Music;
            _volumes.Sfx     = defaults.Sfx;
            _volumes.Ambient = defaults.Ambient;
        }

        /// <summary>
        /// Builds the <see cref="AudioRoot"/> (music, ambient, SFX pool), wires the database,
        /// mixer routing and default volumes. Replaces any root previously created by this service.
        /// </summary>
        /// <param name="settings">Initialisation settings; must not be null.</param>
        /// <param name="parent">Optional parent for the root. When null the root persists across scenes.</param>
        public void Initialize(AudioServiceSettings settings, Transform parent = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            DestroyOwnedRoot();

            AudioRoot root = AudioRoot.Create(
                settings.RootName, parent,
                settings.SfxPoolInitialSize, settings.SfxPoolMaxSize,
                settings.MusicMixerGroup, settings.SfxMixerGroup, settings.AmbientMixerGroup);

            _root = root;
            _ownsRoot = true;
            SetMixerRouting(settings.MusicMixerGroup, settings.SfxMixerGroup, settings.AmbientMixerGroup);
            SetDatabase(settings.Database);
            ApplyDefaults(settings.Volumes);

            LogVerbose(() => $"Initialized — root: '{settings.RootName}', SFX pool: {settings.SfxPoolInitialSize} (max {settings.SfxPoolMaxSize}).");
        }

        /// <summary>Destroys the <see cref="AudioRoot"/> created by <see cref="Initialize"/>, if any.</summary>
        public void Dispose()
        {
            DestroyOwnedRoot();
            _root = null;
            _database = null;
        }

        private void DestroyOwnedRoot()
        {
            if (_ownsRoot && _root != null)
                UnityEngine.Object.Destroy(_root.gameObject);

            _ownsRoot = false;
        }

        public void SetVolume(AudioCategory category, float volume)
        {
            switch (category)
            {
                case AudioCategory.Master:  MasterVolume  = volume; break;
                case AudioCategory.Music:   MusicVolume   = volume; break;
                case AudioCategory.Sfx:     SfxVolume     = volume; break;
                case AudioCategory.Ambient: AmbientVolume = volume; break;
            }
        }

        public float GetVolume(AudioCategory category) => _volumes.EffectiveFor(category);

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

            try
            {
                return def.Type switch
                {
                    AudioType.Music   => PlayOnPlayer(_root.MusicPlayer,   def, request, AudioCategory.Music),
                    AudioType.Ambient => PlayOnPlayer(_root.AmbientPlayer, def, request, AudioCategory.Ambient),
                    _                 => PlaySfxInternal(def, request),
                };
            }
            catch (Exception ex)
            {
                _logger?.Error($"Audio playback failed for '{def.name}': {ex.Message}");
                return AudioPlaybackResult.Fail($"Audio playback failed: {ex.Message}", ex);
            }
        }

        public AudioPlaybackResult StopMusic()
        {
            if (_root == null) return AudioPlaybackResult.Fail("AudioService not initialized.");
            _root.MusicPlayer.Stop();
            LogVerbose("Music stopped.");
            return AudioPlaybackResult.Succeed("Music stopped.");
        }

        public AudioPlaybackResult StopAmbient()
        {
            if (_root == null) return AudioPlaybackResult.Fail("AudioService not initialized.");
            _root.AmbientPlayer.Stop();
            LogVerbose("Ambient stopped.");
            return AudioPlaybackResult.Succeed("Ambient stopped.");
        }

        public void StopAllSfx()
        {
            if (_root == null) return;
            _root.SfxPool.StopAll();
            LogVerbose("All SFX stopped.");
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

            AudioSource source = _root.SfxPool.Acquire();
            if (source == null) return AudioPlaybackResult.Fail("SFX pool exhausted.");

            float volume = Mathf.Clamp01(volumeScale) * _volumes.EffectiveSfx;
            source.clip = clip;
            source.loop = false;
            source.volume = volume;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = _sfxGroup;
            source.Play();

            LogVerbose(() => $"SFX (raw clip): '{clip.name}' (vol={volume:F2}).");
            return AudioPlaybackResult.Succeed($"SFX '{clip.name}' played (raw clip).");
        }

        private AudioPlaybackResult PlayOnPlayer(
            AudioPlayer player,
            AudioDefinition def,
            in AudioPlaybackRequest request,
            AudioCategory category)
        {
            AudioSource source = player.GetComponent<AudioSource>();
            AudioPlaybackContext ctx = ConfigureSource(source, def, request, category, applySpatial: false);

            if (ctx.IsVetoed)
            {
                LogVerbose(() => $"{def.Type} '{def.name}' vetoed: {ctx.VetoReason}");
                return AudioPlaybackResult.Fail($"{def.Type} '{def.name}' vetoed: {ctx.VetoReason}");
            }

            if (ctx.ClipOverride != null) source.clip = ctx.ClipOverride;
            if (!ctx.SuppressDefaultPlay) source.Play();

            bool loop = request.LoopOverride ?? def.BaseLoop;
            LogVerbose(() => $"{def.Type}: '{def.name}' (vol={source.volume:F2}, pitch={source.pitch:F2}, loop={loop}).");
            return AudioPlaybackResult.Succeed($"{def.Type} '{def.name}' started.", player);
        }

        private AudioPlaybackResult PlaySfxInternal(AudioDefinition def, in AudioPlaybackRequest request)
        {
            AudioSource source = _root.SfxPool.Acquire();
            if (source == null)
            {
                _logger?.Warn($"SFX pool saturated, dropping playback of '{def.name}'.");
                return AudioPlaybackResult.Fail("SFX pool exhausted.");
            }

            AudioPlaybackContext ctx = ConfigureSource(source, def, request, AudioCategory.Sfx, applySpatial: true);

            if (ctx.IsVetoed)
            {
                LogVerbose(() => $"SFX '{def.name}' vetoed: {ctx.VetoReason}");
                return AudioPlaybackResult.Fail($"SFX '{def.name}' vetoed: {ctx.VetoReason}");
            }

            if (ctx.ClipOverride != null) source.clip = ctx.ClipOverride;
            if (!ctx.SuppressDefaultPlay) source.Play();

            LogVerbose(() => $"SFX: '{def.name}' (vol={source.volume:F2}, pitch={source.pitch:F2}).");
            return AudioPlaybackResult.Succeed($"SFX '{def.name}' played.");
        }

        private AudioPlaybackContext ConfigureSource(
            AudioSource source,
            AudioDefinition def,
            in AudioPlaybackRequest request,
            AudioCategory category,
            bool applySpatial)
        {
            float effectiveVolume = ResolveVolume(def, request, category);
            float pitch = ResolvePitch(def, request);
            bool loop = request.LoopOverride ?? def.BaseLoop;

            source.clip = def.Clip;
            source.loop = loop;
            source.volume = effectiveVolume;
            source.pitch = pitch;
            source.outputAudioMixerGroup = ResolveMixerGroup(def, category);

            if (applySpatial) ApplySpatial(source, def, request);

            AudioPlaybackContext ctx = new AudioPlaybackContext(def, def.Type, effectiveVolume, pitch);
            ApplyModulatorsAndOptions(source, def, request, ctx);
            return ctx;
        }

        private float ResolveVolume(AudioDefinition def, in AudioPlaybackRequest request, AudioCategory category)
        {
            float scale = request.VolumeScale ?? 1f;
            return Mathf.Clamp01(def.BaseVolume * Mathf.Clamp01(scale)) * _volumes.EffectiveFor(category);
        }

        private static float ResolvePitch(AudioDefinition def, in AudioPlaybackRequest request)
        {
            return request.PitchOverride ?? def.BasePitch;
        }

        private AudioMixerGroup ResolveMixerGroup(AudioDefinition def, AudioCategory category)
        {
            if (def.MixerGroupOverride != null) return def.MixerGroupOverride;
            return category switch
            {
                AudioCategory.Music   => _musicGroup,
                AudioCategory.Ambient => _ambientGroup,
                _                     => _sfxGroup,
            };
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

        private void ApplyChannelVolumes()
        {
            if (_root == null) return;
            if (_root.MusicPlayer.IsPlaying)   _root.MusicPlayer.SetVolume(_volumes.EffectiveMusic);
            if (_root.AmbientPlayer.IsPlaying) _root.AmbientPlayer.SetVolume(_volumes.EffectiveAmbient);
        }

        private void LogVerbose(string message)
        {
            if (_verbose) _logger?.Info(message);
        }

        private void LogVerbose(Func<string> messageFactory)
        {
            if (!_verbose || _logger == null) return;
            if (!_logger.IsInfoEnabled) return;
            _logger.Info(messageFactory());
        }
    }
}
