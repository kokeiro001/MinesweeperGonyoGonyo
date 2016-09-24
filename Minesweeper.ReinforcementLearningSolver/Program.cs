﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Minesweeper.Common;
using System.Diagnostics;
using log4net;
using SQLite;

namespace Minesweeper.ReinforcementLearningSolver
{
    class Program
    {
        static private ILog logger = LogManager.GetLogger("MainLog");
        private static SQLiteConnection db;

        static void Main(string[] args)
        {
            db = new SQLiteConnection("learnResults.sqlite3");
            db.CreateTable<LearningResult>();

            const int SolveTrialCount = 100000;

            var learnCounts = new int[] { /*1000,*/ 10000, /*100000,*/ };
            var learnSteps = new float[] { 0.01f, 0.05f, 0.1f, 0.15f, 0.2f };
            var epsilons = new float[] { 0.01f, 0.05f, 0.1f, 0.15f, 0.2f };
            var widths = new int[] { 3, 4, 5 };
            var heights = new int[] { 3, 4, 5 };
            var bombCounts = new int[] { 2,/* 3,*/ 4, /*5,*/ 6 };
            var rewardOneCells = new float[] { 0.05f, 0.1f, 0.2f, 0.5f, 1f };
            var rewardMultiCells = new float[] { 0.05f, 0.1f, 0.2f, 0.5f, 2f };
            var rewardDeads = new float[] { -0.1f, -0.2f, -0.5f, -1f, -2f};

            var tmp = from w in widths
                      from h in heights
                      from bomb in bombCounts
                      let boardConfig = new BoardConfig(w, h, bomb)
                      from learnCount in learnCounts
                      from step in learnSteps
                      from epsilon in epsilons
                      from rewardOneCell in rewardOneCells
                      from rewardMultiCell in rewardMultiCells
                      from rewardDead in rewardDeads
                      select new LearningParam(boardConfig,
                                               learnCount,
                                               step,
                                               epsilon,
                                               rewardOneCell,
                                               rewardMultiCell,
                                               rewardDead);
            var paramList = tmp.ToList();

            for(int i = 0; i < paramList.Count; i++)
            {
                var learningParam = paramList[i];
                LogInitializer.InitLog("MainLog", learningParam.LogPath);

                Stopwatch learnStopwatch = new Stopwatch();
                learnStopwatch.Start();
                var value = Learn(learningParam);
                learnStopwatch.Stop();

                // 学習結果を用いないで
                SolveParam solveParamUnuseLearningData = new SolveParam(learningParam.BoardConfig, SolveTrialCount, "", 0);
                int solvedCountUnuseLearningData = Solve(solveParamUnuseLearningData);

                // 学習結果を用いて
                //SolveParam solveParamUseLearningData = new SolveParam(learningParam.BoardConfig, SolveTrialCount, learningParam.ValueCsvPath, learningParam.Epsilon);
                SolveParam solveParamUseLearningData = new SolveParam(learningParam.BoardConfig, SolveTrialCount, "", learningParam.Epsilon);
                int solvedCountUseLearningData = Solve(solveParamUseLearningData, value);

                WriteLog(learningParam.BoardConfig, learningParam, learnStopwatch, SolveTrialCount, solvedCountUnuseLearningData, solvedCountUseLearningData);

                Console.WriteLine($"{i}/{paramList.Count}");
            }

            logger.Info($"GC.CollectionCount(0)|{GC.CollectionCount(0).ToString()}|");
            logger.Info($"GC.CollectionCount(1)|{GC.CollectionCount(1).ToString()}|");
            logger.Info($"GC.CollectionCount(2)|{GC.CollectionCount(2).ToString()}|");
        }

        private static void WriteLog(
            BoardConfig boardConfig, 
            LearningParam learningParam, 
            Stopwatch learnStopwatch, 
            int solveTrialCount, 
            int solvedCountUnusedLearningData, 
            int solvedCountUseLearningData)
        {
            logger.Info("| 要素 | 値");
            logger.Info("------------ | -------------");
            logger.Info($"ボードサイズ|{boardConfig.BoardWidth}x{boardConfig.BoardHeight}|");
            logger.Info($"爆弾の数|{boardConfig.BombCount}|");
            logger.Info($"学習回数|{learningParam.LearnCount}|");
            logger.Info($"LearnStep|{learningParam.LearnStepSize}|");
            logger.Info($"Epsilion|{learningParam.Epsilon}|");
            logger.Info($"学習にかかった時間|{learnStopwatch.Elapsed.ToString()}|");

            logger.Info($"性能確認回数回数|{solveTrialCount}|");
            logger.Info($"学習無しクリア回数|{solvedCountUnusedLearningData}|");
            logger.Info($"学習無しクリア率|{solvedCountUnusedLearningData / (double)solveTrialCount}|");
            logger.Info($"学習有りクリア回数|{solvedCountUseLearningData}|");
            logger.Info($"学習有りクリア率|{solvedCountUseLearningData / (double)solveTrialCount}|");

            var table = db.Table<LearningResult>();
            var learningResult = new LearningResult()
            {
                AlgorithmVersion = "hoge",
                CreatedAt = DateTime.Now,

                // Board
                BoardWidth = boardConfig.BoardWidth,
                BoardHeight = boardConfig.BoardHeight,
                BombCount = boardConfig.BombCount,

                // Learn
                LearnCount = learningParam.LearnCount,
                LearnSeconds = (uint)learnStopwatch.Elapsed.TotalSeconds,
                LearnStepSize = learningParam.LearnStepSize,
                LearnEpsilion = learningParam.Epsilon,
                RewardOpenOneCell = learningParam.RewardOpenOneCell,
                RewardOpenMultiCell = learningParam.RewardOpenMultiCell,
                RewardDead = learningParam.RewardDead,

                // Solve
                SolveTrialCount = solveTrialCount,
                SolvedCountUseLearningData = solvedCountUseLearningData,
                SolvedCountUnuseLearningData = solvedCountUnusedLearningData
            };
            db.Insert(learningResult);
        }

