using Astra.Echo.Editor;
using NUnit.Framework;
using UnityEditor;
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
        public void Profiles_StreamMusicAndDecompressSfx()
        {
            Assert.That(_settings.MusicProfile.LoadType, Is.EqualTo(AudioClipLoadType.Streaming));
            Assert.That(_settings.SfxProfile.LoadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
        }

        [Test]
        public void ClassifyByDuration_PicksTheConfiguredBusFromTheThreshold()
        {
            AudioBusAsset music = EchoTestUtil.CreateBus("Music", new SingleTrackPlayback());
            AudioBusAsset sfx = EchoTestUtil.CreateBus("Sfx", new OneShotPoolPlayback());
            AudioClip shortClip = EchoTestUtil.CreateClip("Short", 1f);
            AudioClip longClip = EchoTestUtil.CreateClip("Long", 10f);
            try
            {
                using (SerializedObject serialized = new SerializedObject(_settings))
                {
                    serialized.FindProperty("_musicBus").objectReferenceValue = music;
                    serialized.FindProperty("_sfxBus").objectReferenceValue = sfx;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.That(AudioDefinitionFactory.ClassifyByDuration(shortClip, _settings), Is.SameAs(sfx));
                Assert.That(AudioDefinitionFactory.ClassifyByDuration(longClip, _settings), Is.SameAs(music));
                Assert.That(AudioDefinitionFactory.ClassifyByDuration(null, _settings), Is.SameAs(sfx));
                Assert.That(AudioDefinitionFactory.ClassifyByDuration(longClip, null), Is.Null);
            }
            finally
            {
                EchoTestUtil.Destroy(shortClip, longClip, music, sfx);
            }
        }

        [Test]
        public void ClassifyByDuration_WithoutConfiguredBusesYieldsNoBus()
        {
            AudioClip longClip = EchoTestUtil.CreateClip("Long", 10f);
            try
            {
                Assert.That(AudioDefinitionFactory.ClassifyByDuration(longClip, _settings), Is.Null);
            }
            finally
            {
                EchoTestUtil.Destroy(longClip);
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
