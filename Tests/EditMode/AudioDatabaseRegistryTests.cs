using NUnit.Framework;

namespace Astra.Echo.Tests
{
    public sealed class AudioDatabaseRegistryTests
    {
        private AudioClip _clip;
        private AudioService _service;

        [SetUp]
        public void SetUp()
        {
            _clip = AudioClip.Create("RegistryClip", 4410, 1, 44100, false);
            _service = new AudioService();
            _service.Initialize(new AudioServiceSettings { RootName = "RegistryRoot" });
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            if (_clip != null)
                UnityEngine.Object.DestroyImmediate(_clip);
        }

        [Test]
        public void AddDatabase_RegistersAndResolvesInOrder()
        {
            AudioDefinition first = EchoTestUtil.CreateDefinition("A", _clip, "a1");
            AudioDefinition second = EchoTestUtil.CreateDefinition("B", _clip, "b1");
            AudioDatabase primary = EchoTestUtil.CreateDatabase(first);
            AudioDatabase overlay = EchoTestUtil.CreateDatabase(second);

            try
            {
                Assert.That(_service.AddDatabase(primary), Is.True);
                Assert.That(_service.AddDatabase(overlay), Is.True);

                AudioPlaybackResult firstHit = _service.Play(new AudioId("a1"));
                AudioPlaybackResult secondHit = _service.Play(new AudioId("b1"));
                Assert.That(firstHit.Success, Is.True, firstHit.Message);
                Assert.That(secondHit.Success, Is.True, secondHit.Message);
            }
            finally
            {
                EchoTestUtil.Destroy(primary, overlay, first, second);
            }
        }

        [Test]
        public void AddDatabase_RejectsNullAndDuplicates()
        {
            AudioDatabase database = EchoTestUtil.CreateDatabase();
            try
            {
                Assert.That(_service.AddDatabase(null), Is.False);
                Assert.That(_service.AddDatabase(database), Is.True);
                Assert.That(_service.AddDatabase(database), Is.False);
                Assert.That(_service.Databases.Count, Is.EqualTo(1));
            }
            finally
            {
                EchoTestUtil.Destroy(database);
            }
        }

        [Test]
        public void Initialize_ReplacesTheDatabaseList()
        {
            AudioDatabase first = EchoTestUtil.CreateDatabase();
            AudioDatabase second = EchoTestUtil.CreateDatabase();
            AudioServiceSettings firstSettings = new AudioServiceSettings { RootName = "One" };
            firstSettings.Databases.Add(first);

            AudioServiceSettings secondSettings = new AudioServiceSettings { RootName = "Two" };
            secondSettings.Databases.Add(second);

            try
            {
                _service.Initialize(firstSettings);
                Assert.That(_service.Databases, Is.EqualTo(new[] { first }));

                _service.Initialize(secondSettings);
                Assert.That(_service.Databases, Is.EqualTo(new[] { second }));
            }
            finally
            {
                EchoTestUtil.Destroy(first, second);
            }
        }

        [Test]
        public void SharedVolumeKey_SharesBusVolume()
        {
            AudioBusAsset canonical = EchoTestUtil.CreateBus("Canon", new OneShotPoolPlayback(), 0.5f, "echo.test.shared");
            AudioBusAsset duplicate = EchoTestUtil.CreateBus("Dupe", new OneShotPoolPlayback(), 0.2f, "echo.test.shared");

            try
            {
                _service.SetVolume(canonical, 0.25f);
                Assert.That(_service.GetVolume(duplicate), Is.EqualTo(0.25f).Within(0.0001f));
                Assert.That(_service.Buses.Count, Is.EqualTo(1));
            }
            finally
            {
                EchoTestUtil.Destroy(canonical, duplicate);
            }
        }
    }
}