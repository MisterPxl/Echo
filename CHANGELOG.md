# Changelog

## [2.0.0]

Breaking: audio buses are assets.

- Added `AudioBusAsset` (display name, volume key, default volume, mixer group, playback) and the playback extension point `AudioBusPlayback` / `AudioBusChannel`, with `SingleTrackPlayback` (replaced track) and `OneShotPoolPlayback` (pool per bus).
- Echo ships `Runtime/Buses/MusicBus`, `SfxBus` and `AmbientBus` with the 1.x defaults (0.8, 1, 0.7; SFX pool 8, max 32).
- Removed `AudioType`, `AudioCategory`, `AudioCategoryExtensions`, `AudioVolumeSettings` and `AudioVolumesDefaults`. A project adds a bus without editing Echo.
- `AudioDefinition`: `_type` / `Type` / `EditorSetType` replaced by `_bus` / `Bus` / `EditorSetBus`. A definition with no bus does not play. Rewire 1.x definitions: `_type: 0` → `SfxBus`, `1` → `MusicBus`, `2` → `AmbientBus`.
- `IAudioService`: `MusicVolume`, `SfxVolume`, `AmbientVolume`, `SetVolume(AudioCategory)`, `GetVolume(AudioCategory)`, `IsMusicPlaying`, `IsAmbientPlaying`, `StopMusic`, `StopAmbient` and `StopAllSfx` replaced by `Buses`, `GetVolume(bus)` (bus level before Master), `SetVolume(bus)`, `IsPlaying(bus)` and `Stop(bus)`. `MasterVolume` stays.
- `AudioServiceSettings`: `Volumes`, `SfxPoolInitialSize`, `SfxPoolMaxSize` and the three mixer groups replaced by `MasterVolume`, `Buses` and `RawClipBus`. `PlaySfx(AudioClip)` fails when no `RawClipBus` is set.
- `AudioPlaybackContext.Type` replaced by `Bus`. `AudioRoot` no longer exposes `MusicPlayer`, `AmbientPlayer` and `SfxPool`: each bus builds its own channel under the root.
- `AudioImportSettings`: `ProfileFor(AudioType)` removed; `_musicBus` / `_sfxBus` give auto-created definitions their bus. `AudioDefinitionFactory.ClassifyByDuration` returns a bus.
- `AudioDatabase.EditorValidate` reports definitions without a bus.

## [1.0.0]

- Extracted from the Astra framework (`Core.Audio`) as a standalone UPM package.
- Namespace `Astra.Echo` (Editor: `Astra.Echo.Editor`); every type keeps its `[MovedFrom]` hop to `Core.Audio` and every script keeps its `.meta` GUID.
- Added `IEchoLogger`, `AudioServiceSettings`, `AudioService.Initialize` and `AudioService.Dispose`.
- `AudioId` is now a plain struct. The generated registry is `public static partial class AudioIds`, written to `AudioImportSettings.GeneratedOutputFolder` (default `Assets/Echo/Generated`).
- Menus: `Astra/Echo/...` and `Tools/Astra/Echo/...`.
