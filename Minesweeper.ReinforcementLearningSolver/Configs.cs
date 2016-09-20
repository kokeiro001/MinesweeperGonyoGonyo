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
        public static readonly string ValueCsvPath = @"value.csv";
        public static readonly bool LoadValueFile = false;
        public static readonly bool SaveValueFile = true;
    }

    static class SolveParam
    {
        public static readonly int SolveCount = 1000;
        public static readonly int GameRandomSeed = 1192;
        public static readonly int ComRandomSeed = 1901;
        public static readonly string ValueCsvPath = @"value.csv";
        public static readonly bool LoadValueFile = true;
    }
}
