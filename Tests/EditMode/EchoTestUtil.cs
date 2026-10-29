using System.Collections.Generic;
using UnityEngine;

namespace Astra.Echo.Tests
{
    /// <summary>Shared builders for the Echo tests. Everything created here must be released with <see cref="Destroy"/>.</summary>
    internal static class EchoTestUtil
    {
        public static AudioClip CreateClip(string name = "TestClip", float seconds = 1f)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(44100f * seconds));
            return AudioClip.Create(name, samples, 1, 44100, false);
        }

        public static AudioSource CreateSource(out GameObject owner)
        {
            owner = new GameObject("EchoTestSource");
            AudioSource source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        public static AudioPlaybackContext CreateContext(AudioDefinition definition = null, float volume = 1f, float basePitch = 1f)
        {
            return new AudioPlaybackContext(definition, AudioType.Sfx, volume, basePitch);
        }

        /// <summary>Sets the private serialized fields of a plain [Serializable] option or modulator.</summary>
        public static T FromJson<T>(string json) where T : new()
        {
            T instance = new T();
            JsonUtility.FromJsonOverwrite(json, instance);
            return instance;
        }

        public static AudioDefinition CreateDefinition(string name, AudioClip clip, string guid, AudioType type = AudioType.Sfx)
        {
            AudioDefinition definition = ScriptableObject.CreateInstance<AudioDefinition>();
            definition.name = name;
            definition.EditorSetClip(clip);
            definition.EditorSetType(type);
            definition.EditorSetId(guid == null ? AudioId.None : new AudioId(guid));
            return definition;
        }

        public static AudioDatabase CreateDatabase(params AudioDefinition[] definitions)
        {
            AudioDatabase database = ScriptableObject.CreateInstance<AudioDatabase>();
            database.EditorSetDefinitions(new List<AudioDefinition>(definitions));
            return database;
        }

        public static void Destroy(params Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            }
        }

        /// <summary>Records every message; verbosity of <see cref="IsInfoEnabled"/> is configurable.</summary>
        public sealed class RecordingLogger : IEchoLogger
        {
            public readonly List<string> Infos = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public readonly List<string> Errors = new List<string>();

            public bool InfoEnabled = true;

            public bool IsInfoEnabled => InfoEnabled;
            public void Info(string message) { Infos.Add(message); }
            public void Warn(string message) { Warnings.Add(message); }
            public void Error(string message) { Errors.Add(message); }
        }
    }
}
