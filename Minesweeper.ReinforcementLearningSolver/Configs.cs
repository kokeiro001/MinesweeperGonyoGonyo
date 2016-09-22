﻿using System;

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
        public bool SaveValueFile => true;

        public readonly BoardConfig BoardConfig;
        public readonly int LearnCount;
        public readonly float LearnStepSize;
        public readonly float Epsilon;

        public readonly string LogPath;
        public readonly string ValueCsvPath;


        public LearningParam(BoardConfig baordConfig, int learnCount, float learnStepSize, float epsilon)
        {
            BoardConfig = baordConfig;
            LearnCount = learnCount;
            LearnStepSize = learnStepSize;
            Epsilon = epsilon;

            LogPath =  $"LearningResults/{baordConfig.BoardHeight}x{baordConfig.BoardWidth}_b{baordConfig.BombCount}_{LearnCount}_s{learnStepSize * 100}_e{epsilon * 100}_{DateTime.Now.ToString(@"yyyy_MMdd_HHmmss")}_log.txt";
            ValueCsvPath = $"LearningResults/{baordConfig.BoardHeight}x{baordConfig.BoardWidth}_b{baordConfig.BombCount}_{LearnCount}_s{learnStepSize * 100}_e{epsilon * 100}_{DateTime.Now.ToString(@"yyyy_MMdd_HHmmss")}.csv";
        }
    }

    class SolveParam
    {
        public int GameRandomSeed => 1192;
        public int ComRandomSeed => 1901;

        public readonly int SolveCount;
        public readonly string ValueCsvPath;

        public readonly BoardConfig BoardConfig;
        public readonly bool LoadValueFile;
        public readonly float Epsilon;

        public SolveParam(BoardConfig boardConfig, int solveCount, string valueCsvPath, float epsilion)
        {
            BoardConfig = boardConfig;
            SolveCount = solveCount;
            Epsilon = epsilion;
            if(string.IsNullOrEmpty(valueCsvPath))
            {
                LoadValueFile = false;
            }
            else
            {
                LoadValueFile = true;
                ValueCsvPath = valueCsvPath;
            }
        }
    }
}
