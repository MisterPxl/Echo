using System;
using UnityEngine;

namespace Astra.Echo
{
    /// <summary>
    /// Pooled one-shots: every playback takes an idle source of the bus pool, so sounds overlap (SFX, UI, voice).
    /// Spatial settings apply. Running one-shots keep the volume they started with.
    /// </summary>
    [Serializable]
    public sealed class OneShotPoolPlayback : AudioBusPlayback
    {
        [Tooltip("Sources created with the channel.")]
        [Min(1)] [SerializeField] private int _initialSize = 8;

        [Tooltip("Hard cap on simultaneous sources of this bus. 0 = unlimited.")]
        [Min(0)] [SerializeField] private int _maxSize = 32;

        public OneShotPoolPlayback()
        {
        }

        public OneShotPoolPlayback(int initialSize, int maxSize)
        {
            _initialSize = initialSize;
            _maxSize = maxSize;
        }

        public int InitialSize => Mathf.Max(1, _initialSize);
        public int MaxSize => Mathf.Max(0, _maxSize);

        public override AudioBusChannel CreateChannel(AudioBusAsset bus, Transform parent)
        {
            GameObject go = new GameObject(bus.DisplayName + " Pool");
            go.transform.SetParent(parent, false);
            AudioSourcePool pool = go.AddComponent<AudioSourcePool>();
            pool.Setup(InitialSize, MaxSize, bus.MixerGroup);
            return new Channel(bus, pool, MaxSize);
        }

        private sealed class Channel : AudioBusChannel
        {
            private readonly AudioSourcePool _pool;
            private readonly int _maxSize;

            public Channel(AudioBusAsset bus, AudioSourcePool pool, int maxSize) : base(bus)
            {
                _pool = pool;
                _maxSize = maxSize;
            }

            public override bool IsPlaying => _pool != null && _pool.IsAnyPlaying;
            public override bool IsSpatial => true;

            public override AudioSource Acquire(out string failure)
            {
                if (_pool == null)
                {
                    failure = $"Bus '{Bus.DisplayName}': pool destroyed.";
                    return null;
                }

                AudioSource source = _pool.Acquire();
                failure = source != null ? null : $"Bus '{Bus.DisplayName}': pool exhausted ({_maxSize} sources).";
                return source;
            }

            public override IAudioHandle HandleFor(AudioSource source) => null;

            public override void OnVolumeChanged(float effectiveVolume)
            {
            }

            public override void Stop()
            {
                if (_pool != null) _pool.StopAll();
            }
        }
    }
}
