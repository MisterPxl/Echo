using System;
using System.Collections.Generic;

namespace Astra.Echo.Editor
{
    /// <summary>
    /// Pure path logic: maps each <see cref="AudioDefinition"/> asset path to the owning
    /// <see cref="AudioDatabase"/> asset path (closest parent folder wins).
    /// </summary>
    public static class AudioDatabaseAssignment
    {
        public readonly struct Scope
        {
            public readonly string DatabaseAssetPath;
            public readonly string OwnedFolder;

            public Scope(string databaseAssetPath, string ownedFolder)
            {
                DatabaseAssetPath = databaseAssetPath;
                OwnedFolder = ownedFolder;
            }
        }

        public static Dictionary<string, List<string>> BuildMembership(
            IReadOnlyList<string> definitionAssetPaths,
            IReadOnlyList<Scope> databaseScopes,
            string defaultDatabaseAssetPath)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (int i = 0; i < databaseScopes.Count; i++)
            {
                Scope scope = databaseScopes[i];
                if (!result.ContainsKey(scope.DatabaseAssetPath))
                    result[scope.DatabaseAssetPath] = new List<string>();
            }

            if (!string.IsNullOrEmpty(defaultDatabaseAssetPath) && !result.ContainsKey(defaultDatabaseAssetPath))
                result[defaultDatabaseAssetPath] = new List<string>();

            for (int i = 0; i < definitionAssetPaths.Count; i++)
            {
                string definitionPath = Normalize(definitionAssetPaths[i]);
                if (string.IsNullOrEmpty(definitionPath))
                    continue;

                string owner = ResolveOwner(definitionPath, databaseScopes, defaultDatabaseAssetPath);
                if (string.IsNullOrEmpty(owner))
                    continue;

                if (!result.TryGetValue(owner, out List<string> members))
                {
                    members = new List<string>();
                    result[owner] = members;
                }

                members.Add(definitionPath);
            }

            return result;
        }

        public static string ResolveDefaultDatabaseAssetPath(
            IReadOnlyList<Scope> databaseScopes,
            string configuredDefaultPath)
        {
            if (!string.IsNullOrEmpty(configuredDefaultPath))
                return Normalize(configuredDefaultPath);

            if (databaseScopes == null || databaseScopes.Count == 0)
                return null;

            if (databaseScopes.Count == 1)
                return databaseScopes[0].DatabaseAssetPath;

            string shortest = null;
            for (int i = 0; i < databaseScopes.Count; i++)
            {
                string path = databaseScopes[i].DatabaseAssetPath;
                if (string.IsNullOrEmpty(shortest) || path.Length < shortest.Length)
                    shortest = path;
            }

            return shortest;
        }

        private static string ResolveOwner(string definitionAssetPath, IReadOnlyList<Scope> databaseScopes, string defaultDatabaseAssetPath)
        {
            string definitionFolder = GetFolder(definitionAssetPath);
            string bestFolder = null;
            string bestDatabase = null;

            for (int i = 0; i < databaseScopes.Count; i++)
            {
                Scope scope = databaseScopes[i];
                string owned = Normalize(scope.OwnedFolder);
                if (string.IsNullOrEmpty(owned))
                    continue;

                if (!IsUnderFolder(definitionFolder, owned))
                    continue;

                if (bestFolder == null || owned.Length > bestFolder.Length)
                {
                    bestFolder = owned;
                    bestDatabase = Normalize(scope.DatabaseAssetPath);
                }
            }

            if (!string.IsNullOrEmpty(bestDatabase))
                return bestDatabase;

            return string.IsNullOrEmpty(defaultDatabaseAssetPath) ? null : Normalize(defaultDatabaseAssetPath);
        }

        public static string GetOwnedFolder(string databaseAssetPath)
        {
            string normalized = Normalize(databaseAssetPath);
            if (string.IsNullOrEmpty(normalized))
                return null;

            int slash = normalized.LastIndexOf('/');
            if (slash <= 0)
                return "Assets";

            return normalized.Substring(0, slash);
        }

        private static bool IsUnderFolder(string path, string folder)
        {
            if (path.Equals(folder, StringComparison.OrdinalIgnoreCase))
                return true;

            return path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetFolder(string assetPath)
        {
            string normalized = Normalize(assetPath);
            int slash = normalized.LastIndexOf('/');
            if (slash <= 0)
                return normalized;
            return normalized.Substring(0, slash);
        }

        internal static string Normalize(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            return assetPath.Replace('\\', '/').Trim();
        }
    }
}