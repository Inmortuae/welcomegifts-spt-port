# Welcome Gifts for SPT 4.1.6

A community continuation of Welcome Gifts by ShadowXtrex (EFCL) and Senko. Inmortuae maintains this port with ShadowXtrex's permission to publish and reuse the mod name and GUID. See the [upstream project](https://gitlab.com/ShadowXtrex/welcomegifts) and [MIT license](LICENSE).

## Install

1. Close the SPT game and server. Install [WTT CommonLib 3.0.8](https://sp-mod.com/mod/2310/wtt-commonlib).
2. Move any old Welcome Gifts folder out of `SPT_Runtime/user/mods` before upgrading.
3. Extract the release ZIP into your SPT game directory. The mod DLL belongs at `SPT_Runtime/user/mods/WelcomeGifts/WelcomeGifts.dll`.
4. Open Fence's tasks for most gifts and Prapor's tasks for the two survey gifts. Some gifts unlock after an earlier task.

## Version 4.3.2

Appreciation Package 1 is now available at Fence. Completing it unlocks Appreciation Package 2 after its existing 63-hour wait. BSG BDay 2020 follows after completing Appreciation Package 2 and waiting another 63 hours. Existing profiles keep their quest progress.

## Version 4.3.1

There are 100 gift quests. The 47 newer tasks use mod-authored [reward recipes](db/GiftRecipes.json) and assemble stock item presets from the local SPT installation at runtime. Quest IDs and follow-up prerequisites are retained from 4.3.0. Compatible WTT Content Backport installations get original gear for supported packs.

The recipe file records the public SPT item database revision and SHA-256 used to check every stock item reference. It also links public sources for the promo and seasonal rewards the mod adapts.

SPT 4.1.6 server startup was verified with CommonLib 3.0.8. Fika compatibility and in-game claiming are unverified.

## Build

Run `dotnet build -c Release` with the .NET 10 SDK.
