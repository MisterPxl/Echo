# Changelog

## [1.0.0]

- Extracted from the Astra framework (`Core.Audio`) as a standalone UPM package.
- Namespace `Astra.Echo` (Editor: `Astra.Echo.Editor`); every type keeps its `[MovedFrom]` hop to `Core.Audio` and every script keeps its `.meta` GUID.
- Added `IEchoLogger`, `AudioServiceSettings`, `AudioService.Initialize` and `AudioService.Dispose`.
- `AudioId` is now a plain struct. The generated registry is `public static partial class AudioIds`, written to `AudioImportSettings.GeneratedOutputFolder` (default `Assets/Echo/Generated`).
- Menus: `Astra/Echo/...` and `Tools/Astra/Echo/...`.
