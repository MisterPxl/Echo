using System.Collections.Generic;
using Astra.Echo.Editor;
using NUnit.Framework;

namespace Astra.Echo.Tests
{
    public sealed class AudioDatabaseAssignmentTests
    {
        [Test]
        public void NestedDatabase_OwnsDefinitionsUnderItsFolder()
        {
            List<AudioDatabaseAssignment.Scope> scopes = new List<AudioDatabaseAssignment.Scope>
            {
                new AudioDatabaseAssignment.Scope("Assets/Audio/AudioDatabase.asset", "Assets/Audio"),
                new AudioDatabaseAssignment.Scope("Assets/Audio/Level1/AudioDatabase.asset", "Assets/Audio/Level1"),
            };

            Dictionary<string, List<string>> membership = AudioDatabaseAssignment.BuildMembership(
                new[] { "Assets/Audio/Level1/Sfx/Hit.asset", "Assets/Audio/UI/Click.asset" },
                scopes,
                null);

            Assert.That(membership["Assets/Audio/Level1/AudioDatabase.asset"], Does.Contain("Assets/Audio/Level1/Sfx/Hit.asset"));
            Assert.That(membership["Assets/Audio/AudioDatabase.asset"], Does.Contain("Assets/Audio/UI/Click.asset"));
        }

        [Test]
        public void OrphanDefinition_UsesConfiguredDefaultDatabase()
        {
            List<AudioDatabaseAssignment.Scope> scopes = new List<AudioDatabaseAssignment.Scope>
            {
                new AudioDatabaseAssignment.Scope("Assets/Audio/AudioDatabase.asset", "Assets/Audio"),
            };

            Dictionary<string, List<string>> membership = AudioDatabaseAssignment.BuildMembership(
                new[] { "Assets/Other/Loose.asset" },
                scopes,
                "Assets/Default/AudioDatabase.asset");

            Assert.That(membership["Assets/Default/AudioDatabase.asset"], Does.Contain("Assets/Other/Loose.asset"));
        }

        [Test]
        public void ResolveDefault_PrefersConfiguredPath_ThenSoleDatabase_ThenShortest()
        {
            List<AudioDatabaseAssignment.Scope> scopes = new List<AudioDatabaseAssignment.Scope>
            {
                new AudioDatabaseAssignment.Scope("Assets/Z/AudioDatabase.asset", "Assets/Z"),
                new AudioDatabaseAssignment.Scope("Assets/Audio/AudioDatabase.asset", "Assets/Audio"),
            };

            Assert.That(
                AudioDatabaseAssignment.ResolveDefaultDatabaseAssetPath(scopes, "Assets/Explicit/AudioDatabase.asset"),
                Is.EqualTo("Assets/Explicit/AudioDatabase.asset"));

            List<AudioDatabaseAssignment.Scope> single = new List<AudioDatabaseAssignment.Scope>
            {
                new AudioDatabaseAssignment.Scope("Assets/Only/AudioDatabase.asset", "Assets/Only"),
            };
            Assert.That(AudioDatabaseAssignment.ResolveDefaultDatabaseAssetPath(single, null), Is.EqualTo("Assets/Only/AudioDatabase.asset"));

            Assert.That(
                AudioDatabaseAssignment.ResolveDefaultDatabaseAssetPath(scopes, null),
                Is.EqualTo("Assets/Z/AudioDatabase.asset"));
        }
    }
}