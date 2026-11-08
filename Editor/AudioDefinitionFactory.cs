using System.IO;
using UnityEditor;
using UnityEngine;

namespace Astra.Echo.Editor
{
    /// <summary>
    /// Editor utility that creates and persists <see cref="AudioDefinition"/> assets
    /// from imported <see cref="AudioClip"/> files. Handles bus classification (the music or SFX
    /// bus of <see cref="AudioImportSettings"/>, by duration), id assignment from the asset GUID, and addition to the
    /// project's primary <see cref="AudioDatabase"/>.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio.Editor", "Core.Audio.Editor", "AudioDefinitionFactory")]
    public static class AudioDefinitionFactory
    {
        /// <summary>
        /// Returns an existing definition for <paramref name="clip"/> if any, or creates one
        /// and registers it in the active database. Idempotent — safe to call multiple times
        /// on the same clip.
        /// </summary>
        public static AudioDefinition GetOrCreateForClip(AudioClip clip, AudioImportSettings settings, AudioDatabase database)
        {
            if (clip == null) return null;

            // Skip if a definition already references this clip.
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(AudioDefinition));
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioDefinition existing = AssetDatabase.LoadAssetAtPath<AudioDefinition>(path);
                if (existing != null && existing.Clip == clip)
                    return existing;
            }

            string clipPath = AssetDatabase.GetAssetPath(clip);
            string outputDir = ResolveOutputDirectory(settings, clipPath);
            string fileName = $"AD_{Path.GetFileNameWithoutExtension(clipPath)}.asset";
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(outputDir, fileName).Replace("\\", "/"));

            EnsureFolder(outputDir);

            AudioDefinition def = ScriptableObject.CreateInstance<AudioDefinition>();
            def.EditorSetClip(clip);
            AudioBusAsset bus = ClassifyByDuration(clip, settings);
            def.EditorSetBus(bus);
            if (bus == null)
                Debug.LogWarning($"[Echo] No bus for clip '{clip.name}': assign the music and SFX buses of AudioImportSettings, then set the definition bus.");

            AssetDatabase.CreateAsset(def, assetPath);

            // Capture the asset's GUID — this becomes the stable AudioId.
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            def.EditorSetId(new AudioId(guid));
            EditorUtility.SetDirty(def);

            if (database != null)
            {
                database.EditorAddDefinition(def);
                EditorUtility.SetDirty(database);
            }

            Debug.Log($"[Echo] Auto-created AudioDefinition for clip '{clip.name}' → {assetPath}");
            return def;
        }

        /// <summary>The music bus of <paramref name="settings"/> for clips reaching its threshold, its SFX bus otherwise; null without settings.</summary>
        public static AudioBusAsset ClassifyByDuration(AudioClip clip, AudioImportSettings settings)
        {
            if (settings == null) return null;
            if (clip == null) return settings.SfxBus;
            return clip.length >= settings.MusicDurationThresholdSeconds ? settings.MusicBus : settings.SfxBus;
        }

        private static string ResolveOutputDirectory(AudioImportSettings settings, string clipPath)
        {
            if (settings != null && !string.IsNullOrEmpty(settings.DefinitionOutputFolder))
                return "Assets/" + settings.DefinitionOutputFolder.TrimStart('/');

            return Path.GetDirectoryName(clipPath)?.Replace("\\", "/") ?? "Assets";
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
