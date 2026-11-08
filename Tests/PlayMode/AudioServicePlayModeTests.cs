using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Astra.Echo.Tests
{
    public sealed class AudioServicePlayModeTests
    {
        private AudioClip _clip;
        private AudioClip _other;
        private AudioBusAsset _music;
        private AudioBusAsset _ambient;
        private AudioBusAsset _sfx;
        private AudioService _service;

        [SetUp]
        public void SetUp()
        {
            _clip = AudioClip.Create("EchoSilentClip", 44100 * 2, 1, 44100, false);
            _other = AudioClip.Create("EchoSilentClip2", 44100 * 2, 1, 44100, false);
            _music = EchoTestUtil.CreateBus("Music", new SingleTrackPlayback(), 0.8f);
            _ambient = EchoTestUtil.CreateBus("Ambient", new SingleTrackPlayback(), 0.7f);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _service = null;
            if (_clip != null) Object.Destroy(_clip);
            if (_other != null) Object.Destroy(_other);
            if (_music != null) Object.Destroy(_music);
            if (_ambient != null) Object.Destroy(_ambient);
            if (_sfx != null) Object.Destroy(_sfx);
        }

        private static void RequireAudioDevice(AudioSource source)
        {
            if (!source.isPlaying) Assert.Ignore("No audio output device available.");
        }

        private AudioService CreateService(int poolInitial, int poolMax, IEchoLogger logger = null, bool verbose = false)
        {
            _sfx = EchoTestUtil.CreateBus("Sfx", new OneShotPoolPlayback(poolInitial, poolMax));
            _service = new AudioService(logger, verbose);
            _service.Initialize(Settings("EchoTestRoot"));
            return _service;
        }

        private AudioServiceSettings Settings(string rootName)
        {
            AudioServiceSettings settings = new AudioServiceSettings { RootName = rootName, RawClipBus = _sfx };
            settings.Buses.Add(_music);
            settings.Buses.Add(_ambient);
            return settings;
        }

        private static AudioSource PoolSource(AudioBusAsset bus)
        {
            return GameObject.Find(bus.DisplayName + " Pool").GetComponent<AudioSource>();
        }

        [UnityTest]
        public IEnumerator Initialize_BuildsTheRootWithEveryConfiguredBus()
        {
            CreateService(3, 5);
            yield return null;

            GameObject root = GameObject.Find("EchoTestRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(3 + 2), "3 pooled sources + music + ambient tracks");
            Assert.That(_service.Buses, Is.EqualTo(new[] { _music, _ambient, _sfx }));
        }

        [UnityTest]
        public IEnumerator FreshPooledSources_AreNotPlaying()
        {
            CreateService(2, 2);
            yield return null;

            AudioSource[] sources = GameObject.Find("Sfx Pool").GetComponents<AudioSource>();
            Assert.That(sources.Length, Is.EqualTo(2));
            for (int i = 0; i < sources.Length; i++)
                Assert.That(sources[i].isPlaying, Is.False, "play-on-awake must not leak into a new pooled source");
            Assert.That(_service.IsPlaying(_music), Is.False);
            Assert.That(_service.IsPlaying(_ambient), Is.False);
        }

        [UnityTest]
        public IEnumerator PlaySfx_UsesTheRawClipBusAndAppliesMaster()
        {
            CreateService(1, 1);
            _service.MasterVolume = 0.5f;
            _service.SetVolume(_sfx, 0.4f);

            AudioPlaybackResult result = _service.PlaySfx(_clip, 0.3f);
            yield return null;

            Assert.That(result.Success, Is.True, result.Message);
            AudioSource source = PoolSource(_sfx);
            Assert.That(source.clip, Is.SameAs(_clip));
            Assert.That(source.volume, Is.EqualTo(0.06f).Within(0.00001f));
            Assert.That(source.loop, Is.False);

            _service.Stop(_sfx);
            Assert.That(source.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator PlaySfx_WithoutRawClipBusFails()
        {
            _service = new AudioService();
            _service.Initialize(new AudioServiceSettings { RootName = "EchoNoRawRoot" });
            yield return null;

            AudioPlaybackResult result = _service.PlaySfx(_clip);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("RawClipBus"));
        }

        [UnityTest]
        public IEnumerator Pool_HardCapDropsPlaybackAndWarns()
        {
            EchoTestUtil.RecordingLogger logger = new EchoTestUtil.RecordingLogger();
            CreateService(1, 1, logger, false);

            AudioDefinition definition = EchoTestUtil.CreateDefinition("Sat", _clip, "feed", _sfx);
            AudioPlaybackResult first = _service.Play(AudioPlaybackRequest.ForDefinition(definition));
            yield return null;
            AudioPlaybackResult second = _service.Play(AudioPlaybackRequest.ForDefinition(definition));

            try
            {
                Assert.That(first.Success, Is.True, first.Message);
                RequireAudioDevice(PoolSource(_sfx));
                Assert.That(second.Success, Is.False);
                Assert.That(second.Message, Is.EqualTo("Bus 'Sfx': pool exhausted (1 sources)."));
                Assert.That(logger.Warnings, Has.Some.Contains("pool exhausted"));
            }
            finally
            {
                Object.Destroy(definition);
            }
        }

        [UnityTest]
        public IEnumerator Definition_WithoutBusFails()
        {
            CreateService(1, 1);
            AudioDefinition definition = EchoTestUtil.CreateDefinition("Orphan", _clip, "0ff");
            yield return null;

            try
            {
                AudioPlaybackResult result = _service.Play(AudioPlaybackRequest.ForDefinition(definition));
                Assert.That(result.Success, Is.False);
                Assert.That(result.Message, Is.EqualTo("Definition 'Orphan' has no bus."));
            }
            finally
            {
                Object.Destroy(definition);
            }
        }

        [UnityTest]
        public IEnumerator Music_ReplacesTheCurrentTrackAndStops()
        {
            CreateService(1, 1);
            AudioDefinition first = EchoTestUtil.CreateDefinition("MusicA", _clip, "m1", _music);
            AudioDefinition second = EchoTestUtil.CreateDefinition("MusicB", _other, "m2", _music);

            try
            {
                AudioPlaybackResult result = _service.Play(AudioPlaybackRequest.ForDefinition(first));
                AudioPlayer player = (AudioPlayer)result.Handle;
                yield return null;
                if (!_service.IsPlaying(_music)) Assert.Ignore("No audio output device available.");
                Assert.That(player.CurrentClip, Is.SameAs(_clip));

                AudioPlaybackResult replaced = _service.Play(AudioPlaybackRequest.ForDefinition(second));
                yield return null;
                Assert.That(replaced.Handle, Is.SameAs(player));
                Assert.That(player.CurrentClip, Is.SameAs(_other));

                _service.Stop(_music);
                Assert.That(_service.IsPlaying(_music), Is.False);
            }
            finally
            {
                Object.Destroy(first);
                Object.Destroy(second);
            }
        }

        [UnityTest]
        public IEnumerator Track_FollowsMasterAndBusVolume()
        {
            CreateService(1, 1);
            AudioDefinition definition = EchoTestUtil.CreateDefinition("Theme", _clip, "t1", _music);

            try
            {
                AudioPlaybackResult result = _service.Play(AudioPlaybackRequest.ForDefinition(definition));
                AudioSource source = ((AudioPlayer)result.Handle).GetComponent<AudioSource>();
                yield return null;
                if (!_service.IsPlaying(_music)) Assert.Ignore("No audio output device available.");
                Assert.That(source.volume, Is.EqualTo(0.8f).Within(0.0001f));

                _service.MasterVolume = 0.5f;
                Assert.That(source.volume, Is.EqualTo(0.4f).Within(0.0001f));

                _service.SetVolume(_music, 0.2f);
                Assert.That(source.volume, Is.EqualTo(0.1f).Within(0.0001f));
            }
            finally
            {
                Object.Destroy(definition);
            }
        }

        [UnityTest]
        public IEnumerator Ambient_PlaysOnItsOwnChannelAndStops()
        {
            CreateService(1, 1);
            AudioDefinition ambient = EchoTestUtil.CreateDefinition("Amb", _clip, "a1", _ambient);

            try
            {
                _service.Play(AudioPlaybackRequest.ForDefinition(ambient));
                yield return null;
                if (!_service.IsPlaying(_ambient)) Assert.Ignore("No audio output device available.");
                Assert.That(_service.IsPlaying(_music), Is.False);

                _service.Stop(_ambient);
                Assert.That(_service.IsPlaying(_ambient), Is.False);
            }
            finally
            {
                Object.Destroy(ambient);
            }
        }

        [UnityTest]
        public IEnumerator FadeIn_RampsTheVolumeUp()
        {
            CreateService(1, 1);
            FadeInOption fade = EchoTestUtil.FromJson<FadeInOption>("{\"_durationSeconds\":0.3}");
            AudioDefinition definition = EchoTestUtil.CreateDefinition("Fade", _clip, "f1", _sfx);
            AudioSource source = PoolSource(_sfx);

            try
            {
                AudioPlaybackRequest request = AudioPlaybackRequest.ForDefinition(definition);
                request.Options = new System.Collections.Generic.List<ISoundOption> { fade };
                _service.Play(in request);
                Assert.That(source.volume, Is.EqualTo(0f).Within(0.0001f), "starts silent");
                RequireAudioDevice(source);

                yield return new WaitForSecondsRealtime(0.6f);
                Assert.That(source.volume, Is.EqualTo(1f).Within(0.01f));
            }
            finally
            {
                Object.Destroy(definition);
                if (AudioCoroutineHost.Instance != null) Object.Destroy(AudioCoroutineHost.Instance.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator DelayedPlay_DoesNotThrow()
        {
            CreateService(1, 1);
            AudioDefinition definition = EchoTestUtil.CreateDefinition("Delay", _clip, "d1", _sfx);
            AudioPlaybackRequest request = AudioPlaybackRequest.ForDefinition(definition);
            request.Options = new System.Collections.Generic.List<ISoundOption> { new DelayedPlayOption() };

            try
            {
                AudioPlaybackResult result = _service.Play(in request);
                yield return null;
                Assert.That(result.Success, Is.True, result.Message);
            }
            finally
            {
                Object.Destroy(definition);
            }
        }

        [UnityTest]
        public IEnumerator Verbose_LogsInfoOnlyWhenEnabledAndSupported()
        {
            EchoTestUtil.RecordingLogger quiet = new EchoTestUtil.RecordingLogger();
            AudioService quietService = new AudioService(quiet, false);
            quietService.Initialize(Settings("EchoQuietRoot"));
            quietService.Stop(_music);

            EchoTestUtil.RecordingLogger loud = new EchoTestUtil.RecordingLogger();
            CreateService(1, 1, loud, true);
            _service.Stop(_music);
            yield return null;

            try
            {
                Assert.That(quiet.Infos, Is.Empty);
                Assert.That(loud.Infos, Has.Some.Contains("Bus 'Music' stopped"));
            }
            finally
            {
                quietService.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Dispose_DestroysTheRoot()
        {
            CreateService(1, 1);
            yield return null;
            Assert.That(GameObject.Find("EchoTestRoot"), Is.Not.Null);

            _service.Dispose();
            yield return null;

            Assert.That(GameObject.Find("EchoTestRoot"), Is.Null);
            Assert.That(_service.Play(new AudioId("x")).Success, Is.False);
            Assert.That(_service.IsPlaying(_music), Is.False);
        }

        [UnityTest]
        public IEnumerator Initialize_TwiceReplacesTheRoot()
        {
            CreateService(1, 1);
            yield return null;
            _service.Initialize(Settings("EchoSecondRoot"));
            yield return null;

            Assert.That(GameObject.Find("EchoTestRoot"), Is.Null);
            Assert.That(GameObject.Find("EchoSecondRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("EchoSecondRoot").GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(1 + 2), "1 pooled source + music + ambient tracks");
        }

        [UnityTest]
        public IEnumerator Initialize_WithAParentScopesTheRootToIt()
        {
            GameObject parent = new GameObject("EchoParent");
            try
            {
                _service = new AudioService();
                _service.Initialize(Settings("EchoChildRoot"), parent.transform);
                yield return null;

                Assert.That(parent.transform.Find("EchoChildRoot"), Is.Not.Null);
            }
            finally
            {
                Object.Destroy(parent);
            }
        }

        [UnityTest]
        public IEnumerator MinimalProjectSmoke_NoFrameworkRequired()
        {
            _sfx = EchoTestUtil.CreateBus("Sfx", new OneShotPoolPlayback(2, 4));
            AudioDefinition definition = EchoTestUtil.CreateDefinition("Smoke", _clip, "5m0k3", _sfx);
            AudioDatabase database = EchoTestUtil.CreateDatabase(definition);
            _service = new AudioService();
            _service.Initialize(new AudioServiceSettings { RootName = "EchoSmokeRoot", Database = database });

            try
            {
                AudioPlaybackResult byId = _service.Play(new AudioId("5m0k3"));
                AudioPlaybackResult fluent = _service.CreateRequest(new AudioId("5m0k3")).WithVolume(0.5f).WithPitch(1.2f).Play();
                yield return null;

                Assert.That(byId.Success, Is.True, byId.Message);
                Assert.That(fluent.Success, Is.True, fluent.Message);
                Assert.That(_service.Play(new AudioId("missing")).Success, Is.False);
                Assert.That(_service.Buses, Is.EqualTo(new[] { _sfx }), "a bus met through a definition is created on demand");
            }
            finally
            {
                Object.Destroy(database);
                Object.Destroy(definition);
            }
        }
    }
}
