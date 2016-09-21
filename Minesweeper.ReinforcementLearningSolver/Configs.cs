using System;

namespace Minesweeper.ReinforcementLearningSolver
{
    static class Config
    {
        public static readonly int BoardWidth = 5;
        public static readonly int BoardHeight = 5;
        public static readonly int BombCount = 5;
    }

    static class LearningParam
    {
        // そのうちコンフィグに移す
        public static readonly int LearnCount = 100000;
        public static readonly int BoardRandomSeed = 0;
        public static readonly int ComRandomSeed = 0;
        public static readonly string LogPath = $"LearningResults/{Config.BoardHeight}x{Config.BoardWidth}_b{Config.BombCount}_{LearnCount}_{DateTime.Now.ToString(@"yyyy_MMdd_HHmmss")}_log.txt";
        public static readonly string ValueCsvPath = $"LearningResults/{Config.BoardHeight}x{Config.BoardWidth}_b{Config.BombCount}_{LearnCount}_{DateTime.Now.ToString(@"yyyy_MMdd_HHmmss")}.csv";
        public static readonly bool LoadValueFile = false;
        public static readonly bool SaveValueFile = true;
    }

    static class SolveParam
    {
        public static readonly int SolveCount = 1000;
        public static readonly int GameRandomSeed = 1192;
        public static readonly int ComRandomSeed = 1901;
        public static readonly string ValueCsvPath = LearningParam.ValueCsvPath;
        public static readonly bool LoadValueFile = true;
    }
}