        static EvaluationValue Learn(LearningParam learningParam)
        {
            EvaluationValue value = new EvaluationValue(learningParam.LearnStepSize);

            if(learningParam.LoadValueFile)
            {
                value.LoadFromCsvFile(learningParam.ValueCsvPath);
            }

            MinesweeperCom com = new MinesweeperCom(value, true, learningParam.ComRandomSeed, learningParam.Epsilon);
            MinesweeperGame game = new MinesweeperGame(
                learningParam.BoardConfig.BoardWidth, 
                learningParam.BoardConfig.BoardHeight, 
                learningParam.BoardConfig.BombCount, 
                learningParam.BoardRandomSeed);
            MinesweeperLearner leaner = new MinesweeperLearner(game, com, value, learningParam);

            try
            {
                learningProgress = 0;
                for(int i = 0; i < learningParam.LearnCount; i++)
                {
                    leaner.Learn();
                    LearningProgressView(i, learningParam.LearnCount);
                }
            }
            catch(Exception e)
            {
                logger.Error("メモリなくなったんじゃないかな(´・ω・`)", e);
            }

            // save
            if(learningParam.SaveValueFile)
            {
                value.SaveToCsvFile(learningParam.ValueCsvPath);
            }
            return value;
        }

        static int learningProgress = 0;
        static void LearningProgressView(int currentLeanCount, int learnCount)
        {
            float per = ((float)currentLeanCount / learnCount) * 100f;
            if(per >= learningProgress)
            {
                learningProgress += 1;
                logger.Debug($"Progress={learningProgress}%, LearnCount={currentLeanCount}");
            }
        }


        static int Solve(SolveParam solveParam, EvaluationValue value = null)
        {
            if(value == null)
            {
                value = new EvaluationValue(0);
                if(solveParam.LoadValueFile)
                {
                    value.LoadFromCsvFile(solveParam.ValueCsvPath);
                }
            }

            MinesweeperCom com = new MinesweeperCom(value, false, solveParam.ComRandomSeed, solveParam.Epsilon);
            MinesweeperGame game = new MinesweeperGame(
                solveParam.BoardConfig.BoardWidth, 
                solveParam.BoardConfig.BoardHeight,
                solveParam.BoardConfig.BombCount, 
                solveParam.GameRandomSeed);

            int solvedCount = 0;
            ulong[] boardHashBuf = new ulong[2];
            for(int i = 0; i < solveParam.SolveTrialCount; i++)
            {
                game.ClearBoard();
                game.GenerateRandomBoard();

                while(true)
                {
                    var currentAction = com.SelectCommand(game.Board);

                    game.Board.MakeHash(boardHashBuf);
                    int idx = currentAction.Y * solveParam.BoardConfig.BoardWidth + currentAction.X;
                    var result = game.OpenCell(idx);

                    if(result.IsClear)
                    {
                        solvedCount++;
                        break;
                    }
                    else if(result.IsDead)
                    {
                        break;
                    }
                }
            }
            return solvedCount;
        }
    }

    static class MinesweeperExtensions
    {
        // HACK: 拡張元におとなしくもたせる
        public static GameCommand[] ValidCommands(this MinesweeperBoard board)
        {
            return board
                    .Where(c => c.State == CellState.Close)
                    .Select(c => new GameCommand(c.BoardIndex / board.Width, c.BoardIndex % board.Width, GameCommandType.Open))
                    .ToArray();
        }

        public static void ShowBoardUser(this MinesweeperGame game)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" |" + string.Join("", Enumerable.Range(0, game.Width).Select(num => num.ToString())));
            sb.AppendLine(" +" + string.Join("", Enumerable.Range(0, game.Width).Select(_ => "-")));
            for(int y = 0; y < game.Height; y++)
            {
                sb.Append($"{y}|");
                for(int x = 0; x < game.Width; x++)
                {
                    var cell = game[y * game.Width + x];
                    if(cell.State == CellState.Flag)
                    {
                        sb.Append("!");
                    }
                    else if(cell.State == CellState.Open)
                    {
                        if(cell.HasBomb)
                        {
                            sb.Append("*");
                        }
                        else
                        {
                            sb.Append(cell.Value);
                        }
                    }
                    else
                    {
                        sb.Append("?");
                    }
                }
                sb.AppendLine();
            }
            LogManager.GetLogger("MainLog").Info(sb.ToString());
        }

    }
}
