using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Persistent root hosting the channel of every bus (track players, one-shot pools).
    /// Created once during initialization, parented to <c>PersistentAppRoot</c> when present.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioRoot")]
    public sealed class AudioRoot : MonoBehaviour
    {
        internal static AudioRoot Create(string rootName, Transform parent)
        {
            GameObject go = new GameObject(rootName);

            if (parent != null)
                go.transform.SetParent(parent, false);
            else
                DontDestroyOnLoad(go);

            return go.AddComponent<AudioRoot>();
        }
    }
}
