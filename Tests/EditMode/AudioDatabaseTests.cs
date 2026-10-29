using NUnit.Framework;
using UnityEngine;

namespace Astra.Echo.Tests
{
    public sealed class AudioDatabaseTests
    {
        private AudioClip _clipA;
        private AudioClip _clipB;
        private AudioDefinition _defA;
        private AudioDefinition _defB;
        private AudioDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _clipA = EchoTestUtil.CreateClip("ClipA");
            _clipB = EchoTestUtil.CreateClip("ClipB");
            _defA = EchoTestUtil.CreateDefinition("DefA", _clipA, "aaaa");
            _defB = EchoTestUtil.CreateDefinition("DefB", _clipB, "bbbb");
            _database = EchoTestUtil.CreateDatabase(_defA, _defB);
        }

        [TearDown]
        public void TearDown()
        {
            EchoTestUtil.Destroy(_database, _defA, _defB, _clipA, _clipB);
        }

        [Test]
        public void TryResolve_ById_FindsTheDefinition()
        {
            Assert.That(_database.TryResolve(new AudioId("bbbb"), out AudioDefinition found), Is.True);
            Assert.That(found, Is.SameAs(_defB));
            Assert.That(_database.Contains(new AudioId("aaaa")), Is.True);
        }

        [Test]
        public void TryResolve_ByUnknownIdOrNone_Fails()
        {
            Assert.That(_database.TryResolve(new AudioId("zzzz"), out AudioDefinition unknown), Is.False);
            Assert.That(unknown, Is.Null);
            Assert.That(_database.TryResolve(AudioId.None, out AudioDefinition none), Is.False);
            Assert.That(none, Is.Null);
        }

        [Test]
        public void TryResolve_ByClip_FindsTheDefinition()
        {
            Assert.That(_database.TryResolve(_clipA, out AudioDefinition found), Is.True);
            Assert.That(found, Is.SameAs(_defA));
            Assert.That(_database.TryResolve((AudioClip)null, out AudioDefinition none), Is.False);
            Assert.That(none, Is.Null);
        }

        [Test]
        public void TryResolveByLabel_UsesTheAssetNameWhenNoLabelIsSet()
        {
            Assert.That(_database.TryResolveByLabel("DefA", out AudioDefinition found), Is.True);
            Assert.That(found, Is.SameAs(_defA));
            Assert.That(_database.TryResolveByLabel("", out AudioDefinition none), Is.False);
            Assert.That(none, Is.Null);
        }

        [Test]
        public void Definitions_KeepTheInsertionOrder()
        {
            Assert.That(_database.Definitions.Count, Is.EqualTo(2));
            Assert.That(_database.Definitions[0], Is.SameAs(_defA));
            Assert.That(_database.Definitions[1], Is.SameAs(_defB));
        }

        [Test]
        public void Duplicates_FirstDefinitionWinsAndValidationReportsThem()
        {
            AudioDefinition duplicate = EchoTestUtil.CreateDefinition("Dup", _clipA, "aaaa");
            AudioDatabase database = EchoTestUtil.CreateDatabase(_defA, duplicate);
            try
            {
                Assert.That(database.TryResolve(new AudioId("aaaa"), out AudioDefinition found), Is.True);
                Assert.That(found, Is.SameAs(_defA));
                Assert.That(database.EditorValidate(), Has.Some.Contains("Duplicate AudioId"));
            }
            finally
            {
                EchoTestUtil.Destroy(database, duplicate);
            }
        }

        [Test]
        public void Validate_ReportsNullSlotsAndMissingIds()
        {
            AudioDefinition destroyed = EchoTestUtil.CreateDefinition("Destroyed", _clipA, "dddd");
            AudioDefinition noId = EchoTestUtil.CreateDefinition("NoId", _clipA, null);
            AudioDatabase database = EchoTestUtil.CreateDatabase(destroyed, noId);
            Object.DestroyImmediate(destroyed);
            try
            {
                System.Collections.Generic.List<string> warnings = database.EditorValidate();
                Assert.That(warnings, Has.Some.Contains("null"));
                Assert.That(warnings, Has.Some.Contains("no valid AudioId"));
            }
            finally
            {
                EchoTestUtil.Destroy(database, noId);
            }
        }

        [Test]
        public void EditorAddDefinition_IsIdempotentAndRefreshesTheIndex()
        {
            AudioDefinition extra = EchoTestUtil.CreateDefinition("Extra", _clipA, "cccc");
            try
            {
                Assert.That(_database.EditorAddDefinition(extra), Is.True);
                Assert.That(_database.EditorAddDefinition(extra), Is.False);
                Assert.That(_database.TryResolve(new AudioId("cccc"), out AudioDefinition found), Is.True);
                Assert.That(found, Is.SameAs(extra));
            }
            finally
            {
                EchoTestUtil.Destroy(extra);
            }
        }

        [Test]
        public void AudioId_EqualityIsByGuid()
        {
            Assert.That(new AudioId("x"), Is.EqualTo(new AudioId("x")));
            Assert.That(new AudioId("x") == new AudioId("y"), Is.False);
            Assert.That(AudioId.None.IsValid, Is.False);
            Assert.That(new AudioId("x").IsValid, Is.True);
        }
    }
}
