using System.Collections.Generic;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Read-side contract over the audio asset database.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "IAudioDatabase")]
    public interface IAudioDatabase
    {
        /// <summary>
        /// All registered definitions, in stable (asset-driven) order.
        /// </summary>
        IReadOnlyList<AudioDefinition> Definitions { get; }

        bool TryResolve(AudioId id, out AudioDefinition definition);
        bool TryResolve(AudioClip clip, out AudioDefinition definition);
        bool TryResolveByLabel(string label, out AudioDefinition definition);

        bool Contains(AudioId id);
    }
}
