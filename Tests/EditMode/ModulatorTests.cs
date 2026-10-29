using NUnit.Framework;
using UnityEngine;

namespace Astra.Echo.Tests
{
    public sealed class ModulatorTests
    {
        private GameObject _owner;
        private AudioSource _source;
        private AudioClip _clipA;
        private AudioClip _clipB;
        private AudioClip _clipC;

        [SetUp]
        public void SetUp()
        {
            _source = EchoTestUtil.CreateSource(out _owner);
            _clipA = EchoTestUtil.CreateClip("A");
            _clipB = EchoTestUtil.CreateClip("B");
            _clipC = EchoTestUtil.CreateClip("C");
        }

        [TearDown]
        public void TearDown()
        {
            EchoTestUtil.Destroy(_owner, _clipA, _clipB, _clipC);
        }

        [Test]
        public void RandomPitch_StaysWithinRangeAroundBasePitch()
        {
            RandomPitchModulator modulator = EchoTestUtil.FromJson<RandomPitchModulator>("{\"_min\":0.8,\"_max\":1.2}");
            AudioPlaybackContext context = EchoTestUtil.CreateContext(basePitch: 2f);

            Random.InitState(1234);
            for (int i = 0; i < 50; i++)
            {
                modulator.Modulate(_source, context);
                Assert.That(_source.pitch, Is.InRange(2f * 0.8f - 0.0001f, 2f * 1.2f + 0.0001f));
            }
        }

        [Test]
        public void RandomPitch_SwappedBoundsAreNormalised()
        {
            RandomPitchModulator modulator = EchoTestUtil.FromJson<RandomPitchModulator>("{\"_min\":1.5,\"_max\":0.5}");
            AudioPlaybackContext context = EchoTestUtil.CreateContext();

            Random.InitState(7);
            for (int i = 0; i < 50; i++)
            {
                modulator.Modulate(_source, context);
                Assert.That(_source.pitch, Is.InRange(0.5f - 0.0001f, 1.5f + 0.0001f));
            }
        }

        [Test]
        public void RandomVolume_ScalesCurrentVolumeWithinRange()
        {
            RandomVolumeModulator modulator = EchoTestUtil.FromJson<RandomVolumeModulator>("{\"_min\":0.5,\"_max\":0.6}");
            AudioPlaybackContext context = EchoTestUtil.CreateContext();

            Random.InitState(99);
            for (int i = 0; i < 20; i++)
            {
                _source.volume = 1f;
                modulator.Modulate(_source, context);
                Assert.That(_source.volume, Is.InRange(0.5f - 0.0001f, 0.6f + 0.0001f));
            }
        }

        [Test]
        public void Cooldown_SecondImmediateTriggerIsVetoed()
        {
            CooldownModulator modulator = EchoTestUtil.FromJson<CooldownModulator>("{\"_cooldownSeconds\":5}");

            AudioPlaybackContext first = EchoTestUtil.CreateContext();
            modulator.Modulate(_source, first);
            Assert.That(first.IsVetoed, Is.False);

            AudioPlaybackContext second = EchoTestUtil.CreateContext();
            modulator.Modulate(_source, second);
            Assert.That(second.IsVetoed, Is.True);
            Assert.That(second.VetoReason, Does.Contain("cooldown"));
        }

        [Test]
        public void RoundRobin_CyclesClipsInOrder()
        {
            RoundRobinClipModulator modulator = new RoundRobinClipModulator();
            SetClips(modulator, "_clips", _clipA, _clipB, _clipC);

            AudioClip[] expected = { _clipA, _clipB, _clipC, _clipA };
            for (int i = 0; i < expected.Length; i++)
            {
                AudioPlaybackContext context = EchoTestUtil.CreateContext();
                modulator.Modulate(_source, context);
                Assert.That(context.ClipOverride, Is.SameAs(expected[i]), "call " + i);
            }
        }

        [Test]
        public void RoundRobin_EmptyListLeavesClipUntouched()
        {
            RoundRobinClipModulator modulator = new RoundRobinClipModulator();
            AudioPlaybackContext context = EchoTestUtil.CreateContext();

            modulator.Modulate(_source, context);

            Assert.That(context.ClipOverride, Is.Null);
        }

        [Test]
        public void RandomClip_AvoidsImmediateRepeatWithTwoClips()
        {
            RandomClipModulator modulator = new RandomClipModulator();
            SetClips(modulator, "_clips", _clipA, _clipB);

            Random.InitState(5);
            AudioClip previous = null;
            for (int i = 0; i < 30; i++)
            {
                AudioPlaybackContext context = EchoTestUtil.CreateContext();
                modulator.Modulate(_source, context);
                Assert.That(context.ClipOverride, Is.Not.Null);
                if (previous != null) Assert.That(context.ClipOverride, Is.Not.SameAs(previous), "call " + i);
                previous = context.ClipOverride;
            }
        }

        [Test]
        public void WeightedRandom_ZeroWeightClipIsNeverPicked()
        {
            WeightedRandomClipModulator modulator = new WeightedRandomClipModulator();
            WeightedRandomClipModulator.WeightedClip heavy = new WeightedRandomClipModulator.WeightedClip { Clip = _clipA, Weight = 1f };
            WeightedRandomClipModulator.WeightedClip zero = new WeightedRandomClipModulator.WeightedClip { Clip = _clipB, Weight = 0f };
            SetEntries(modulator, heavy, zero);

            Random.InitState(42);
            for (int i = 0; i < 40; i++)
            {
                AudioPlaybackContext context = EchoTestUtil.CreateContext();
                modulator.Modulate(_source, context);
                Assert.That(context.ClipOverride, Is.SameAs(_clipA));
            }
        }

