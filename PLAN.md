# CarGame Plan

Last updated: 2026-05-15

## Vision

Build a small Need-for-Speed-inspired arcade racing game as a personal gift project. Keep the scope compact, polished, and playable: a few race tracks, AI opponents, boost pickups, race mode, and time attack mode.

## Status Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Done

## Current Focus

- `[x]` Fix chase camera stutter.
- `[x]` Remove first-person camera mode.
- `[x]` Move active development to `Assets/CartoonTracksPack1/Track1/Demo Scenes/complete_track_demo.unity`.
- `[x]` Add the player car and chase camera to the new track scene.
- `[x]` Build race waypoints/checkpoints for the new track.
- `[x]` Add editor tooling to place track points manually.
- `[x]` Infer an AI route graph from placed checkpoints without requiring manual branch marking.
- `[x]` Add lightweight AI raycast and turn awareness for speed/steering decisions.
- `[x]` Add a pre-race setup screen for lap count and AI difficulty.
- `[x]` Add player boost meter, boost input, and boost pickups.
- `[x]` Add difficulty-aware AI rubberbanding to keep races tense without a minimap.
- `[x]` Organize custom scripts into a clean runtime/editor folder structure.
- `[x]` Add speed readout and boost-aware camera FOV kick.
- `[~]` Set up the new car pack as selectable player car visuals.
- `[x]` Connect imported boost sprite and boost VFX assets to the existing boost system.
- `[~]` Prepare imported timer sprite, coin pack, and night map for Time Attack/map selection work.
- `[~]` Add start/finish trigger lap counting that validates route progress first.
- `[~]` Validate and tune AI driving on the completed checkpoint map.

## Milestone 1: Track Scene Integration

- `[x]` Set up `complete_track_demo` as the working race scene.
- `[~]` Add a clean player spawn point.
- `[x]` Add the player car prefab to the track scene.
- `[x]` Attach/configure `CarCameraController` on the scene camera.
- `[ ]` Tune car position, rotation, and camera values for the track.
- `[ ]` Confirm the car drives cleanly on the Cartoon Tracks road mesh.
- `[x]` Add track checkpoint/waypoint data for laps and AI navigation.
- `[x]` Add an editor tool for manually placing and reordering checkpoints.
- `[x]` Extend the editor tool so points can be connected as branches/alternate routes.
- `[x]` Add automatic route graph inference from checkpoint positions.
- `[~]` Add a visible start/finish trigger collider at the lap line.

## Milestone 2: Race Mode

- `[ ]` Create a reusable map definition system for multiple race tracks.
- `[~]` Support player-selected lap count, countdown, race timer, player position, and finish result.
- `[x]` Spawn AI opponents at the selected map's start grid.
- `[~]` Count laps from the start/finish trigger after required checkpoint progress.
- `[~]` Make AI follow the track route graph reliably.
- `[~]` Let AI choose short, normal, or wide paths based on difficulty and current driving situation.
- `[x]` Add difficulty-aware rubberbanding so unseen opponents stay competitive.
- `[x]` Add difficulty selection: Easy, Medium, Hard, EMPRESS.
- `[x]` Add raycast-based AI awareness for nearby cars/obstacles and track-side correction.
- `[~]` Tune each difficulty with speed, steering, braking, and mistake/forgiveness values.
- `[~]` Tune difficulty route preference: Easy safer/wider, Medium balanced, Hard tighter, EMPRESS shortest/aggressive.
- `[x]` Add a race HUD with lap, position, timer, speed, boost, and standings.
- `[x]` Add a simple finish screen.

## Milestone 3: Map And Mode Selection

- `[ ]` Create a main menu flow.
- `[ ]` Add map selection.
- `[~]` Register/import the night city map as a future selectable track.
- `[ ]` Add mode selection: Race or Time Attack.
- `[~]` Add player car selection.
- `[ ]` Add AI difficulty selection for Race mode.
- `[ ]` Make it easy to add more track maps later without rewriting race logic.

## Milestone 4: Boost System

- `[x]` Add boost meter to the player.
- `[x]` Add boost input.
- `[x]` Add boost pickups on the track.
- `[x]` Make boost pickups refill/increase the boost meter.
- `[~]` Add boost effects: FOV kick, camera feel, particles, sound, and speed trail if available.
- `[x]` Add pickup respawn behavior.
- `[x]` Render boost pickups using the imported 2D boost sprite with a 3D trigger collider.
- `[x]` Add hyperdrive screen VFX and rear nitro VFX while the player is boosting.
- `[ ]` Decide whether AI opponents can use boost.

## Milestone 5: Time Attack Mode

- `[ ]` Add collectible coins to each track.
- `[~]` Pick and register a default 3D coin prefab from the imported coin pack.
- `[ ]` Add time attack timer.
- `[ ]` Player wins by collecting all coins before time runs out.
- `[ ]` Add time powerups that extend the timer.
- `[ ]` Add coin and time pickup UI feedback.
- `[ ]` Add win/loss screen for time attack.

## Milestone 6: Enhanced Camera And Game Feel

- `[x]` Smooth chase camera and remove stutter.
- `[x]` Remove first-person camera mode.
- `[ ]` Add polished chase camera presets for normal driving, high speed, drift, and boost.
- `[x]` Add controlled speed-based FOV.
- `[ ]` Add optional drift/boost camera shake that does not stutter.
- `[ ]` Add subtle camera tilt/lean during turns if it feels good.
- `[~]` Add stronger boost presentation.

## Milestone 7: Polish

