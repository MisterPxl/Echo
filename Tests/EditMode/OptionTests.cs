using NUnit.Framework;
using UnityEngine;

namespace Astra.Echo.Tests
{
    public sealed class OptionTests
    {
        private GameObject _owner;
        private AudioSource _source;
        private AudioPlaybackContext _context;

        [SetUp]
        public void SetUp()
        {
            _source = EchoTestUtil.CreateSource(out _owner);
            _context = EchoTestUtil.CreateContext();
        }

        [TearDown]
        public void TearDown()
        {
            EchoTestUtil.Destroy(_owner);
        }

        [Test]
        public void Priority_WritesThePriority()
        {
            EchoTestUtil.FromJson<PriorityOption>("{\"_priority\":42}").Apply(_source, _context);
            Assert.That(_source.priority, Is.EqualTo(42));
        }

        [Test]
        public void StereoPan_WritesThePan()
        {
            EchoTestUtil.FromJson<StereoPanOption>("{\"_pan\":-0.5}").Apply(_source, _context);
            Assert.That(_source.panStereo, Is.EqualTo(-0.5f).Within(0.0001f));
        }

        [Test]
        public void SpatialBlend_WritesTheBlend()
        {
            EchoTestUtil.FromJson<SpatialBlendOption>("{\"_blend\":0.75}").Apply(_source, _context);
            Assert.That(_source.spatialBlend, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void Spread3D_WritesTheSpread()
        {
            EchoTestUtil.FromJson<Spread3DOption>("{\"_spread\":90}").Apply(_source, _context);
            Assert.That(_source.spread, Is.EqualTo(90f).Within(0.0001f));
        }

        [Test]
        public void Doppler_WritesTheDopplerLevel()
        {
            EchoTestUtil.FromJson<DopplerOption>("{\"_dopplerLevel\":2}").Apply(_source, _context);
            Assert.That(_source.dopplerLevel, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void BypassOptions_DefaultToBypassing()
        {
            new BypassEffectsOption().Apply(_source, _context);
            new BypassListenerEffectsOption().Apply(_source, _context);
            new BypassReverbZonesOption().Apply(_source, _context);

            Assert.That(_source.bypassEffects, Is.True);
            Assert.That(_source.bypassListenerEffects, Is.True);
            Assert.That(_source.bypassReverbZones, Is.True);
        }

        [Test]
        public void BypassOptions_CanBeDisabled()
        {
            _source.bypassEffects = true;
            _source.bypassListenerEffects = true;
            _source.bypassReverbZones = true;

            EchoTestUtil.FromJson<BypassEffectsOption>("{\"_bypass\":false}").Apply(_source, _context);
            EchoTestUtil.FromJson<BypassListenerEffectsOption>("{\"_bypass\":false}").Apply(_source, _context);
            EchoTestUtil.FromJson<BypassReverbZonesOption>("{\"_bypass\":false}").Apply(_source, _context);

            Assert.That(_source.bypassEffects, Is.False);
            Assert.That(_source.bypassListenerEffects, Is.False);
            Assert.That(_source.bypassReverbZones, Is.False);
        }

        [Test]
        public void RolloffOverride_WritesModeAndDistances()
        {
            RolloffOverrideOption option = EchoTestUtil.FromJson<RolloffOverrideOption>(
                "{\"_mode\":1,\"_minDistance\":3,\"_maxDistance\":40}");

            option.Apply(_source, _context);

            Assert.That(_source.rolloffMode, Is.EqualTo(AudioRolloffMode.Linear));
            Assert.That(_source.minDistance, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(_source.maxDistance, Is.EqualTo(40f).Within(0.0001f));
        }

        [Test]
        public void MixerGroupOverride_WithNullGroupClearsTheOutput()
        {
            new MixerGroupOverrideOption().Apply(_source, _context);
            Assert.That(_source.outputAudioMixerGroup, Is.Null);
        }

        [Test]
        public void SerializedDefaults_MatchTheFrozenValues()
        {
            Assert.That(JsonUtility.ToJson(new PriorityOption()), Does.Contain("\"_priority\":128"));
            Assert.That(JsonUtility.ToJson(new DelayedPlayOption()), Does.Contain("\"_delaySeconds\":0.1"));
            Assert.That(JsonUtility.ToJson(new FadeInOption()), Does.Contain("\"_durationSeconds\":0.5"));
        }
    }
}
