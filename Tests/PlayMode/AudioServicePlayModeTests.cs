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
            AudioSource[] sources = GameObject.Find(bus.DisplayName + " Pool").GetComponentsInChildren<AudioSource>();
            Assert.That(sources.Length, Is.GreaterThan(0));
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].isPlaying)
                    return sources[i];
            }

            return sources[0];
        }

        private static AudioDefinition CreateSpatialDefinition(string name, AudioClip clip, string guid, AudioBusAsset bus)
        {
            AudioDefinition definition = EchoTestUtil.CreateDefinition(name, clip, guid, bus);
            JsonUtility.FromJsonOverwrite("{\"_spatialBlend\":1}", definition);
            return definition;
        }

        [UnityTest]
        public IEnumerator Initialize_BuildsTheRootWithEveryConfiguredBus()
        {
            CreateService(3, 5);
            yield return null;

            GameObject root = GameObject.Find("EchoTestRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(3 + 4), "3 pooled sources + music and ambient tracks (staging + active each)");
            Assert.That(_service.Buses, Is.EqualTo(new[] { _music, _ambient, _sfx }));
        }

        [UnityTest]
        public IEnumerator FreshPooledSources_AreNotPlaying()
        {
            CreateService(2, 2);
            yield return null;

            AudioSource[] sources = GameObject.Find("Sfx Pool").GetComponentsInChildren<AudioSource>();
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
                AudioSource source = ((AudioPlayer)result.Handle).ActiveSource;
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
            Assert.That(GameObject.Find("EchoSecondRoot").GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(1 + 4), "1 pooled source + music and ambient tracks (staging + active each)");
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
        public IEnumerator SpatialPool_TwoPlaybacksKeepSeparateWorldPositions()
        {
            CreateService(2, 4);
            AudioDefinition first = CreateSpatialDefinition("Near", _clip, "sp1", _sfx);
            AudioDefinition second = CreateSpatialDefinition("Far", _other, "sp2", _sfx);
            Vector3 near = new Vector3(1f, 0f, 2f);
            Vector3 far = new Vector3(-3f, 1f, 0.5f);

            try
            {
                AudioPlaybackRequest firstRequest = AudioPlaybackRequest.ForDefinition(first);
                firstRequest.WorldPosition = near;
                AudioPlaybackResult one = _service.Play(in firstRequest);
                yield return null;

                AudioPlaybackRequest secondRequest = AudioPlaybackRequest.ForDefinition(second);
                secondRequest.WorldPosition = far;
                AudioPlaybackResult two = _service.Play(in secondRequest);
                yield return null;

                Assert.That(one.Success, Is.True, one.Message);
                Assert.That(two.Success, Is.True, two.Message);

                AudioSource[] sources = GameObject.Find("Sfx Pool").GetComponentsInChildren<AudioSource>();
                int playing = 0;
                AudioSource firstPlaying = null;
                AudioSource secondPlaying = null;
                for (int i = 0; i < sources.Length; i++)
                {
                    if (!sources[i].isPlaying)
                        continue;

                    playing++;
                    if (firstPlaying == null)
                        firstPlaying = sources[i];
                    else
                        secondPlaying = sources[i];
                }

                if (playing == 0)
                    Assert.Ignore("No audio output device available.");

                Assert.That(playing, Is.EqualTo(2));
                Assert.That(firstPlaying.transform.position, Is.EqualTo(near));
                Assert.That(secondPlaying.transform.position, Is.EqualTo(far));
            }
            finally
            {
                Object.Destroy(first);
                Object.Destroy(second);
            }
        }

        [UnityTest]
        public IEnumerator Music_VetoedPlaybackLeavesTheActiveTrackUntouched()
        {
            CreateService(1, 1);
            AudioDefinition first = EchoTestUtil.CreateDefinition("TrackA", _clip, "v1", _music);
            AudioDefinition second = EchoTestUtil.CreateDefinition("TrackB", _other, "v2", _music);
            CooldownModulator cooldown = EchoTestUtil.FromJson<CooldownModulator>("{\"_cooldownSeconds\":10}");

            try
            {
                AudioPlaybackRequest start = AudioPlaybackRequest.ForDefinition(first);
                start.Modulators = new System.Collections.Generic.List<ISoundModulator> { cooldown };
                AudioPlaybackResult started = _service.Play(in start);
                yield return null;
                if (!started.Success)
                    Assert.Ignore("No audio output device available.");

                AudioPlayer player = (AudioPlayer)started.Handle;
                AudioClip clipBefore = player.CurrentClip;
                float volumeBefore = player.ActiveSource.volume;
                float pitchBefore = player.ActiveSource.pitch;

                AudioPlaybackRequest vetoed = AudioPlaybackRequest.ForDefinition(second);
                vetoed.Modulators = new System.Collections.Generic.List<ISoundModulator> { cooldown };
                AudioPlaybackResult veto = _service.Play(in vetoed);

                Assert.That(veto.Success, Is.False);
                Assert.That(veto.Message, Does.Contain("vetoed"));
                Assert.That(player.CurrentClip, Is.SameAs(clipBefore));
                Assert.That(player.ActiveSource.volume, Is.EqualTo(volumeBefore).Within(0.0001f));
                Assert.That(player.ActiveSource.pitch, Is.EqualTo(pitchBefore).Within(0.0001f));
                Assert.That(player.ActiveSource.isPlaying, Is.True);
            }
            finally
            {
                Object.Destroy(first);
                Object.Destroy(second);
            }
        }

        [UnityTest]
        public IEnumerator Registry_AddedDatabaseResolves_AndRemoveStopsPlayback()
        {
            CreateService(2, 4);
            AudioBusAsset sfxLocal = _sfx;
            AudioDefinition bankSound = EchoTestUtil.CreateDefinition("Bank", _other, "bk1", sfxLocal);
            AudioDatabase bank = EchoTestUtil.CreateDatabase(bankSound);

            try
            {
                Assert.That(((IAudioDatabaseRegistry)_service).AddDatabase(bank), Is.True);
                AudioPlaybackResult play = _service.Play(new AudioId("bk1"));
                yield return null;
                Assert.That(play.Success, Is.True, play.Message);

                Assert.That(((IAudioDatabaseRegistry)_service).RemoveDatabase(bank), Is.True);
                AudioPlaybackResult missing = _service.Play(new AudioId("bk1"));
                Assert.That(missing.Success, Is.False);
                Assert.That(missing.Message, Does.Match("not found|No AudioDatabase registered"));
            }
            finally
            {
                Object.Destroy(bank);
                Object.Destroy(bankSound);
            }
        }

        [UnityTest]
        public IEnumerator SharedVolumeKey_BuildsOnePoolForDuplicateBusAssets()
        {
            AudioBusAsset canon = EchoTestUtil.CreateBus("CanonSfx", new OneShotPoolPlayback(2, 4), 1f, "echo.volume.sfx.shared");
            AudioBusAsset bundleCopy = EchoTestUtil.CreateBus("BundleSfx", new OneShotPoolPlayback(2, 4), 1f, "echo.volume.sfx.shared");
            _sfx = canon;
            CreateService(2, 4);
            AudioDefinition def = EchoTestUtil.CreateDefinition("Dup", _clip, "dup1", bundleCopy);

            try
            {
                _service.Play(AudioPlaybackRequest.ForDefinition(def));
                yield return null;

                GameObject[] pools = new GameObject[2];
                pools[0] = GameObject.Find("CanonSfx Pool");
                pools[1] = GameObject.Find("BundleSfx Pool");
                int found = 0;
                if (pools[0] != null) found++;
                if (pools[1] != null) found++;
                Assert.That(found, Is.EqualTo(1));
            }
            finally
            {
                EchoTestUtil.Destroy(def, canon, bundleCopy);
            }
        }

        [UnityTest]
        public IEnumerator MinimalProjectSmoke_NoFrameworkRequired()
        {
            _sfx = EchoTestUtil.CreateBus("Sfx", new OneShotPoolPlayback(2, 4));
            AudioDefinition definition = EchoTestUtil.CreateDefinition("Smoke", _clip, "5m0k3", _sfx);
            AudioDatabase database = EchoTestUtil.CreateDatabase(definition);
            _service = new AudioService();
            AudioServiceSettings smokeSettings = new AudioServiceSettings { RootName = "EchoSmokeRoot" };
            smokeSettings.Databases.Add(database);
            _service.Initialize(smokeSettings);

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
