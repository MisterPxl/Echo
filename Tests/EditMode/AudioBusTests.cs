using NUnit.Framework;
using UnityEditor;

namespace Astra.Echo.Tests
{
    public sealed class AudioBusTests
    {
        private const string BusFolder = "Packages/com.misterpxl.echo/Runtime/Buses/";

        [TestCase("MusicBus", "Music", "echo.volume.music", 0.8f, typeof(SingleTrackPlayback))]
        [TestCase("SfxBus", "Sfx", "echo.volume.sfx", 1f, typeof(OneShotPoolPlayback))]
        [TestCase("AmbientBus", "Ambient", "echo.volume.ambient", 0.7f, typeof(SingleTrackPlayback))]
        public void ShippedBus_KeepsTheHistoricalDefaults(string asset, string displayName, string volumeKey, float volume, System.Type playback)
        {
            AudioBusAsset bus = AssetDatabase.LoadAssetAtPath<AudioBusAsset>(BusFolder + asset + ".asset");

            Assert.That(bus, Is.Not.Null, asset);
            Assert.That(bus.DisplayName, Is.EqualTo(displayName));
            Assert.That(bus.VolumeKey, Is.EqualTo(volumeKey));
            Assert.That(bus.DefaultVolume, Is.EqualTo(volume).Within(0.0001f));
            Assert.That(bus.MixerGroup, Is.Null);
            Assert.That(bus.Playback, Is.InstanceOf(playback));
        }

        [Test]
        public void ShippedSfxBus_PoolsLikeTheFormerSfxChannel()
        {
            AudioBusAsset bus = AssetDatabase.LoadAssetAtPath<AudioBusAsset>(BusFolder + "SfxBus.asset");
            OneShotPoolPlayback pool = (OneShotPoolPlayback)bus.Playback;

            Assert.That(pool.InitialSize, Is.EqualTo(8));
            Assert.That(pool.MaxSize, Is.EqualTo(32));
        }

        [Test]
        public void NewBus_DefaultsToPooledOneShotsAndItsAssetName()
        {
            AudioBusAsset bus = UnityEngine.ScriptableObject.CreateInstance<AudioBusAsset>();
            bus.name = "UiBus";
            try
            {
                Assert.That(bus.Playback, Is.InstanceOf<OneShotPoolPlayback>());
                Assert.That(bus.DisplayName, Is.EqualTo("UiBus"));
                Assert.That(bus.VolumeKey, Is.EqualTo("UiBus"));
                Assert.That(bus.DefaultVolume, Is.EqualTo(1f));
            }
            finally
            {
                EchoTestUtil.Destroy(bus);
            }
        }

        [Test]
        public void ProjectBus_HasItsOwnVolumeUnderMaster()
        {
            AudioService service = new AudioService();
            AudioBusAsset sfx = AssetDatabase.LoadAssetAtPath<AudioBusAsset>(BusFolder + "SfxBus.asset");
            AudioBusAsset voice = EchoTestUtil.CreateBus("Voice", new OneShotPoolPlayback(1, 2), 0.6f);
            try
            {
                service.MasterVolume = 0.5f;
                service.SetVolume(sfx, 0.9f);

                Assert.That(service.GetVolume(voice), Is.EqualTo(0.6f).Within(0.0001f));
                service.SetVolume(voice, 0.3f);
                Assert.That(service.GetVolume(voice), Is.EqualTo(0.3f).Within(0.0001f));
                Assert.That(service.GetVolume(sfx), Is.EqualTo(0.9f).Within(0.0001f));
                Assert.That(service.Buses, Is.EqualTo(new[] { sfx, voice }));
            }
            finally
            {
                EchoTestUtil.Destroy(voice);
            }
        }
    }
}
