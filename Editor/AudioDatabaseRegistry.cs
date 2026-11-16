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
    /// <see cref="AudioServiceSettings.Databases"/>).</para>
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

        public static List<AudioDatabase> FindAllDatabases()
        {
            List<AudioDatabase> result = new List<AudioDatabase>();
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(AudioDatabase));
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioDatabase database = AssetDatabase.LoadAssetAtPath<AudioDatabase>(path);
                if (database != null)
                    result.Add(database);
            }

            result.Sort((AudioDatabase a, AudioDatabase b) =>
                string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));
            return result;
        }

        public static void SyncAllDatabases(AudioImportSettings settings)
        {
            List<AudioDefinition> definitions = FindAllDefinitions();
            List<AudioDatabase> databases = FindAllDatabases();
            List<AudioDatabaseAssignment.Scope> scopes = BuildScopes(databases);

            List<string> definitionPaths = new List<string>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
                definitionPaths.Add(AssetDatabase.GetAssetPath(definitions[i]));

            string defaultPath = ResolveDefaultDatabaseAssetPath(settings, scopes, databases.Count > 1);
            Dictionary<string, List<string>> membership = AudioDatabaseAssignment.BuildMembership(
                definitionPaths, scopes, defaultPath);

            for (int i = 0; i < databases.Count; i++)
            {
                AudioDatabase database = databases[i];
                string path = AssetDatabase.GetAssetPath(database);
                List<AudioDefinition> owned = ResolveDefinitions(membership, path, definitions);
                database.EditorSetDefinitions(owned);
                EditorUtility.SetDirty(database);
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                AudioDefinition def = definitions[i];
                string assetPath = AssetDatabase.GetAssetPath(def);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrEmpty(guid) && def.Id.Guid != guid)
                {
                    def.EditorSetId(new AudioId(guid));
                    EditorUtility.SetDirty(def);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static List<AudioDatabaseAssignment.Scope> BuildScopes(List<AudioDatabase> databases)
        {
            List<AudioDatabaseAssignment.Scope> scopes = new List<AudioDatabaseAssignment.Scope>(databases.Count);
            for (int i = 0; i < databases.Count; i++)
            {
                string databasePath = AssetDatabase.GetAssetPath(databases[i]);
                string folder = AudioDatabaseAssignment.GetOwnedFolder(databasePath);
                scopes.Add(new AudioDatabaseAssignment.Scope(databasePath, folder));
            }

            return scopes;
        }

        private static string ResolveDefaultDatabaseAssetPath(
            AudioImportSettings settings,
            List<AudioDatabaseAssignment.Scope> scopes,
            bool warnWhenAmbiguous)
        {
            string configured = settings != null && settings.DefaultDatabase != null
                ? AssetDatabase.GetAssetPath(settings.DefaultDatabase)
                : null;

            string resolved = AudioDatabaseAssignment.ResolveDefaultDatabaseAssetPath(scopes, configured);
            if (warnWhenAmbiguous && settings != null && settings.DefaultDatabase == null && scopes.Count > 1 && resolved != null)
            {
                Debug.LogWarning("[Echo] Multiple AudioDatabase assets found — using the shortest path as the default orphan bucket. " +
                                 "Assign AudioImportSettings.DefaultDatabase to silence this.");
            }

            return resolved;
        }

        private static List<AudioDefinition> ResolveDefinitions(
            Dictionary<string, List<string>> membership,
            string databasePath,
            List<AudioDefinition> definitions)
        {
            if (!membership.TryGetValue(databasePath, out List<string> paths) || paths.Count == 0)
                return new List<AudioDefinition>();

            Dictionary<string, AudioDefinition> byPath = new Dictionary<string, AudioDefinition>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
            {
                string path = AssetDatabase.GetAssetPath(definitions[i]);
                byPath[path] = definitions[i];
            }

            List<AudioDefinition> owned = new List<AudioDefinition>(paths.Count);
            for (int i = 0; i < paths.Count; i++)
            {
                if (byPath.TryGetValue(paths[i], out AudioDefinition definition))
                    owned.Add(definition);
            }

            return owned;
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
