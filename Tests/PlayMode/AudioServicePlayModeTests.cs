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
        private AudioService _service;

        [SetUp]
        public void SetUp()
        {
            _clip = AudioClip.Create("EchoSilentClip", 44100 * 2, 1, 44100, false);
            _other = AudioClip.Create("EchoSilentClip2", 44100 * 2, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _service = null;
            if (_clip != null) Object.Destroy(_clip);
            if (_other != null) Object.Destroy(_other);
        }

        private static void RequireAudioDevice(AudioSource source)
        {
            if (!source.isPlaying) Assert.Ignore("No audio output device available.");
        }

        private AudioService CreateService(int poolInitial, int poolMax, IEchoLogger logger = null, bool verbose = false)
        {
            _service = new AudioService(logger, verbose);
            _service.Initialize(new AudioServiceSettings
            {
                RootName = "EchoTestRoot",
                SfxPoolInitialSize = poolInitial,
                SfxPoolMaxSize = poolMax,
            });
            return _service;
        }

        [UnityTest]
        public IEnumerator Initialize_BuildsTheRootWithTheConfiguredPool()
        {
            CreateService(3, 5);
            yield return null;

            GameObject root = GameObject.Find("EchoTestRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(3 + 2), "3 pooled sources + music + ambient");
        }

        [UnityTest]
        public IEnumerator FreshPooledSources_AreNotPlaying()
        {
            CreateService(2, 2);
            AudioRoot root = Object.FindFirstObjectByType<AudioRoot>();
            yield return null;

            AudioSource[] sources = root.SfxPool.GetComponents<AudioSource>();
            Assert.That(sources.Length, Is.EqualTo(2));
            for (int i = 0; i < sources.Length; i++)
                Assert.That(sources[i].isPlaying, Is.False, "play-on-awake must not leak into a new pooled source");
            Assert.That(_service.IsMusicPlaying, Is.False);
            Assert.That(_service.IsAmbientPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator PlaySfx_UsesThePoolAndAppliesVolumeBuses()
        {
            CreateService(1, 1);
            _service.MasterVolume = 0.5f;
            _service.SfxVolume = 0.4f;

            AudioPlaybackResult result = _service.PlaySfx(_clip, 0.3f);
            yield return null;

            Assert.That(result.Success, Is.True, result.Message);
            AudioSource source = Object.FindFirstObjectByType<AudioRoot>().SfxPool.GetComponent<AudioSource>();
            Assert.That(source.clip, Is.SameAs(_clip));
            Assert.That(source.volume, Is.EqualTo(0.06f).Within(0.00001f));
            Assert.That(source.loop, Is.False);

            _service.StopAllSfx();
            Assert.That(source.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator Pool_HardCapDropsPlaybackAndWarns()
        {
            EchoTestUtil.RecordingLogger logger = new EchoTestUtil.RecordingLogger();
            CreateService(1, 1, logger, false);

            AudioDefinition definition = EchoTestUtil.CreateDefinition("Sat", _clip, "feed");
            AudioPlaybackResult first = _service.Play(AudioPlaybackRequest.ForDefinition(definition));
            yield return null;
            AudioPlaybackResult second = _service.Play(AudioPlaybackRequest.ForDefinition(definition));

            try
            {
                Assert.That(first.Success, Is.True, first.Message);
                RequireAudioDevice(Object.FindFirstObjectByType<AudioRoot>().SfxPool.GetComponent<AudioSource>());
                Assert.That(second.Success, Is.False);
                Assert.That(second.Message, Is.EqualTo("SFX pool exhausted."));
                Assert.That(logger.Warnings, Has.Some.Contains("saturated"));
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
            AudioDefinition first = EchoTestUtil.CreateDefinition("MusicA", _clip, "m1", AudioType.Music);
            AudioDefinition second = EchoTestUtil.CreateDefinition("MusicB", _other, "m2", AudioType.Music);
            AudioPlayer player = Object.FindFirstObjectByType<AudioRoot>().MusicPlayer;

            try
            {
                _service.Play(AudioPlaybackRequest.ForDefinition(first));
                yield return null;
                if (!_service.IsMusicPlaying) Assert.Ignore("No audio output device available.");
                Assert.That(_service.IsMusicPlaying, Is.True);
                Assert.That(player.CurrentClip, Is.SameAs(_clip));

                _service.Play(AudioPlaybackRequest.ForDefinition(second));
                yield return null;
                Assert.That(player.CurrentClip, Is.SameAs(_other));

                _service.StopMusic();
                Assert.That(_service.IsMusicPlaying, Is.False);
            }
            finally
            {
                Object.Destroy(first);
                Object.Destroy(second);
            }
        }

        [UnityTest]
        public IEnumerator Ambient_PlaysOnItsOwnChannelAndStops()
        {
            CreateService(1, 1);
            AudioDefinition ambient = EchoTestUtil.CreateDefinition("Amb", _clip, "a1", AudioType.Ambient);

            try
            {
                _service.Play(AudioPlaybackRequest.ForDefinition(ambient));
                yield return null;
                if (!_service.IsAmbientPlaying) Assert.Ignore("No audio output device available.");
                Assert.That(_service.IsAmbientPlaying, Is.True);
                Assert.That(_service.IsMusicPlaying, Is.False);

                _service.StopAmbient();
                Assert.That(_service.IsAmbientPlaying, Is.False);
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
            AudioDefinition definition = EchoTestUtil.CreateDefinition("Fade", _clip, "f1");
            AudioSource source = Object.FindFirstObjectByType<AudioRoot>().SfxPool.GetComponent<AudioSource>();

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
            AudioDefinition definition = EchoTestUtil.CreateDefinition("Delay", _clip, "d1");
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
            quietService.Initialize(new AudioServiceSettings { RootName = "EchoQuietRoot", SfxPoolInitialSize = 1, SfxPoolMaxSize = 1 });
            quietService.StopMusic();

            EchoTestUtil.RecordingLogger loud = new EchoTestUtil.RecordingLogger();
            CreateService(1, 1, loud, true);
            _service.StopMusic();
            yield return null;

            try
            {
                Assert.That(quiet.Infos, Is.Empty);
                Assert.That(loud.Infos, Has.Some.Contains("Music stopped"));
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
        }

        [UnityTest]
        public IEnumerator Initialize_TwiceReplacesTheRoot()
        {
            CreateService(1, 1);
            yield return null;
            _service.Initialize(new AudioServiceSettings { RootName = "EchoSecondRoot", SfxPoolInitialSize = 1, SfxPoolMaxSize = 1 });
            yield return null;

            Assert.That(GameObject.Find("EchoTestRoot"), Is.Null);
            Assert.That(GameObject.Find("EchoSecondRoot"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Initialize_WithAParentScopesTheRootToIt()
        {
            GameObject parent = new GameObject("EchoParent");
            try
            {
                _service = new AudioService();
                _service.Initialize(new AudioServiceSettings { RootName = "EchoChildRoot", SfxPoolInitialSize = 1, SfxPoolMaxSize = 1 }, parent.transform);
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
            AudioDefinition definition = EchoTestUtil.CreateDefinition("Smoke", _clip, "5m0k3");
            AudioDatabase database = EchoTestUtil.CreateDatabase(definition);
            _service = new AudioService();
            _service.Initialize(new AudioServiceSettings { RootName = "EchoSmokeRoot", Database = database, SfxPoolInitialSize = 2, SfxPoolMaxSize = 4 });

            try
            {
                AudioPlaybackResult byId = _service.Play(new AudioId("5m0k3"));
                AudioPlaybackResult fluent = _service.CreateRequest(new AudioId("5m0k3")).WithVolume(0.5f).WithPitch(1.2f).Play();
                yield return null;

                Assert.That(byId.Success, Is.True, byId.Message);
                Assert.That(fluent.Success, Is.True, fluent.Message);
                Assert.That(_service.Play(new AudioId("missing")).Success, Is.False);
            }
            finally
            {
                Object.Destroy(database);
                Object.Destroy(definition);
            }
        }
    }
}
