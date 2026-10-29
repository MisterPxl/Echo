using NUnit.Framework;
using UnityEngine;

namespace Astra.Echo.Tests
{
    public sealed class AudioServiceContractTests
    {
        [Test]
        public void VolumeBuses_EffectiveVolumeIsMasterTimesCategory()
        {
            AudioVolumeSettings volumes = new AudioVolumeSettings
            {
                Master = 0.5f,
                Music = 0.8f,
                Sfx = 0.4f,
                Ambient = 0.2f,
            };

            Assert.That(volumes.EffectiveMusic, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(volumes.EffectiveSfx, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(volumes.EffectiveAmbient, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(volumes.EffectiveFor(AudioCategory.Master), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(volumes.EffectiveFor(AudioCategory.Sfx), Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void VolumeBuses_ValuesAreClamped()
        {
            AudioVolumeSettings volumes = new AudioVolumeSettings { Master = 4f, Music = -1f };
            Assert.That(volumes.Master, Is.EqualTo(1f));
            Assert.That(volumes.Music, Is.EqualTo(0f));
        }

        [Test]
        public void Service_SetVolumeAndGetVolumeRoundTripPerCategory()
        {
            AudioService service = new AudioService();
            service.SetVolume(AudioCategory.Master, 0.5f);
            service.SetVolume(AudioCategory.Sfx, 0.5f);

            Assert.That(service.MasterVolume, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(service.GetVolume(AudioCategory.Sfx), Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void Service_ApplyDefaultsFeedsTheBuses()
        {
            AudioService service = new AudioService();
            service.ApplyDefaults(new AudioVolumesDefaults(0.9f, 0.8f, 0.7f, 0.6f));

            Assert.That(service.MasterVolume, Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(service.MusicVolume, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(service.SfxVolume, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(service.AmbientVolume, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void Service_PlayWithoutRootFailsWithAClearMessage()
        {
            AudioService service = new AudioService();
            AudioPlaybackResult result = service.Play(new AudioId("abc"));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("not initialized"));
        }

        [Test]
        public void Service_InitializeRejectsNullSettings()
        {
            AudioService service = new AudioService();
            Assert.Throws<System.ArgumentNullException>(() => service.Initialize(null));
        }

        [Test]
        public void RequestBuilder_WithModsAndOptionsIgnoreNulls()
        {
            RecordingService recorder = new RecordingService();
            CooldownModulator modulator = new CooldownModulator();
            PriorityOption option = new PriorityOption();

            recorder.CreateRequest(new AudioId("abc"))
                .WithMods(modulator)
                .WithMods((ISoundModulator)null)
                .WithMods(new ISoundModulator[] { null, modulator })
                .With(option)
                .With((ISoundOption)null)
                .With(new ISoundOption[] { option, null })
                .Play();

            Assert.That(recorder.Last.Modulators.Count, Is.EqualTo(2));
            Assert.That(recorder.Last.Options.Count, Is.EqualTo(2));
        }

        [Test]
        public void RequestBuilder_OverridesAreForwarded()
        {
            RecordingService recorder = new RecordingService();
            Vector3 position = new Vector3(1f, 2f, 3f);

            recorder.CreateRequest(new AudioId("abc"))
                .WithVolume(0.3f)
                .WithPitch(1.5f)
                .WithLoop(true)
                .At(position)
                .Play();

            Assert.That(recorder.Last.Id, Is.EqualTo(new AudioId("abc")));
            Assert.That(recorder.Last.VolumeScale, Is.EqualTo(0.3f));
            Assert.That(recorder.Last.PitchOverride, Is.EqualTo(1.5f));
            Assert.That(recorder.Last.LoopOverride, Is.EqualTo(true));
            Assert.That(recorder.Last.WorldPosition, Is.EqualTo(position));
        }

        [Test]
        public void PlaybackRequest_ForDefinitionCarriesTheDefinitionId()
        {
            AudioDefinition definition = EchoTestUtil.CreateDefinition("Def", null, "abcd");
            try
            {
                AudioPlaybackRequest request = AudioPlaybackRequest.ForDefinition(definition);
                Assert.That(request.Definition, Is.SameAs(definition));
                Assert.That(request.Id, Is.EqualTo(new AudioId("abcd")));
            }
            finally
            {
                EchoTestUtil.Destroy(definition);
            }
        }

        [Test]
        public void PlaybackResult_Factories()
        {
            System.InvalidOperationException exception = new System.InvalidOperationException("boom");
            AudioPlaybackResult ok = AudioPlaybackResult.Succeed("done");
            AudioPlaybackResult ko = AudioPlaybackResult.Fail("nope", exception);

            Assert.That(ok.Success, Is.True);
            Assert.That(ok.Message, Is.EqualTo("done"));
            Assert.That(ok.Exception, Is.Null);
            Assert.That(ko.Success, Is.False);
            Assert.That(ko.Exception, Is.SameAs(exception));
            Assert.That(ko.Handle, Is.Null);
        }

        [Test]
        public void PlaySfxExtension_FailsForNullService()
        {
            IAudioService service = null;
            AudioPlaybackResult result = service.PlaySfx(null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("IAudioService not available."));
        }

        [Test]
        public void PlaySfxExtension_FailureMessageNamesTheCapability()
        {
            AudioPlaybackResult result = new RecordingService().PlaySfx(null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("IRawClipAudioService"));
        }

        [Test]
        public void PlaySfxExtension_UsesTheRawClipCapabilityWhenPresent()
        {
            AudioService service = new AudioService();
            AudioPlaybackResult result = service.PlaySfx(null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("PlaySfx: clip is null."));
        }

        [Test]
        public void Settings_DefaultsMatchTheHistoricalConfiguration()
        {
            AudioServiceSettings settings = new AudioServiceSettings();

            Assert.That(settings.RootName, Is.EqualTo("AudioRoot"));
            Assert.That(settings.Database, Is.Null);
            Assert.That(settings.SfxPoolInitialSize, Is.EqualTo(8));
            Assert.That(settings.SfxPoolMaxSize, Is.EqualTo(32));
            Assert.That(settings.Volumes.Master, Is.EqualTo(1f));
            Assert.That(settings.Volumes.Music, Is.EqualTo(0.8f));
            Assert.That(settings.Volumes.Sfx, Is.EqualTo(1f));
            Assert.That(settings.Volumes.Ambient, Is.EqualTo(0.7f));
            Assert.That(settings.MusicMixerGroup, Is.Null);
        }

        [Test]
        public void Logger_NullIsAcceptedEverywhere()
        {
            AudioService service = new AudioService(null, true);
            Assert.DoesNotThrow(() => service.StopAllSfx());
            Assert.That(service.StopMusic().Success, Is.False);
            Assert.That(service.StopAmbient().Success, Is.False);
        }

        private sealed class RecordingService : IAudioService
        {
            public AudioPlaybackRequest Last;

            public bool IsMusicPlaying => false;
            public bool IsAmbientPlaying => false;
            public float MasterVolume { get; set; }
            public float MusicVolume { get; set; }
            public float SfxVolume { get; set; }
            public float AmbientVolume { get; set; }

            public void SetVolume(AudioCategory category, float volume) { }
            public float GetVolume(AudioCategory category) => 0f;
            public AudioRequest CreateRequest(AudioId id) => new AudioRequest(this, id);
            public AudioPlaybackResult Play(AudioId id) => Play(AudioPlaybackRequest.ForId(id));

            public AudioPlaybackResult Play(in AudioPlaybackRequest request)
            {
                Last = request;
                return AudioPlaybackResult.Succeed("recorded");
            }

            public AudioPlaybackResult StopMusic() => AudioPlaybackResult.Succeed("stopped");
            public AudioPlaybackResult StopAmbient() => AudioPlaybackResult.Succeed("stopped");
            public void StopAllSfx() { }
        }
    }
}