        [Test]
        public void WeightedRandom_SeededRollIsDeterministic()
        {
            WeightedRandomClipModulator modulator = new WeightedRandomClipModulator();
            SetEntries(modulator,
                new WeightedRandomClipModulator.WeightedClip { Clip = _clipA, Weight = 1f },
                new WeightedRandomClipModulator.WeightedClip { Clip = _clipB, Weight = 1f });

            string[] first = new string[10];
            string[] second = new string[10];

            Random.InitState(2024);
            for (int i = 0; i < first.Length; i++) first[i] = Pick(modulator);

            Random.InitState(2024);
            for (int i = 0; i < second.Length; i++) second[i] = Pick(modulator);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void MaxConcurrent_VetoesWhenLimitReached()
        {
            MaxConcurrentModulator modulator = EchoTestUtil.FromJson<MaxConcurrentModulator>("{\"_maxConcurrent\":2}");
            _source.clip = _clipA; // one second long, so the entries stay alive during the test.

            AudioPlaybackContext first = EchoTestUtil.CreateContext();
            AudioPlaybackContext second = EchoTestUtil.CreateContext();
            AudioPlaybackContext third = EchoTestUtil.CreateContext();

            modulator.Modulate(_source, first);
            modulator.Modulate(_source, second);
            modulator.Modulate(_source, third);

            Assert.That(first.IsVetoed, Is.False);
            Assert.That(second.IsVetoed, Is.False);
            Assert.That(third.IsVetoed, Is.True);
            Assert.That(third.VetoReason, Does.Contain("max concurrent"));
        }

        [Test]
        public void IncrementalPitch_RaisesPitchOnConsecutiveHitsAndRestartsAtMaxSteps()
        {
            IncrementalPitchModulator modulator = EchoTestUtil.FromJson<IncrementalPitchModulator>(
                "{\"_step\":0.1,\"_maxSteps\":3,\"_resetCooldownSeconds\":100}");
            AudioPlaybackContext context = EchoTestUtil.CreateContext(basePitch: 1f);

            float[] expected = { 1f, 1.1f, 1.2f, 1f };
            for (int i = 0; i < expected.Length; i++)
            {
                modulator.Modulate(_source, context);
                Assert.That(_source.pitch, Is.EqualTo(expected[i]).Within(0.0001f), "hit " + i);
            }
        }

        [Test]
        public void IncrementalPitch_ResetsAfterTheCooldownWindow()
        {
            IncrementalPitchModulator modulator = EchoTestUtil.FromJson<IncrementalPitchModulator>(
                "{\"_step\":0.1,\"_maxSteps\":8,\"_resetCooldownSeconds\":0}");
            AudioPlaybackContext context = EchoTestUtil.CreateContext(basePitch: 1f);

            // With a zero window and a frozen editor clock the delta is never strictly greater: no reset.
            modulator.Modulate(_source, context);
            modulator.Modulate(_source, context);
            Assert.That(_source.pitch, Is.EqualTo(1.1f).Within(0.0001f));
        }

        [Test]
        public void DampingVolume_AttenuatesRepeatedHitsInsideTheWindow()
        {
            DampingVolumeModulator modulator = EchoTestUtil.FromJson<DampingVolumeModulator>(
                "{\"_windowSeconds\":100,\"_dampPerHit\":0.5,\"_maxStacks\":4}");
            AudioPlaybackContext context = EchoTestUtil.CreateContext();

            float[] expected = { 1f, 0.5f, 0.25f };
            for (int i = 0; i < expected.Length; i++)
            {
                _source.volume = 1f;
                modulator.Modulate(_source, context);
                Assert.That(_source.volume, Is.EqualTo(expected[i]).Within(0.0001f), "hit " + i);
            }
        }

        [Test]
        public void ContextVeto_KeepsTheFirstReasonAndDefaultsWhenNull()
        {
            AudioPlaybackContext context = EchoTestUtil.CreateContext();
            context.Veto("first");
            context.Veto("second");
            Assert.That(context.VetoReason, Is.EqualTo("first"));

            AudioPlaybackContext nullReason = EchoTestUtil.CreateContext();
            nullReason.Veto(null);
            Assert.That(nullReason.IsVetoed, Is.True);
            Assert.That(nullReason.VetoReason, Is.EqualTo("vetoed"));
        }

        private string Pick(WeightedRandomClipModulator modulator)
        {
            AudioPlaybackContext context = EchoTestUtil.CreateContext();
            modulator.Modulate(_source, context);
            return context.ClipOverride.name;
        }

        private static void SetClips(object modulator, string fieldName, params AudioClip[] clips)
        {
            System.Reflection.FieldInfo field = modulator.GetType().GetField(
                fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(modulator, new System.Collections.Generic.List<AudioClip>(clips));
        }

        private static void SetEntries(WeightedRandomClipModulator modulator, params WeightedRandomClipModulator.WeightedClip[] entries)
        {
            System.Reflection.FieldInfo field = typeof(WeightedRandomClipModulator).GetField(
                "_entries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(modulator, new System.Collections.Generic.List<WeightedRandomClipModulator.WeightedClip>(entries));
        }
    }
}
