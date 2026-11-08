using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.Scripting.APIUpdating;

namespace Astra.Echo.Tests
{
    /// <summary>The single MovedFrom hop to the historical Core.Audio identity must survive the extraction.</summary>
    public sealed class EchoMovedFromTests
    {
        [TestCase(typeof(AudioService))]
        [TestCase(typeof(AudioId))]
        [TestCase(typeof(AudioDefinition))]
        [TestCase(typeof(StereoPanOption))]
        [TestCase(typeof(CooldownModulator))]
        public void Type_KeepsItsHistoricalIdentity(Type type)
        {
            MovedFromAttribute attribute = type.GetCustomAttribute<MovedFromAttribute>(false);

            Assert.That(attribute, Is.Not.Null, type.Name);
            Assert.That(type.Namespace, Is.EqualTo("Astra.Echo"));
            Assert.That(type.Assembly.GetName().Name, Is.EqualTo("Astra.Echo.Runtime"));
        }
    }
}
