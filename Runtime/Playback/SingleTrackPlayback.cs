using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// One persistent track: playing a definition on the bus replaces the current one (music, ambient).
    /// The running track follows Master and bus volume changes. Spatial settings are not applied.
    /// </summary>
    [Serializable]
    public sealed class SingleTrackPlayback : AudioBusPlayback
    {
        public override AudioBusChannel CreateChannel(AudioBusAsset bus, Transform parent)
        {
            GameObject go = new GameObject(bus.DisplayName + " Track");
            go.transform.SetParent(parent, false);
            AudioPlayer player = go.AddComponent<AudioPlayer>();
            player.Setup(bus.MixerGroup);
            return new Channel(bus, player);
        }

        private sealed class Channel : AudioBusChannel
        {
            private readonly AudioPlayer _player;

            public Channel(AudioBusAsset bus, AudioPlayer player) : base(bus)
            {
                _player = player;
            }

            public override bool IsPlaying => _player != null && _player.IsPlaying;
            public override bool IsSpatial => false;

            public override AudioSource Acquire(out string failure)
            {
                if (_player == null)
                {
                    failure = $"Bus '{Bus.DisplayName}': track player destroyed.";
                    return null;
                }

                failure = null;
                return _player.Source;
            }

            public override IAudioHandle HandleFor(AudioSource source) => _player;

            public override void OnVolumeChanged(float effectiveVolume)
            {
                if (IsPlaying) _player.SetVolume(effectiveVolume);
            }

            public override void Stop()
            {
                if (_player != null) _player.Stop();
            }
        }
    }
}
