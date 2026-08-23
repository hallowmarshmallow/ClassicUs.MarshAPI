# ManuAPI v1.6.0

## New Core APIs

- **PlayerUtils** — centralized player lookup (`FindById`, `FindByOwnerId`), name resolution (`NameSafe`), team filtering (`AllAliveOnTeam`), distance/range queries (`ClosestAlive`), teleport (`Teleport` using SnapTo).
- **RpcExtensions** — binary base64 RPC serialization for argument types beyond Reactor's native bool/byte/int/float/string: `SendPacked` sends `Vector2`, `Vector3`, `byte[]`, `long`, `ulong`, `double` etc. through a single string-keyed Reactor RPC. Works with `[ReactorRpc]` handlers reading a `string payload` argument.
- **CosmeticAPI** — player outfit management: `SetColor`, `SetHat`, `SetHatByProductId`, `SetSkin`, `SetSkinByProductId`, `SetPet`, `SetPetByProductId`, `SetNamePlate`. Find hats/skins/pets by ProductId substring.
- **MapAPI** — vent/console/door queries from `ShipStatus.Instance`: `AllVents`, `FindVentById`, `ClosestVent`, `ConnectedVents`, `AllConsoles`, `ClosestConsole`, `AllDoors`, `CloseDoors`, `RepairSystem`, `RandomSpawnPosition`.
- **SoundAPI** — extends `SpatialAudio` with 2D global/local sounds (`PlayGlobal`, `PlayLocal`), looping audio (`PlayLooping` / `StopLooping` / managed handles), kill sound (`PlayKillSound`), and name-based lookup (`PlaySoundByName`).
- **MeetingAPI** — meeting state queries: `IsInMeeting`, `AliveVoterCount`, `VotedCount`, `VoteTally`, `MostVotedPlayer`, `VotesFor`, `VoteTargetName`, `WasExiled`, `ExiledPlayerName`. Inject meeting chat messages via `SendMeetingChat`.
- **CoroutineRunner** — safe Unity coroutine execution from MonoBehaviour callbacks (avoids IEnumerator Harmony patching which segfaults on Linux IL2CPP). All coroutines auto-stop on `GameEvents.GameEnded`.
- **TaskAPI** — task progress queries: `TotalTasks`, `CompletedTasks`, `IsDone`, `CrewProgress`, `IncompleteTasks`, `RemainingTaskCount`.

## New UI APIs

- **HudOverlay** — player nameplate overrides and floating world-space indicators. `SetNameColor` changes a player's name color. `SetSubText` / `RemoveSubText` / `ClearSubTexts` manage colored sub-text lines positioned under the nameplate (per-player, keyed). `ShowFloatingText` creates a TextMeshPro indicator that follows the player. `ResetAll` cleans up on game end.
- **ButtonFactory** — safe clickable button creation without cloning the KillButton (which copies IL2CPP component trees and risks native delegate marshalling crashes). `Create` builds a sprite button with AspectPosition anchoring; `CreateText` makes text-only labels. Returns a `ManagedButton` handle — tick it and call `WasClicked()` to detect presses. No `OnClick.AddListener`.
- **NotificationAPI** — timed on-screen notification system. `Show` displays a colored message that stacks vertically (top-center) and auto-dismisses with a fade-out. Call `Tick` every frame. `Clear` dismisses all.
- **OverlayAPI** — persistent screen-space text overlay. `SetLine` places text lines at five corner positions (TopLeft, TopRight, BottomLeft, BottomRight, Center). `ShowBanner` displays a large centered banner that fades and auto-dismisses. `Tick` repositions all lines every frame. `Clear` removes everything.
- **CooldownBar** — horizontal progress/cooldown bar widget. `Create` builds a named bar with colored fill; `SetProgress(0-1)` updates the fill and color (red→yellow→green gradient). `SetVisible` toggles. `Destroy` cleans up.

## Expanded Settings API

`SettingsMenuBuilder` now has 7 control types (was 2):
- `AddSectionHeader` — bold gold section title, no controls
- `AddToggle` — On/Off switch (existing, unchanged)
- `AddNumeric` — ± step value (existing, unchanged)
- `AddDropdown` — cycle through string choices with arrow buttons
- `AddButton` — single action button with colored label
- `AddSlider` — fine-grained float slider (± small steps)
- `AddColorPicker` — cycle Palette.PlayerColors with preview swatch

**SettingsSync** — host-authoritative settings broadcast over Reactor RPC.
Host calls `BeginBuild`, `PushFloat`/`PushBool`/`PushInt`/`PushString`, `EndBuild`
inside their `SettingsMenuBuilder` callback. Clients call `RegisterFloat`/etc.
once in `Load()` with apply callbacks. Protocol is versioned binary base64. RPC
key: `classicus.manuapi.SettingsSync`.

