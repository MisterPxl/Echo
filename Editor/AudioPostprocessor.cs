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
    ///         for newly-imported clips that fall in an auto-create scope, then refreshes
    ///         the database and triggers <see cref="AudioIdCodegen"/>.</item>
    /// </list>
    ///
    /// <para>The database itself is only created lazily, the first time at least one
    /// eligible clip is imported. Editing or moving definitions never causes a database
    /// to spring into existence.</para>
    ///
    /// <para>All steps are batched into a single end-of-import pass. The codegen and
    /// AssetDatabase write only happen once even if many clips were imported together.</para>
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
            // The clip duration is unknown before import: the profile follows file name hints,
            // the bus is chosen by duration once the definition is created.
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

            // Nothing audio-related happened in this batch → bail out.
            // No DB lookup, no DB creation, no codegen.
            if (eligibleClipPaths.Count == 0 && !definitionsTouched) return;

            // The database is created on demand only when we actually have an eligible
            // clip to register. Editing or moving an existing definition never triggers
            // a database creation — it only refreshes the existing one if any.
            AudioDatabase database = AudioDatabaseRegistry.FindPrimaryDatabase();
            bool autoCreateRequested = eligibleClipPaths.Count > 0
                                    && settings != null
                                    && settings.AutoCreateDefinitions;

            if (database == null)
            {
                if (autoCreateRequested)
                {
                    database = AudioDatabaseRegistry.CreatePrimaryDatabase();
                }
                else
                {
                    // No database, no clips to register: nothing to do beyond definition diffs
                    // (which would have nowhere to land). Skip silently.
                    if (definitionsTouched)
                        AudioIdCodegen.Generate(AudioDatabaseRegistry.FindAllDefinitions());
                    return;
                }
            }

            bool changed = false;

            if (autoCreateRequested)
            {
                for (int i = 0; i < eligibleClipPaths.Count; i++)
                {
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(eligibleClipPaths[i]);
                    if (clip == null) continue;

                    AudioDefinition def = AudioDefinitionFactory.GetOrCreateForClip(clip, settings, database);
                    if (def != null) changed = true;
                }
            }

            // Re-sync the database with all definitions in the project so manual
            // creates / moves / deletes are reflected without a separate command.
            List<AudioDefinition> allDefs = AudioDatabaseRegistry.FindAllDefinitions();
            database.EditorSetDefinitions(allDefs);

            // Make sure each definition carries its current asset GUID as its AudioId.
            for (int i = 0; i < allDefs.Count; i++)
            {
                AudioDefinition def = allDefs[i];
                string assetPath = AssetDatabase.GetAssetPath(def);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrEmpty(guid) && def.Id.Guid != guid)
                {
                    def.EditorSetId(new AudioId(guid));
                    EditorUtility.SetDirty(def);
                    changed = true;
                }
            }

            EditorUtility.SetDirty(database);
            if (changed) AssetDatabase.SaveAssets();

            AudioIdCodegen.Generate(allDefs);
        }

        private static List<string> CollectEligibleClipPaths(string[] importedAssets, AudioImportSettings settings)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < importedAssets.Length; i++)
            {
                string path = importedAssets[i];
                if (!IsAudioClipPath(path)) continue;

                // Honor the auto-create scope whitelist. Out-of-scope clips are ignored
                // silently — by design, see AudioImportSettings.AutoCreateScopes.
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

            // PreloadAudioData moved from AudioImporter to AudioImporterSampleSettings
            // in Unity 2022 — it is now a per-platform sample setting.
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
