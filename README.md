# Astra Echo — Data-driven Audio

`com.misterpxl.echo` — Unity 6000.0+. Depends only on `com.unity.modules.audio`.

## Concepts

- `AudioDefinition` (ScriptableObject): clip, bus, base volume and pitch, spatial settings, `[SerializeReference]` modulators and options.
- `AudioBusAsset` (ScriptableObject): a volume bus under Master — display name, volume key (for a settings layer that persists volumes), default volume, mixer group and playback behaviour. A source plays at definition volume × bus volume × Master.
- `AudioDatabase`: lookup by `AudioId`, clip or label.
- `AudioId`: stable id (the definition asset GUID). The editor generates `public static partial class AudioIds` in `AudioImportSettings.GeneratedOutputFolder`.
- `IAudioService` / `AudioService`: `MasterVolume`, `GetVolume` / `SetVolume` / `IsPlaying` / `Stop` per bus, `Buses`, fluent `CreateRequest(id)`.
- Extension points, in any assembly, with no enum and no registry:
  - `ISoundModulator` / `ISoundOption` (derive from `SoundModulatorBase` / `SoundOptionBase`);
  - `AudioBusPlayback` / `AudioBusChannel` for a new playback behaviour (crossfade, voice queue…).

## Buses

Echo ships three buses in `Runtime/Buses`:

| Asset | GUID | Playback | Default volume | Volume key |
| --- | --- | --- | --- | --- |
| `MusicBus` | `6b78cb2ecfdf4fe29247f30be6425392` | `SingleTrackPlayback` | 0.8 | `echo.volume.music` |
| `SfxBus` | `0a3f4f454fb94b1380a1378fb9628892` | `OneShotPoolPlayback` (8, max 32) | 1 | `echo.volume.sfx` |
| `AmbientBus` | `50c456a67b6a497da46925f93cb7403f` | `SingleTrackPlayback` | 0.7 | `echo.volume.ambient` |

- `SingleTrackPlayback`: one track; the next Play on the bus replaces it. The running track follows volume changes. The result handle is the track `AudioPlayer`.
- `OneShotPoolPlayback`: a pool per bus; one-shots overlap up to the cap, spatial settings apply.

To add a bus (Voice, UI…), create `Astra/Echo/Audio Bus`, pick its playback and reference it from the definitions. Nothing is registered: the service creates a bus the first time a definition plays on it (or `SetVolume` names it). List buses in `AudioServiceSettings.Buses` to build their players and pools at initialization. The package buses are read-only: to change a mixer group, a default volume or a pool size, create your own bus assets.

## Quick start

```csharp
using Astra.Echo;

AudioServiceSettings settings = new AudioServiceSettings { Database = myDatabase, RawClipBus = mySfxBus };
settings.Buses.Add(myMusicBus);
settings.Buses.Add(mySfxBus);

AudioService audio = new AudioService(logger: null, verbose: false);
audio.Initialize(settings);
audio.Play(AudioIds.Campfire);
audio.SetVolume(myMusicBus, 0.5f);
audio.Stop(myMusicBus);
// ...
audio.Dispose(); // destroys the AudioRoot created by Initialize
```

Pass a parent `Transform` to `Initialize` to scope the root; with no parent it persists across scenes. `PlaySfx(AudioClip)` (the `IRawClipAudioService` capability) plays clips that no definition describes on `RawClipBus`.

## Import pipeline

`AudioImportSettings` applies the music or SFX import profile and gives each auto-created definition its music bus (clip at least `MusicDurationThresholdSeconds` long) or its SFX bus. Assign both buses (usually `MusicBus` and `SfxBus`); without them definitions are created with no bus and `AudioDatabase.EditorValidate` reports them.

## Logging

Implement `IEchoLogger` to receive diagnostics. `null` is accepted everywhere.

## Menus

`Astra/Echo/...` (create assets) and `Tools/Astra/Echo/Regenerate Audio ID Registry`.

## Tests

`Tests/EditMode` (`Astra.Echo.Tests.EditMode`) and `Tests/PlayMode` (`Astra.Echo.Tests.PlayMode`).
