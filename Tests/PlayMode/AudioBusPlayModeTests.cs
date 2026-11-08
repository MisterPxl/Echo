using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Astra.Echo.Tests
{
    /// <summary>A project adds a bus (and even a playback behaviour) with assets and its own code only.</summary>
    public sealed class AudioBusPlayModeTests
    {
        private AudioClip _clip;
        private AudioBusAsset _sfx;
        private AudioBusAsset _voice;
        private AudioService _service;

        [SetUp]
        public void SetUp()
        {
            _clip = AudioClip.Create("EchoVoiceClip", 44100 * 2, 1, 44100, false);
            _sfx = EchoTestUtil.CreateBus("Sfx", new OneShotPoolPlayback(1, 1));
            _voice = EchoTestUtil.CreateBus("Voice", new OneShotPoolPlayback(1, 2), 0.6f);
            _service = new AudioService();
            AudioServiceSettings settings = new AudioServiceSettings { RootName = "EchoBusRoot", RawClipBus = _sfx };
            _service.Initialize(settings);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            Object.Destroy(_clip);
            Object.Destroy(_sfx);
            Object.Destroy(_voice);
        }

        [UnityTest]
        public IEnumerator UndeclaredVoiceBus_PlaysOnItsOwnPoolUnderMaster()
        {
            AudioDefinition line = EchoTestUtil.CreateDefinition("Line", _clip, "v1", _voice);
            _service.MasterVolume = 0.5f;

            try
            {
                AudioPlaybackResult result = _service.Play(AudioPlaybackRequest.ForDefinition(line));
                yield return null;

                Assert.That(result.Success, Is.True, result.Message);
                GameObject pool = GameObject.Find("Voice Pool");
                Assert.That(pool, Is.Not.Null);
                Assert.That(pool.transform.parent, Is.SameAs(GameObject.Find("EchoBusRoot").transform));
                AudioSource source = pool.GetComponent<AudioSource>();
                Assert.That(source.clip, Is.SameAs(_clip));
                Assert.That(source.volume, Is.EqualTo(0.3f).Within(0.0001f));
                Assert.That(_service.Buses, Is.EqualTo(new[] { _sfx, _voice }));
            }
            finally
            {
                Object.Destroy(line);
            }
        }

        [UnityTest]
        public IEnumerator StoppingTheVoiceBus_LeavesOtherBusesPlaying()
        {
            AudioDefinition line = EchoTestUtil.CreateDefinition("Line", _clip, "v2", _voice);

            try
            {
                _service.Play(AudioPlaybackRequest.ForDefinition(line));
                _service.PlaySfx(_clip);
                yield return null;
                if (!_service.IsPlaying(_voice) || !_service.IsPlaying(_sfx)) Assert.Ignore("No audio output device available.");

                _service.Stop(_voice);

                Assert.That(_service.IsPlaying(_voice), Is.False);
                Assert.That(_service.IsPlaying(_sfx), Is.True);
            }
            finally
            {
                Object.Destroy(line);
            }
        }

        [UnityTest]
        public IEnumerator ProjectPlayback_IsUsedThroughThePublicExtensionPoint()
        {
            CountingPlayback playback = new CountingPlayback();
            AudioBusAsset ui = EchoTestUtil.CreateBus("Ui", playback, 0.5f);
            AudioDefinition click = EchoTestUtil.CreateDefinition("Click", _clip, "u1", ui);

            try
            {
                AudioPlaybackResult first = _service.Play(AudioPlaybackRequest.ForDefinition(click));
                AudioPlaybackResult second = _service.Play(AudioPlaybackRequest.ForDefinition(click));
                _service.MasterVolume = 0.5f;
                yield return null;

                Assert.That(first.Success, Is.True, first.Message);
                Assert.That(second.Success, Is.True, second.Message);
                Assert.That(playback.Channels, Is.EqualTo(1), "one channel per bus and root");
                Assert.That(playback.Last.Acquired, Is.EqualTo(2));
                Assert.That(first.Handle, Is.SameAs(playback.Last));
                Assert.That(playback.Last.LastEffectiveVolume, Is.EqualTo(0.25f).Within(0.0001f));
                Assert.That(playback.Last.Source.volume, Is.EqualTo(0.5f).Within(0.0001f), "volume at play time");
            }
            finally
            {
                Object.Destroy(click);
                Object.Destroy(ui);
            }
        }

        [Serializable]
        private sealed class CountingPlayback : AudioBusPlayback
        {
            public int Channels;
            public CountingChannel Last;

            public override AudioBusChannel CreateChannel(AudioBusAsset bus, Transform parent)
            {
                GameObject go = new GameObject(bus.DisplayName + " Counting");
                go.transform.SetParent(parent, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                Channels++;
                Last = new CountingChannel(bus, source);
                return Last;
            }
        }

        private sealed class CountingChannel : AudioBusChannel, IAudioHandle
        {
            public readonly AudioSource Source;
            public int Acquired;
            public float LastEffectiveVolume = -1f;

            public CountingChannel(AudioBusAsset bus, AudioSource source) : base(bus)
            {
                Source = source;
            }

            public override bool IsPlaying => Source.isPlaying;
            public override bool IsSpatial => false;

            public override AudioSource Acquire(out string failure)
            {
                failure = null;
                Acquired++;
                return Source;
            }

            public override IAudioHandle HandleFor(AudioSource source) => this;
            public override void OnVolumeChanged(float effectiveVolume) => LastEffectiveVolume = effectiveVolume;
            public override void Stop() => Source.Stop();
        }
    }
}
