namespace DesertTower.Levels
{
    /// <summary>Resolved spatial input for a game-owned wave system. Never commands an actor.</summary>
    public readonly struct SpawnBinding
    {
        public LevelMarker Spawn { get; }
        public LevelMarker Target { get; }
        public LevelRoute SuggestedRoute { get; }

        public SpawnBinding(LevelMarker spawn, LevelMarker target, LevelRoute suggestedRoute)
        { Spawn=spawn; Target=target; SuggestedRoute=suggestedRoute; }
    }
}
