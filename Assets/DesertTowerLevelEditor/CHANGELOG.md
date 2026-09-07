# Changelog

## 0.2.0

- Removed Input System and movement dependencies from the level data assembly.
- Moved play diagnostics into a separately exportable, optional Editor-only module.
- Split diagnostic orchestration, wave scheduling, navigation, input, camera and visuals.
- Replaced mixed ActorDefinition with LevelElementDefinition; gameplay uses a catalog key and owns its own configuration.
- Renamed CanBuild to IsPointInBuildableZone to state its point-query scope.
- Added explicit spawn/target references with optional route guides and non-mutating legacy route resolution.
- Preserved script GUIDs and serialized field aliases; added real asset migration and dependency tests.
- Added ARCHITECTURE.md with game integration boundaries.

## 0.1.0

- Korean level authoring window with terrain entry points, block and road placement, prefab placement.
- Marker-first core, spawn, player, respawn and initial facility authoring.
- Planar polygon build/no-build/combat areas with height-aware point queries.
- Spawn-to-core routes, waypoint handles, wave group editing.
- Scoped navigation baking, path previews, structural and reference validation.
- Layout assets, undoable layout restore and source-only Unity package export.
- Fortress and canyon prototype generators, isolated navigation playtest, editor tests.
