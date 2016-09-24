using System;

namespace Minesweeper.ReinforcementLearningSolver
{
    class BoardConfig
    {
        public readonly int BoardWidth;
        public readonly int BoardHeight;
        public readonly int BombCount;

        public BoardConfig(int width, int height, int bombCount)
        {
            BoardWidth = width;
            BoardHeight = height;
            BombCount = bombCount;
        }
    }

    class LearningParam
    {
        public int BoardRandomSeed => 0;
        public int ComRandomSeed => 0;

        public bool LoadValueFile => false;
        public bool SaveValueFile => false;

        public readonly BoardConfig BoardConfig;
        public readonly int LearnCount;
        public readonly float Epsilon;

        public readonly float RewardOpenOneCell;
        public readonly float RewardOpenMultiCell;
        public readonly float RewardDead;

        public readonly string LogPath;
        public readonly string ValueCsvPath;

        public LearningParam(
            BoardConfig baordConfig, 
            int learnCount, 
            float epsilon,
            float rewardOpenOneCell,
            float rewardOpenMultiCell,
            float rewardDead)
        {
            BoardConfig = baordConfig;
            LearnCount = learnCount;
            Epsilon = epsilon;
            RewardOpenOneCell = rewardOpenOneCell;
            RewardOpenMultiCell = rewardOpenMultiCell;
            RewardDead = rewardDead;

            string baseFilePath = $"LearningResults/{baordConfig.BoardHeight}x{baordConfig.BoardWidth}_b{baordConfig.BombCount}_{LearnCount}_e{epsilon * 100}_{DateTime.Now.ToString(@"yyyy_MMdd_HHmmss")}";
            LogPath =  $"{baseFilePath}.log";
            ValueCsvPath = $"{baseFilePath}.csv";
        }
    }

    class SolveParam
    {
        enum LoadValueFrom
        {
            OnMemory,
            CsvFile
        }

        public int GameRandomSeed => 1192;
        public int ComRandomSeed => 1901;

        public readonly int SolveTrialCount;

        public readonly BoardConfig BoardConfig;
        public readonly EvaluationValue Value;

        public SolveParam(BoardConfig boardConfig, EvaluationValue value, int solveTrialCount)
        {
            BoardConfig = boardConfig;
            Value = value;
            SolveTrialCount = solveTrialCount;
        }
    }
}
