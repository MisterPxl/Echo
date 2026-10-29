using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Astra.Echo.Editor
{
    /// <summary>
    /// Editor-side helper that locates the project's primary <see cref="AudioDatabase"/>
    /// and the active <see cref="AudioImportSettings"/> via <c>AssetDatabase</c>.
    ///
    /// <para>Strategy: pick the first asset of the requested type discovered through
    /// <c>FindAssets</c>. Projects with multiple databases must designate one as the
    /// primary by leaving only one in scope (or by referencing it explicitly via
    /// <see cref="AudioServiceSettings.Database"/>).</para>
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio.Editor", "Core.Audio.Editor", "AudioDatabaseRegistry")]
    public static class AudioDatabaseRegistry
    {
        private const string DefaultAudioFolder = "Assets/Echo";
        private const string DefaultDatabaseFileName = "AudioDatabase.asset";

        /// <summary>
        /// Returns the project's primary <see cref="AudioDatabase"/> if one exists,
        /// or null. Does not create anything — use <see cref="CreatePrimaryDatabase"/>
        /// when an explicit creation is desired.
        /// </summary>
        public static AudioDatabase FindPrimaryDatabase()
        {
            return FindFirstAsset<AudioDatabase>();
        }

        /// <summary>
        /// Creates a default <see cref="AudioDatabase"/> at the canonical path.
        /// Caller is responsible for first checking that none exists.
        /// </summary>
        public static AudioDatabase CreatePrimaryDatabase()
        {
            string folder = ResolveAudioFolder();
            EnsureFolder(folder);

            AudioDatabase asset = ScriptableObject.CreateInstance<AudioDatabase>();
            string path = $"{folder}/{DefaultDatabaseFileName}";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log($"[Echo] Created default AudioDatabase at {path}.");
            return asset;
        }

        /// <summary>
        /// Convenience: returns the existing primary database, creating one only if absent.
        /// Use sparingly — prefer the find/create split for predictable side effects.
        /// </summary>
        public static AudioDatabase FindOrCreatePrimaryDatabase()
        {
            return FindPrimaryDatabase() ?? CreatePrimaryDatabase();
        }

        /// <summary>
        /// Folder where the default database is created: the folder of the active
        /// <see cref="AudioImportSettings"/> asset when one exists, otherwise
        /// <c>Assets/Echo</c>. Never inside the (read-only) package.
        /// </summary>
        internal static string ResolveAudioFolder()
        {
            AudioImportSettings settings = FindImportSettings();
            if (settings != null)
            {
                string settingsPath = AssetDatabase.GetAssetPath(settings);
                string folder = Path.GetDirectoryName(settingsPath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(folder) && folder.StartsWith("Assets")) return folder;
            }

            return DefaultAudioFolder;
        }

        public static AudioImportSettings FindImportSettings()
        {
            return FindFirstAsset<AudioImportSettings>();
        }

        public static List<AudioDefinition> FindAllDefinitions()
        {
            List<AudioDefinition> result = new List<AudioDefinition>();
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(AudioDefinition));
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioDefinition def = AssetDatabase.LoadAssetAtPath<AudioDefinition>(path);
                if (def != null) result.Add(def);
            }
            return result;
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length == 0) return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
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
