using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Astra.Echo.Tests
{
    public sealed class AudioServiceContractTests
    {
        [Test]
        public void Volumes_UnknownBusReportsItsDefaultWithoutBeingRegistered()
        {
            AudioService service = new AudioService();
            AudioBusAsset music = EchoTestUtil.CreateBus("Music", new SingleTrackPlayback(), 0.8f);
            try
            {
                Assert.That(service.GetVolume(music), Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(service.Buses, Is.Empty);
            }
            finally
            {
                EchoTestUtil.Destroy(music);
            }
        }

        [Test]
        public void Volumes_AreClampedAndKeptPerBus()
        {
            AudioService service = new AudioService();
            AudioBusAsset music = EchoTestUtil.CreateBus("Music", new SingleTrackPlayback());
            AudioBusAsset sfx = EchoTestUtil.CreateBus("Sfx", new OneShotPoolPlayback());
            try
            {
                service.MasterVolume = 4f;
                service.SetVolume(music, -1f);
                service.SetVolume(sfx, 0.4f);

                Assert.That(service.MasterVolume, Is.EqualTo(1f));
                Assert.That(service.GetVolume(music), Is.EqualTo(0f));
                Assert.That(service.GetVolume(sfx), Is.EqualTo(0.4f).Within(0.0001f));
                Assert.That(service.Buses, Is.EqualTo(new[] { music, sfx }));
            }
            finally
            {
                EchoTestUtil.Destroy(music, sfx);
            }
        }

        [Test]
        public void Volumes_MasterDoesNotRewriteTheBusLevel()
        {
            AudioService service = new AudioService();
            AudioBusAsset sfx = EchoTestUtil.CreateBus("Sfx", new OneShotPoolPlayback());
            try
            {
                service.SetVolume(sfx, 0.5f);
                service.MasterVolume = 0.5f;

                Assert.That(service.GetVolume(sfx), Is.EqualTo(0.5f).Within(0.0001f));
            }
            finally
            {
                EchoTestUtil.Destroy(sfx);
            }
        }

        [Test]
        public void Volumes_NullBusIsRejected()
        {
            AudioService service = new AudioService();
            Assert.Throws<System.ArgumentNullException>(() => service.SetVolume(null, 1f));
            Assert.Throws<System.ArgumentNullException>(() => service.GetVolume(null));
            Assert.That(service.IsPlaying(null), Is.False);
            Assert.That(service.Stop(null).Success, Is.False);
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
        public void Settings_DefaultsDeclareNoBus()
        {
            AudioServiceSettings settings = new AudioServiceSettings();

            Assert.That(settings.RootName, Is.EqualTo("AudioRoot"));
            Assert.That(settings.Database, Is.Null);
            Assert.That(settings.MasterVolume, Is.EqualTo(1f));
            Assert.That(settings.Buses, Is.Empty);
            Assert.That(settings.RawClipBus, Is.Null);
        }

        [Test]
        public void Logger_NullIsAcceptedEverywhere()
        {
            AudioService service = new AudioService(null, true);
            AudioBusAsset music = EchoTestUtil.CreateBus("Music", new SingleTrackPlayback());
            try
            {
                Assert.That(service.Stop(music).Success, Is.False);
            }
            finally
            {
                EchoTestUtil.Destroy(music);
            }
        }

        private sealed class RecordingService : IAudioService
        {
            public AudioPlaybackRequest Last;

            public float MasterVolume { get; set; }
            public IReadOnlyList<AudioBusAsset> Buses => System.Array.Empty<AudioBusAsset>();

            public float GetVolume(AudioBusAsset bus) => 0f;
            public void SetVolume(AudioBusAsset bus, float volume) { }
            public bool IsPlaying(AudioBusAsset bus) => false;
            public AudioRequest CreateRequest(AudioId id) => new AudioRequest(this, id);
            public AudioPlaybackResult Play(AudioId id) => Play(AudioPlaybackRequest.ForId(id));

            public AudioPlaybackResult Play(in AudioPlaybackRequest request)
            {
                Last = request;
                return AudioPlaybackResult.Succeed("recorded");
            }

            public AudioPlaybackResult Stop(AudioBusAsset bus) => AudioPlaybackResult.Succeed("stopped");
        }
    }
}
