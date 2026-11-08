using System.Collections.Generic;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Asset that aggregates every <see cref="AudioDefinition"/> available at runtime.
    /// The editor pipeline keeps the serialized list in sync; runtime builds a
    /// lookup dictionary on demand for O(1) resolution.
    ///
    /// <para>Validation: duplicate ids, null entries and definitions without a bus are reported
    /// (only) on <see cref="LogValidation"/> — kept off the hot path on purpose.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "Astra/Echo/Audio Database")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioDatabase")]
    public sealed class AudioDatabase : ScriptableObject, IAudioDatabase
    {
        [SerializeField] private List<AudioDefinition> _definitions = new List<AudioDefinition>();

        private Dictionary<AudioId, AudioDefinition> _byId;
        private Dictionary<AudioClip, AudioDefinition> _byClip;
        private Dictionary<string, AudioDefinition> _byLabel;

        public IReadOnlyList<AudioDefinition> Definitions => _definitions;

        private void OnEnable()
        {
            BuildIndex();
        }

        private void OnValidate()
        {
            BuildIndex();
        }

        private void BuildIndex()
        {
            _byId = new Dictionary<AudioId, AudioDefinition>(_definitions.Count);
            _byClip = new Dictionary<AudioClip, AudioDefinition>(_definitions.Count);
            _byLabel = new Dictionary<string, AudioDefinition>(_definitions.Count);

            for (int i = 0; i < _definitions.Count; i++)
            {
                AudioDefinition def = _definitions[i];
                if (def == null) continue;

                if (def.Id.IsValid && !_byId.ContainsKey(def.Id))
                    _byId[def.Id] = def;

                if (def.Clip != null && !_byClip.ContainsKey(def.Clip))
                    _byClip[def.Clip] = def;

                string label = def.Label;
                if (!string.IsNullOrEmpty(label) && !_byLabel.ContainsKey(label))
                    _byLabel[label] = def;
            }
        }

        public bool TryResolve(AudioId id, out AudioDefinition definition)
        {
            if (_byId == null) BuildIndex();
            return _byId.TryGetValue(id, out definition);
        }

        public bool TryResolve(AudioClip clip, out AudioDefinition definition)
        {
            if (_byClip == null) BuildIndex();
            if (clip == null) { definition = null; return false; }
            return _byClip.TryGetValue(clip, out definition);
        }

        public bool TryResolveByLabel(string label, out AudioDefinition definition)
        {
            if (_byLabel == null) BuildIndex();
            if (string.IsNullOrEmpty(label)) { definition = null; return false; }
            return _byLabel.TryGetValue(label, out definition);
        }

        public bool Contains(AudioId id)
        {
            if (_byId == null) BuildIndex();
            return _byId.ContainsKey(id);
        }

        /// <summary>
        /// Editor-only: replaces the serialized list and rebuilds the runtime index.
        /// </summary>
        internal void EditorSetDefinitions(IReadOnlyList<AudioDefinition> definitions)
        {
            _definitions.Clear();
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null)
                    _definitions.Add(definitions[i]);
            }
            BuildIndex();
        }

        /// <summary>
        /// Editor-only: registers a single definition if not already present.
        /// </summary>
        internal bool EditorAddDefinition(AudioDefinition definition)
        {
            if (definition == null) return false;
            if (_definitions.Contains(definition)) return false;
            _definitions.Add(definition);
            BuildIndex();
            return true;
        }

        /// <summary>
        /// Editor-only diagnostics: writes warnings for duplicate ids or null entries.
        /// </summary>
        public List<string> EditorValidate()
        {
            List<string> warnings = new List<string>();
            HashSet<AudioId> seenIds = new HashSet<AudioId>();
            for (int i = 0; i < _definitions.Count; i++)
            {
                AudioDefinition def = _definitions[i];
                if (def == null)
                {
                    warnings.Add($"Slot {i} is null.");
                    continue;
                }

                if (!def.Id.IsValid)
                {
                    warnings.Add($"Definition '{def.name}' has no valid AudioId.");
                    continue;
                }

                if (!seenIds.Add(def.Id))
                    warnings.Add($"Duplicate AudioId for '{def.name}' ({def.Id}).");

                if (def.Bus == null)
                    warnings.Add($"Definition '{def.name}' has no bus.");
            }
            return warnings;
        }
    }
}
