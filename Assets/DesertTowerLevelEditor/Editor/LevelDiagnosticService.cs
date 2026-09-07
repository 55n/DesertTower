using System;

namespace DesertTower.Levels.Editor
{
    /// <summary>Optional editor extension point. The core never references a diagnostic implementation.</summary>
    public static class LevelDiagnosticService
    {
        public static Action<LevelRoot, int> StartHandler { private get; set; }
        public static bool IsAvailable => StartHandler != null;
        public static bool TryStart(LevelRoot root, int waveIndex)
        {
            if (StartHandler==null) return false;
            StartHandler(root,waveIndex); return true;
        }
    }
}
