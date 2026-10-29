using UnityEngine;
using UnityEngine.Audio;

namespace Astra.Echo
{
    /// <summary>
    /// Persistent root that hosts the music / ambient single-channel players and the SFX pool.
    /// Created once during initialization, parented to <c>PersistentAppRoot</c> when present.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio", "Core.Audio", "AudioRoot")]
    public sealed class AudioRoot : MonoBehaviour
    {
        public AudioPlayer MusicPlayer { get; private set; }
        public AudioPlayer AmbientPlayer { get; private set; }
        public AudioSourcePool SfxPool { get; private set; }

        internal static AudioRoot Create(
            string rootName,
            Transform parent,
            int sfxPoolInitial,
            int sfxPoolMax,
            AudioMixerGroup musicGroup,
            AudioMixerGroup sfxGroup,
            AudioMixerGroup ambientGroup)
        {
            GameObject go = new GameObject(rootName);

            if (parent != null)
                go.transform.SetParent(parent, false);
            else
                DontDestroyOnLoad(go);

            AudioRoot root = go.AddComponent<AudioRoot>();

            GameObject musicGo = new GameObject("MusicPlayer");
            musicGo.transform.SetParent(go.transform, false);
            root.MusicPlayer = musicGo.AddComponent<AudioPlayer>();
            root.MusicPlayer.Setup(musicGroup);

            GameObject ambientGo = new GameObject("AmbientPlayer");
            ambientGo.transform.SetParent(go.transform, false);
            root.AmbientPlayer = ambientGo.AddComponent<AudioPlayer>();
            root.AmbientPlayer.Setup(ambientGroup);

            GameObject sfxGo = new GameObject("SfxPool");
            sfxGo.transform.SetParent(go.transform, false);
            root.SfxPool = sfxGo.AddComponent<AudioSourcePool>();
            root.SfxPool.Setup(sfxPoolInitial, sfxPoolMax, sfxGroup);

            return root;
        }
    }
}
