# Astra Echo — Data-driven Audio

`com.misterpxl.echo` — Unity 6000.0+. Depends only on `com.unity.modules.audio`.

## Concepts

- `AudioDefinition` (ScriptableObject): clip, type (Sfx / Music / Ambient), base volume and pitch, spatial settings, `[SerializeReference]` modulators and options.
- `AudioDatabase`: lookup by `AudioId`, clip or label.
- `AudioId`: stable id (the definition asset GUID). The editor generates `public static partial class AudioIds` in `AudioImportSettings.GeneratedOutputFolder`.
- `IAudioService` / `AudioService`: volume buses, pooled SFX, single music and ambient channels, fluent `CreateRequest(id)`.
- Extension points: implement `ISoundModulator` / `ISoundOption` (derive from `SoundModulatorBase` / `SoundOptionBase`) in any assembly. No enum, no registry.

## Quick start

```csharp
using Astra.Echo;

AudioService audio = new AudioService(logger: null, verbose: false);
audio.Initialize(new AudioServiceSettings { Database = myDatabase });
audio.Play(AudioIds.Campfire);
// ...
audio.Dispose(); // destroys the AudioRoot created by Initialize
```

Pass a parent `Transform` to `Initialize` to scope the root; with no parent it persists across scenes.

## Logging

Implement `IEchoLogger` to receive diagnostics. `null` is accepted everywhere.

## Menus

`Astra/Echo/...` (create assets) and `Tools/Astra/Echo/Regenerate Audio ID Registry`.

## Tests

`Tests/EditMode` (`Astra.Echo.Tests.EditMode`) and `Tests/PlayMode` (`Astra.Echo.Tests.PlayMode`).
