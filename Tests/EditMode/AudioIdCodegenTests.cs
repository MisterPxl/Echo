using System.Collections.Generic;
using System.IO;
using Astra.Echo.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Astra.Echo.Tests
{
    /// <summary>Covers the AudioIds registry generator: names, duplicates, content, idempotent writes and the settings-relative output.</summary>
    public sealed class AudioIdCodegenTests
    {
        private string _tempFolder;

        [SetUp]
        public void SetUp()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "EchoCodegen_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFolder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempFolder)) Directory.Delete(_tempFolder, true);
        }

        [TestCase("AD_Campfire Fire Loop 4", "ADCampfireFireLoop4")]
        [TestCase("ui/button-click 01", "UiButtonClick01")]
        [TestCase("1stHit", "_1stHit")]
        [TestCase("", "_Unnamed")]
        [TestCase("   ", "_Unnamed")]
        [TestCase("!!!", "_Unnamed")]
        public void Sanitizer_ProducesValidIdentifiers(string raw, string expected)
        {
            Assert.That(AudioMemberNameSanitizer.ToPascalCaseIdentifier(raw), Is.EqualTo(expected));
        }

        [Test]
        public void BuildEntries_DuplicateLabelsGetNumericSuffixesAndTheOutputIsSorted()
        {
            List<AudioIdCodegen.Entry> raw = new List<AudioIdCodegen.Entry>
            {
                new AudioIdCodegen.Entry("zeta", "g1", "Zeta"),
                new AudioIdCodegen.Entry("hit", "g2", "Hit"),
                new AudioIdCodegen.Entry("hit", "g3", "Hit copy"),
                new AudioIdCodegen.Entry("hit", "g4", "Hit copy 2"),
            };

            List<AudioIdCodegen.Entry> entries = AudioIdCodegen.BuildEntries(raw);

            Assert.That(entries.ConvertAll((AudioIdCodegen.Entry e) => e.MemberName),
                Is.EqualTo(new List<string> { "Hit", "Hit_2", "Hit_3", "Zeta" }));
            Assert.That(entries[1].Guid, Is.EqualTo("g3"));
        }

        [Test]
        public void BuildContent_EmitsAPartialStaticRegistryWithTheGuids()
        {
            List<AudioIdCodegen.Entry> entries = new List<AudioIdCodegen.Entry>
            {
                new AudioIdCodegen.Entry("Campfire", "5043f5ce26ef147a58581087e06c901e", "AD_Campfire <loop>"),
            };

            string content = AudioIdCodegen.BuildContent(entries);

            Assert.That(content, Does.Contain("public static partial class AudioIds"));
            Assert.That(content, Does.Contain("using Astra.Echo;"));
            Assert.That(content, Does.Contain("public static readonly AudioId Campfire = new AudioId(\"5043f5ce26ef147a58581087e06c901e\");"));
            Assert.That(content, Does.Contain("AD_Campfire &lt;loop&gt;"));
            Assert.That(content, Does.Not.Contain("partial struct"));
        }

        [Test]
        public void BuildContent_IsDeterministic()
        {
            List<AudioIdCodegen.Entry> entries = new List<AudioIdCodegen.Entry>
            {
                new AudioIdCodegen.Entry("A", "g1", "A"),
                new AudioIdCodegen.Entry("B", "g2", "B"),
            };

            Assert.That(AudioIdCodegen.BuildContent(entries), Is.EqualTo(AudioIdCodegen.BuildContent(entries)));
        }

        [Test]
        public void WriteIfChanged_WritesOnceThenLeavesTheFileUntouched()
        {
            string path = Path.Combine(_tempFolder, AudioIdCodegen.OutputFileName);

            Assert.That(AudioIdCodegen.WriteIfChanged(path, "first"), Is.True);
            System.DateTime stamp = new System.DateTime(2001, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, stamp);

            Assert.That(AudioIdCodegen.WriteIfChanged(path, "first"), Is.False, "same content must not rewrite");
            Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(stamp), "no write when unchanged");

            Assert.That(AudioIdCodegen.WriteIfChanged(path, "second"), Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo("second"));
        }

        [Test]
        public void ResolveOutputFolder_WithoutSettingsUsesTheDefault()
        {
            Assert.That(AudioIdCodegen.ResolveOutputFolder(null), Is.EqualTo("Assets/Echo/Generated"));
            Assert.That(AudioImportSettings.DefaultGeneratedOutputFolder, Is.EqualTo("Assets/Echo/Generated"));
        }

        [Test]
        public void ResolveOutputFolder_IsRelativeToTheSettings()
        {
            AudioImportSettings settings = ScriptableObject.CreateInstance<AudioImportSettings>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_generatedOutputFolder\":\"Assets\\\\Game/Audio/Generated/\"}", settings);
                Assert.That(settings.GeneratedOutputFolder, Is.EqualTo("Assets\\Game/Audio/Generated/"));
                Assert.That(AudioIdCodegen.ResolveOutputFolder(settings), Is.EqualTo("Assets/Game/Audio/Generated"));

                JsonUtility.FromJsonOverwrite("{\"_generatedOutputFolder\":\"\"}", settings);
                Assert.That(AudioIdCodegen.ResolveOutputFolder(settings), Is.EqualTo("Assets/Echo/Generated"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
