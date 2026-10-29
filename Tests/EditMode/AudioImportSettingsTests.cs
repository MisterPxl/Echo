using Astra.Echo.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Astra.Echo.Tests
{
    public sealed class AudioImportSettingsTests
    {
        private AudioImportSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<AudioImportSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            EchoTestUtil.Destroy(_settings);
        }

        [Test]
        public void ProfileFor_ChoosesMusicOnlyForMusicType()
        {
            Assert.That(_settings.ProfileFor(AudioType.Music).LoadType, Is.EqualTo(AudioClipLoadType.Streaming));
            Assert.That(_settings.ProfileFor(AudioType.Sfx).LoadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
            Assert.That(_settings.ProfileFor(AudioType.Ambient).LoadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
        }

        [Test]
        public void ClassifyByDuration_UsesTheThreshold()
        {
            AudioClip shortClip = EchoTestUtil.CreateClip("Short", 1f);
            AudioClip longClip = EchoTestUtil.CreateClip("Long", 10f);
            try
            {
                Assert.That(AudioDefinitionFactory.ClassifyByDuration(shortClip, _settings), Is.EqualTo(AudioType.Sfx));
                Assert.That(AudioDefinitionFactory.ClassifyByDuration(longClip, _settings), Is.EqualTo(AudioType.Music));
                Assert.That(AudioDefinitionFactory.ClassifyByDuration(null, _settings), Is.EqualTo(AudioType.Sfx));
            }
            finally
            {
                EchoTestUtil.Destroy(shortClip, longClip);
            }
        }

        [Test]
        public void AutoCreateScope_EmptyMeansWholeProject()
        {
            Assert.That(_settings.IsPathInAutoCreateScope("Assets/Anything/clip.wav"), Is.True);
            Assert.That(_settings.IsPathInAutoCreateScope(""), Is.False);
        }

        [Test]
        public void AutoCreateScope_WhitelistLimitsTheFolders()
        {
            JsonUtility.FromJsonOverwrite("{\"_autoCreateScopes\":[\"_Project/Audio\"]}", _settings);

            Assert.That(_settings.IsPathInAutoCreateScope("Assets/_Project/Audio/hit.wav"), Is.True);
            Assert.That(_settings.IsPathInAutoCreateScope("Assets/_Project/AudioOther/hit.wav"), Is.False);
            Assert.That(_settings.IsPathInAutoCreateScope("Assets/Elsewhere/hit.wav"), Is.False);
        }

        [Test]
        public void GeneratedOutputFolder_DefaultsToAssetsEchoGenerated()
        {
            Assert.That(_settings.GeneratedOutputFolder, Is.EqualTo("Assets/Echo/Generated"));
        }
    }
}
