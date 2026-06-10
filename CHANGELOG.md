# Changelog
Here are listed all the changes made in each update.

# [0.1.4](https://github.com/VELD-Dev/LC-BackroomsRenewed/releases/tag/v0.1.4) - 2026-06-10
- Fixed a bug preventing from teleporting into the backrooms
- Removed momentum cancellation as it was causing issues with teleportation
- Fixed backrooms generation (PR [#1](https://github.com/VELD-Dev/LC-BackroomsRenewed/pull/1) by Entity378)
- Fixed configs mismatch (PR [#1](https://github.com/VELD-Dev/LC-BackroomsRenewed/pull/1) by Entity378)

# [0.1.3](https://github.com/VELD-Dev/LC-BackroomsRenewed/releases/tag/v0.1.3) - 2026-06-09
- Fixed a bug causing backrooms to occasionally generate inside of a map's terrain.
- Removed any momentum the player could have had when teleporting to the backrooms.

# [0.1.2](https://github.com/VELD-Dev/LC-BackroomsRenewed/releases/tag/v0.1.2) - 2026-06-08
- Fixed a bug occurring when a player tried to join that would prevent from joining.

# [0.1.1](https://github.com/VELD-Dev/LC-BackroomsRenewed/releases/tag/v0.1.1) - 2026-06-08
- Fixed a bug occurring when a player tried to join that would prevent from joining.
- Fixed a game crash due to networking objects not synchronizing properly
- Fixed a bug that made some ceiling lights to be rendered black even if there was actually light.

# [0.1.0](https://youtu.be/dQw4w9WgXcQ) - 2026-06-07
- Implemented customizable procedural backrooms generation with 4 algorithms.
- Implemented basic ways to get TP'd to the backrooms
- Added many settings to push to another level custom gameplay experience. Some settings are synchronizable and can be modified only by host during the game.
- Added bases for cells variants and Backrooms themes/levels (I'll decide later if we only keep 1 level of the backrooms at once or if we generate several backroom levels)
- Added ambience effects (networked twinkling lights) for more immersion
- Added ambient noise (slight background TV static)