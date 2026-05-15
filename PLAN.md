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
- `[ ]` Support player-selected lap count, countdown, race timer, player position, and finish result.
- `[x]` Spawn AI opponents at the selected map's start grid.
- `[~]` Count laps from the start/finish trigger after required checkpoint progress.
- `[~]` Make AI follow the track route graph reliably.
- `[~]` Let AI choose short, normal, or wide paths based on difficulty and current driving situation.
- `[~]` Add difficulty selection: Easy, Medium, Hard, EMPRESS.
- `[x]` Add raycast-based AI awareness for nearby cars/obstacles and track-side correction.
- `[~]` Tune each difficulty with speed, steering, braking, and mistake/forgiveness values.
- `[~]` Tune difficulty route preference: Easy safer/wider, Medium balanced, Hard tighter, EMPRESS shortest/aggressive.
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
- Track points should become a route graph: points are nodes, connections are legal road segments, and AI chooses a route through the graph.
- The default workflow is now checkpoint-first: place points in rough driving order, then let `RaceTrackDefinition` infer the forward graph and likely shortcut links.
- Manual route links remain available as optional corrections if one track needs a special branch.
- The final checkpoint is not auto-drawn back to the start by default, avoiding confusing loop lines in the editor; lap completion still uses the start/finish trigger.
- The start point is also the finish/lap point for race mode.
- Laps should be counted by a start/finish trigger only after the racer has made valid progress through required route/checkpoint gates.
- Use trigger gates for race progress and placement; use route points for AI steering.
- AI opponents should combine route graph steering with local raycast awareness, so they can slow for nearby obstacles and adapt to sharp turns based on difficulty.
- Do not use NavMesh for the main racing AI unless the waypoint graph proves insufficient.

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
