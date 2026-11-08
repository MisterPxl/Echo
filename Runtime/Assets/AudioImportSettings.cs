using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Asset describing how the editor pipeline should configure newly-imported audio clips
    /// and which bus new definitions get. The asset itself stays runtime-safe
    /// (no UnityEditor references) so it can live next to the consumer's other audio assets.
    ///
    /// Used by the Editor-side AudioPostprocessor.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioImportSettings", menuName = "Astra/Echo/Audio Import Settings")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioImportSettings")]
    public sealed class AudioImportSettings : ScriptableObject
    {
        public const string DefaultGeneratedOutputFolder = "Assets/Echo/Generated";

        [Header("Classification")]
        [Tooltip("Clips longer than this duration (seconds) get the music bus. " +
                 "Anything shorter gets the SFX bus. Any other bus is assigned by hand.")]
        [SerializeField, Min(0.1f)] private float _musicDurationThresholdSeconds = 8f;

        [Tooltip("Bus of auto-created definitions whose clip reaches the threshold (Echo ships MusicBus).")]
        [SerializeField] private AudioBusAsset _musicBus;

        [Tooltip("Bus of auto-created definitions whose clip is shorter than the threshold (Echo ships SfxBus).")]
        [SerializeField] private AudioBusAsset _sfxBus;

        [Header("Default profiles")]
        [SerializeField] private AudioImportProfile _sfx = AudioImportProfile.SfxDefault();
        [SerializeField] private AudioImportProfile _music = AudioImportProfile.MusicDefault();

        [Header("Pipeline")]
        [Tooltip("Auto-create an AudioDefinition next to each imported clip if missing.")]
        [SerializeField] private bool _autoCreateDefinitions = true;

        [Tooltip("Folder (relative to Assets/) where new AudioDefinition assets are created. " +
                 "When empty, definitions are created next to the imported clip.")]
        [SerializeField] private string _definitionOutputFolder = "";

        [Tooltip("Whitelist of folders (relative to Assets/) inside which audio clips trigger " +
                 "auto-creation. When the list is empty, the whole project is in scope. " +
                 "Example entries: '_Project/Audio', '_Project/Game/Sfx'.")]
        [SerializeField] private string[] _autoCreateScopes = System.Array.Empty<string>();

        [Tooltip("Folder (project-relative, e.g. 'Assets/Echo/Generated') where the AudioIds registry " +
                 "(public static partial class AudioIds) is generated. Must belong to the consumer project, " +
                 "never to a read-only package.")]
        [SerializeField] private string _generatedOutputFolder = DefaultGeneratedOutputFolder;

        [Tooltip("If true, applies the import profile (compression, load type, mono, …) on import.")]
        [SerializeField] private bool _applyImportProfiles = true;

        public float MusicDurationThresholdSeconds => _musicDurationThresholdSeconds;
        public AudioBusAsset MusicBus => _musicBus;
        public AudioBusAsset SfxBus => _sfxBus;
        public AudioImportProfile SfxProfile => _sfx;
        public AudioImportProfile MusicProfile => _music;
        public bool AutoCreateDefinitions => _autoCreateDefinitions;
        public string DefinitionOutputFolder => _definitionOutputFolder;
        public string GeneratedOutputFolder => _generatedOutputFolder;
        public bool ApplyImportProfiles => _applyImportProfiles;

        /// <summary>
        /// Returns true if the given asset path falls inside an auto-create scope.
        /// When no scope is configured, the whole project is considered in scope.
        /// </summary>
        public bool IsPathInAutoCreateScope(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (_autoCreateScopes == null || _autoCreateScopes.Length == 0) return true;

            string normalized = assetPath.Replace('\\', '/');
            const string assetsPrefix = "Assets/";
            string relative = normalized.StartsWith(assetsPrefix)
                ? normalized.Substring(assetsPrefix.Length)
                : normalized;

            for (int i = 0; i < _autoCreateScopes.Length; i++)
            {
                string scope = _autoCreateScopes[i];
                if (string.IsNullOrEmpty(scope)) continue;

                string trimmed = scope.Replace('\\', '/').Trim('/');
                if (trimmed.Length == 0) continue;

                if (relative.Equals(trimmed, System.StringComparison.OrdinalIgnoreCase)) return true;
                if (relative.StartsWith(trimmed + "/", System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Serializable bundle of Unity audio import parameters.
    /// Mirrored manually so the runtime assembly stays free of <c>UnityEditor</c>.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioImportProfile")]
    public struct AudioImportProfile
    {
        public AudioClipLoadType LoadType;
        public AudioCompressionFormat CompressionFormat;
        [Range(0f, 1f)] public float Quality;
        public bool ForceMono;
        public bool PreloadAudioData;
        public bool LoadInBackground;
        public bool Ambisonic;

        public static AudioImportProfile SfxDefault()
        {
            return new AudioImportProfile
            {
                LoadType = AudioClipLoadType.DecompressOnLoad,
                CompressionFormat = AudioCompressionFormat.ADPCM,
                Quality = 1f,
                ForceMono = true,
                PreloadAudioData = true,
                LoadInBackground = false,
                Ambisonic = false,
            };
        }

        public static AudioImportProfile MusicDefault()
        {
            return new AudioImportProfile
            {
                LoadType = AudioClipLoadType.Streaming,
                CompressionFormat = AudioCompressionFormat.Vorbis,
                Quality = 0.7f,
                ForceMono = false,
                PreloadAudioData = false,
                LoadInBackground = true,
                Ambisonic = false,
            };
        }
    }

    /// <summary>
    /// Mirrors UnityEditor's AudioCompressionFormat without depending on UnityEditor
    /// (so the runtime assembly stays clean). Editor code maps to the real enum.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioCompressionFormat")]
    public enum AudioCompressionFormat
    {
        PCM = 0,
        Vorbis = 1,
        ADPCM = 2,
        MP3 = 3,
        AAC = 4
    }
}
