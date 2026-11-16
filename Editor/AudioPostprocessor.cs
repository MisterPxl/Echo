using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Astra.Echo.Editor
{
    /// <summary>
    /// AssetPostprocessor that automates the audio pipeline:
    /// <list type="number">
    ///   <item>On <see cref="OnPreprocessAudio"/>: applies the SFX/Music import profile
    ///         declared in <see cref="AudioImportSettings"/>.</item>
    ///   <item>On <see cref="OnPostprocessAllAssets"/>: creates missing AudioDefinitions
    ///         for newly-imported clips that fall in an auto-create scope, resyncs every
    ///         <see cref="AudioDatabase"/> with definitions in its folder tree, and triggers
    ///         <see cref="AudioIdCodegen"/>.</item>
    /// </list>
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio.Editor", "Core.Audio.Editor", "AudioPostprocessor")]
    public sealed class AudioPostprocessor : AssetPostprocessor
    {
        private const string AudioClipExtensionsPattern = ".wav|.mp3|.ogg|.aif|.aiff|.flac";

        private void OnPreprocessAudio()
        {
            AudioImportSettings settings = AudioDatabaseRegistry.FindImportSettings();
            if (settings == null || !settings.ApplyImportProfiles) return;

            AudioImporter importer = (AudioImporter)assetImporter;
            ApplyProfile(importer, LooksLikeMusic(assetPath) ? settings.MusicProfile : settings.SfxProfile);
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            AudioImportSettings settings = AudioDatabaseRegistry.FindImportSettings();

            List<string> eligibleClipPaths = CollectEligibleClipPaths(importedAssets, settings);
            bool definitionsTouched = ContainsAssetOfType<AudioDefinition>(importedAssets)
                                    || ContainsAssetOfType<AudioDefinition>(movedAssets)
                                    || ContainsAssetOfType<AudioDefinition>(deletedAssets);

            if (eligibleClipPaths.Count == 0 && !definitionsTouched) return;

            bool autoCreateRequested = eligibleClipPaths.Count > 0
                                    && settings != null
                                    && settings.AutoCreateDefinitions;

            if (autoCreateRequested && AudioDatabaseRegistry.FindAllDatabases().Count == 0)
                AudioDatabaseRegistry.CreatePrimaryDatabase();

            if (autoCreateRequested)
            {
                for (int i = 0; i < eligibleClipPaths.Count; i++)
                {
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(eligibleClipPaths[i]);
                    if (clip == null) continue;

                    AudioDefinitionFactory.GetOrCreateForClip(clip, settings);
                }
            }

            if (definitionsTouched || autoCreateRequested)
                AudioDatabaseRegistry.SyncAllDatabases(settings);

            AudioIdCodegen.Generate(AudioDatabaseRegistry.FindAllDefinitions());
        }

        private static List<string> CollectEligibleClipPaths(string[] importedAssets, AudioImportSettings settings)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < importedAssets.Length; i++)
            {
                string path = importedAssets[i];
                if (!IsAudioClipPath(path)) continue;

                if (settings != null && !settings.IsPathInAutoCreateScope(path)) continue;

                result.Add(path);
            }
            return result;
        }

        private static bool IsAudioClipPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            int dot = path.LastIndexOf('.');
            if (dot < 0) return false;
            string ext = path.Substring(dot).ToLowerInvariant();
            return AudioClipExtensionsPattern.Contains(ext);
        }

        private static bool ContainsAssetOfType<T>(string[] paths) where T : Object
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<T>(paths[i]) != null) return true;
            }
            return false;
        }

        private static bool LooksLikeMusic(string assetPath)
        {
            string lower = assetPath.ToLowerInvariant();
            return lower.Contains("/music/") || lower.Contains("_music");
        }

        private static void ApplyProfile(AudioImporter importer, AudioImportProfile profile)
        {
            importer.forceToMono = profile.ForceMono;
            importer.loadInBackground = profile.LoadInBackground;
            importer.ambisonic = profile.Ambisonic;

            AudioImporterSampleSettings sample = importer.defaultSampleSettings;
            sample.loadType = profile.LoadType;
            sample.compressionFormat = MapCompression(profile.CompressionFormat);
            sample.quality = Mathf.Clamp01(profile.Quality);
            sample.preloadAudioData = profile.PreloadAudioData;
            importer.defaultSampleSettings = sample;
        }

        private static UnityEngine.AudioCompressionFormat MapCompression(AudioCompressionFormat format)
        {
            return format switch
            {
                AudioCompressionFormat.PCM    => UnityEngine.AudioCompressionFormat.PCM,
                AudioCompressionFormat.Vorbis => UnityEngine.AudioCompressionFormat.Vorbis,
                AudioCompressionFormat.ADPCM  => UnityEngine.AudioCompressionFormat.ADPCM,
                AudioCompressionFormat.MP3    => UnityEngine.AudioCompressionFormat.MP3,
                AudioCompressionFormat.AAC    => UnityEngine.AudioCompressionFormat.AAC,
                _                             => UnityEngine.AudioCompressionFormat.Vorbis,
            };
        }
    }
}