## Renamed dependency

- `ClassicUs.Manactor` → `ClassicUs.Reactor` (namespace, package id, and GUID). All `Manactor*` types renamed to `Reactor*`.
- Target game: Classic Us 2026.8.16 (`ClassicUs.GameLibs` 2026.8.16.1).

# ManuAPI v1.3.24

First public release of ManuAPI, a modding framework for Classic Us (Among Us) BepInEx mods built on top of Reactor. This changelog consolidates every change from the initial 1.3.3 compatibility pass up to this release.

## Roles

- Custom role framework (`CustomRole`, `CustomImpostorRole`, `CustomCrewmateRole`) with automatic IL2CPP type registration, retrying safely until the type is ready.
- `RoleRegistry.Register` handles role assignment, team-based candidate selection with configurable count/chance, and syncing the assignment to every client.
- Native role text, description, team color, and kill-ability text are patched so custom roles display exactly like native ones in the intro cutscene, task list, settings menu, and exile screen.
- Fixed custom role types not registering under Classic Us 2026.7.4 due to a false-positive in Il2CppInterop's `ClassInjector.RewriteType` (it treated any type merely *named* like a BCL type, e.g. `SystemTypes`, as needing remapping).
- Fixed custom roles not being assignable in fast game starts (e.g. freeplay) by force-flushing pending IL2CPP registrations right before `RoleManager.AssignRolesForTeam` runs.
- Fixed custom role assignment being silently reverted shortly after being set: the native `PlayerControl.SetRole` call (part of the game's own role-sync flow) would overwrite the assignment a moment later. A guard now blocks the native call outright for any player with a custom role assigned, instead of reactively fighting it.
- Fixed a fatal CLR crash on the second match of a session: cached `RoleBehaviour` references from the previous (now-destroyed) `RoleManager` were read after being invalidated. All cached role state is now cleared on game end/restart.
- Removed an overly broad `TaskFolder` patch that corrupted vanilla role assignment mid-match; freeplay role-folder registration now uses a narrower, safe hook.

## Abilities

- `CustomAbility` base class: clone-of-the-kill-button ability buttons with cooldowns, timed effects, icons, and visibility/activation checks — no manual UI wiring required.
- `AbilityButtonGrid`: fixed, known-good bottom-left slot positions so multiple ability buttons from different mods line up instead of overlapping.
- Ability buttons now reset themselves automatically on death, restart, and game-end (`HudManager.Start`, `AmongUsClient.OnGameEnd`/`ExitGame`, `EndGameManager.NextGame`/`Exit`) instead of every mod re-implementing the same five Harmony patches.
- Fixed ability icons silently reverting to the native kill sprite after touching mod settings: `LoadableAsset<T>` now retries after a failed or Unity-destroyed load instead of permanently caching the failure, and `AbilityButton` re-applies its icon every tick instead of once at creation.

## Kills

- `KillManager` / `KillRequest`: a full, host-authoritative re-implementation of the murder flow that never touches the native `RpcMurderPlayer`/kill-animation path.
- Independently toggleable side effects: `TeleportKiller`, `CreateDeadBody` (colored to match the victim, spawned from the killer's own `KillAnimations` prefab), `PlayKillSound`, and `ShowKillAnimation` (shown only to the victim's own client, purely cosmetic).
- Replaces an earlier reactive approach (pausing/re-snapping the killer's `NetTransform` after the fact), which fought the native kill animation and produced visible rubber-banding.

## Settings & UI

- `SettingsMenuAPI` / `SettingsMenuBuilder`: inject toggle/numeric rows into the real in-game settings menu.
- Rows are now positioned using the native `SettingMenu.RepositionChildren()` after injection, so they land directly after vanilla options instead of using stale manual layout math.
- `ModBadgeAPI`: register a loaded-mod badge and a lobby prelobby tag.

## Assets

- `LoadableSprite`, `LoadableBundleAsset<T>`, `LoadableEmbeddedBundleAsset<T>`: embedded-resource and asset-bundle loading with retry-on-failure caching.
- Generalized asset loading beyond sprites: `AssetUtils.LoadAudioClipFromWavBytes`/`LoadAudioClipFromEmbeddedResource` and a new `LoadableSound` class, so mods can embed and play custom sounds with the same self-healing loader.

## Documentation

- Added a full documentation site (Docusaurus, served from `/docs`) covering installation, plugin setup, custom roles, ability buttons, the kill manager, settings menu, mod badges, and asset loading — with real examples pulled from a production mod.

## Dependencies

- Requires `ClassicUs.Reactor` 1.0.26+ and `ClassicUs.GameLibs` 2026.7.4.1, targeting Classic Us 2026.7.4.