- `[ ]` Add start lights/countdown presentation.
- `[ ]` Add engine, boost, pickup, UI, and finish sounds.
- `[ ]` Add simple music loop.
- `[ ]` Add pause/restart flow.
- `[ ]` Add small personal touches for the gift version.
- `[ ]` Build a playable demo.

## Technical Notes

- Keep custom gameplay scripts in `Assets/Scripts/Runtime`.
- Keep editor-only tools in `Assets/Scripts/Editor`.
- Avoid modifying imported asset package files unless there is a strong reason.
- Prefer map/difficulty configuration data over hardcoded scene-specific logic.
- Keep a Prometeo-compatible controller surface for existing race systems, but use our own lightweight arcade controller now that the old package source was removed.
- Keep systems small and testable: race flow, AI, pickups, modes, UI, and camera should stay separate.
- Track points should become a route graph: points are nodes, connections are legal road segments, and AI chooses a route through the graph.
- The default workflow is now checkpoint-first: place points in rough driving order, then let `RaceTrackDefinition` infer the forward graph and likely shortcut links.
- Manual route links remain available as optional corrections if one track needs a special branch.
- The final checkpoint is not auto-drawn back to the start by default, avoiding confusing loop lines in the editor; lap completion still uses the start/finish trigger.
- The start point is also the finish/lap point for race mode.
- Laps should be counted by a start/finish trigger only after the racer has made valid progress through required route/checkpoint gates.
- Use trigger gates for race progress and placement; use route points for AI steering.
- AI opponents should combine route graph steering with local raycast awareness, so they can slow for nearby obstacles and adapt to sharp turns based on difficulty.
- Rubberbanding should use progress score, not teleporting: opponents behind the player get a smooth speed multiplier, while opponents far ahead ease off according to difficulty.
- Player boost is handled by `ArcadeBoostController`; boost pickups are reusable trigger objects and can be auto-created from route checkpoints for quick testing.
- Car visuals are now driven by `Assets/Resources/ArcadeCarCatalog.asset`; the race setup screen swaps the selected visual onto a generated arcade driving rig.
- Pickup/VFX defaults are driven by `Assets/Resources/ArcadePickupCatalog.asset`; it references `Boost.png`, `Timer.png`, a default gold coin prefab, `vfx_Hyperdrive_01`, and `CarNitroVFX`.
- Boost pickups keep 3D trigger colliders while rendering the imported 2D boost sprite as a camera-facing world sprite.
- Player boost creates two optional VFX instances at runtime: a camera-attached hyperdrive effect and a car-attached nitro effect. Tune offsets/scales on `ArcadeBoostVfx` after visual testing.
- Do not use NavMesh for the main racing AI unless the waypoint graph proves insufficient.

## Project Structure

- `Assets/Scripts/Runtime/Race`: race lifecycle, participants, start/finish trigger, bootstrap.
- `Assets/Scripts/Runtime/AI`: opponent driving, route following, awareness, rubberbanding use.
- `Assets/Scripts/Runtime/Boost`: boost meter/controller and pickup behavior.
- `Assets/Scripts/Runtime/Pickups`: shared pickup asset catalog and future pickup helpers.
- `Assets/Scripts/Runtime/Track`: track definitions, checkpoint graph data, map metadata.
- `Assets/Scripts/Runtime/UI`: runtime HUD, race setup, and result UI.
- `Assets/Scripts/Runtime/Camera`: custom camera behavior.
- `Assets/Scripts/Runtime/Vehicle`: selected car catalog, runtime vehicle rig setup, and the lightweight car controller compatibility layer.
- `Assets/Scripts/Editor/Track`: Unity editor tooling for track/checkpoint setup.

## Known Risks

- The Cartoon Tracks scene currently starts as an art/demo scene, so it needs player, camera, race manager, and waypoint setup.
- AI quality depends heavily on route graph placement, connection quality, and braking logic.
- Boost can make car physics unstable if torque/speed changes are too sudden.
- Multiple maps will be easier if map setup is data-driven from the beginning.

## Track Graph Design Notes

The track editor should support more than a single ordered checkpoint list. The desired model is:

- Route points placed on the road surface.
- Connections between route points, forming a directed or bidirectional graph.
- Start/finish line represented by a trigger collider at the start point.
- Optional checkpoint gates around the track to validate lap progress.
- Branches for short and long paths.
- AI route planner chooses the next point/branch using difficulty and driving context.
- Difficulty should not need separate tracks. The same graph can support all AI levels by changing route choice, speed, braking distance, steering aggression, and recovery behavior.

Initial AI behavior by difficulty:

- Easy: prefers safer/wider paths, brakes earlier, slower top speed, less aggressive overtaking.
- Medium: balanced racing line, moderate speed, normal braking.
- Hard: tighter racing line, later braking, higher speed, more willing to use shortcuts.
- EMPRESS: shortest/aggressive route choices, highest speed, late braking, minimal hesitation.

## Checkpoint Mapping Workflow

- Place checkpoint objects around the drivable route in rough forward order.
- Keep checkpoint 0 near the start/finish line.
- Shortcut or branch points can be placed where they naturally rejoin the forward route; the auto graph looks for forward links that save distance.
- Use the route graph preview in the `RaceTrackDefinition` inspector to check inferred links.
- If an inferred shortcut is too aggressive or missing, tune `Auto Graph Max Connection Distance`, `Auto Graph Max Index Skip`, and `Auto Graph Minimum Shortcut Saving`.
- Use manual route links only as corrections, not as the normal mapping workflow.
