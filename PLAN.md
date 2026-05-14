# CarGame Plan

Last updated: 2026-05-14

## Vision

Build a small Need-for-Speed-inspired arcade racing game as a personal gift project. Keep the scope compact, polished, and playable: a few race tracks, AI opponents, boost pickups, race mode, and time attack mode.

## Status Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Done

## Current Focus

- `[x]` Fix chase camera stutter.
- `[x]` Remove first-person camera mode.
- `[~]` Move active development to `Assets/CartoonTracksPack1/Track1/Demo Scenes/complete_track_demo.unity`.
- `[ ]` Add the player car and chase camera to the new track scene.
- `[ ]` Build race waypoints/checkpoints for the new track.

## Milestone 1: Track Scene Integration

- `[ ]` Set up `complete_track_demo` as the working race scene.
- `[ ]` Add a clean player spawn point.
- `[ ]` Add the player car prefab to the track scene.
- `[ ]` Attach/configure `CarCameraController` on the scene camera.
- `[ ]` Tune car position, rotation, and camera values for the track.
- `[ ]` Confirm the car drives cleanly on the Cartoon Tracks road mesh.
- `[ ]` Add track checkpoint/waypoint data for laps and AI navigation.

## Milestone 2: Race Mode

- `[ ]` Create a reusable map definition system for multiple race tracks.
- `[ ]` Support lap count, countdown, race timer, player position, and finish result.
- `[ ]` Spawn AI opponents at the selected map's start grid.
- `[ ]` Make AI follow the track waypoints reliably.
- `[ ]` Add difficulty selection: Easy, Medium, Hard, EMPRESS.
- `[ ]` Tune each difficulty with speed, steering, braking, and mistake/forgiveness values.
- `[ ]` Add a race HUD with lap, position, timer, speed, boost, and standings.
- `[ ]` Add a simple finish screen.

## Milestone 3: Map And Mode Selection

- `[ ]` Create a main menu flow.
- `[ ]` Add map selection.
- `[ ]` Add mode selection: Race or Time Attack.
- `[ ]` Add AI difficulty selection for Race mode.
- `[ ]` Make it easy to add more track maps later without rewriting race logic.

## Milestone 4: Boost System

- `[ ]` Add boost meter to the player.
- `[ ]` Add boost input.
- `[ ]` Add boost pickups on the track.
- `[ ]` Make boost pickups refill/increase the boost meter.
- `[ ]` Add boost effects: FOV kick, camera feel, particles, sound, and speed trail if available.
- `[ ]` Add pickup respawn behavior.
- `[ ]` Decide whether AI opponents can use boost.

## Milestone 5: Time Attack Mode

- `[ ]` Add collectible coins to each track.
- `[ ]` Add time attack timer.
- `[ ]` Player wins by collecting all coins before time runs out.
- `[ ]` Add time powerups that extend the timer.
- `[ ]` Add coin and time pickup UI feedback.
- `[ ]` Add win/loss screen for time attack.

## Milestone 6: Enhanced Camera And Game Feel

- `[x]` Smooth chase camera and remove stutter.
- `[x]` Remove first-person camera mode.
- `[ ]` Add polished chase camera presets for normal driving, high speed, drift, and boost.
- `[ ]` Add controlled speed-based FOV.
- `[ ]` Add optional drift/boost camera shake that does not stutter.
- `[ ]` Add subtle camera tilt/lean during turns if it feels good.
- `[ ]` Add stronger boost presentation.

## Milestone 7: Polish

- `[ ]` Add start lights/countdown presentation.
- `[ ]` Add engine, boost, pickup, UI, and finish sounds.
- `[ ]` Add simple music loop.
- `[ ]` Add pause/restart flow.
- `[ ]` Add small personal touches for the gift version.
- `[ ]` Build a playable demo.

## Technical Notes

- Keep custom gameplay scripts in `Assets/Scripts`.
- Avoid modifying imported asset package files unless there is a strong reason.
- Prefer map/difficulty configuration data over hardcoded scene-specific logic.
- Reuse the existing Prometeo car controller for now.
- Keep systems small and testable: race flow, AI, pickups, modes, UI, and camera should stay separate.

## Known Risks

- The Cartoon Tracks scene currently starts as an art/demo scene, so it needs player, camera, race manager, and waypoint setup.
- AI quality depends heavily on waypoint placement and braking logic.
- Boost can make car physics unstable if torque/speed changes are too sudden.
- Multiple maps will be easier if map setup is data-driven from the beginning.
