using System;
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
        private static SQLiteConnection learningResultDB;

        static void Main(string[] args)
        {
            learningResultDB = new SQLiteConnection("learnResults.sqlite3");
            learningResultDB.CreateTable<LearningResult>();

            const int SolveTrialCount = 100000;

            var learnCounts = new int[] { /*1000,*/ 10000, /*100000,*/ };
            var epsilons = new float[] { 0.1f, };
            var widths = new int[] { 3, 4, 5 };
            var heights = new int[] { 3, 4, 5 };
            var bombCounts = new int[] { 2,/* 3,*/ 4, /*5,*/ 6 };
            var rewardOneCells = new float[] { 0.05f, 0.1f, 0.2f,  };
            var rewardMultiCells = new float[] { 0.2f, 0.5f, 2f };
            var rewardDeads = new float[] { -1f, -2f};

            var tmp = from w in widths
                      from h in heights
                      from bomb in bombCounts
                      let boardConfig = new BoardConfig(w, h, bomb)
                      from learnCount in learnCounts
                      from epsilon in epsilons
                      from rewardOneCell in rewardOneCells
                      from rewardMultiCell in rewardMultiCells
                      from rewardDead in rewardDeads
                      select new LearningParam(boardConfig,
                                               learnCount,
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
                var evaluationValue = Learn(learningParam);
                learnStopwatch.Stop();

                // 学習結果を用いないで指定回数解いてみる。
                // すべての手をランダムに選択する。
                SolveParam solveParamUnuseLearningData = new SolveParam(learningParam.BoardConfig, new EvaluationValue(), SolveTrialCount);
                int solvedCountUnuseLearningData = Solve(solveParamUnuseLearningData);

                // 学習結果を用いて指定回数解いてみる。
                SolveParam solveParamUseLearningData = new SolveParam(learningParam.BoardConfig, evaluationValue, SolveTrialCount);
                int solvedCountUseLearningData = Solve(solveParamUseLearningData);

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
            logger.Info($"Epsilion|{learningParam.Epsilon}|");
            logger.Info($"学習にかかった時間|{learnStopwatch.Elapsed.ToString()}|");

            logger.Info($"性能確認回数回数|{solveTrialCount}|");
            logger.Info($"学習無しクリア回数|{solvedCountUnusedLearningData}|");
            logger.Info($"学習無しクリア率|{solvedCountUnusedLearningData / (double)solveTrialCount}|");
            logger.Info($"学習有りクリア回数|{solvedCountUseLearningData}|");
            logger.Info($"学習有りクリア率|{solvedCountUseLearningData / (double)solveTrialCount}|");

            var table = learningResultDB.Table<LearningResult>();
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
                LearnEpsilion = learningParam.Epsilon,
                RewardOpenOneCell = learningParam.RewardOpenOneCell,
                RewardOpenMultiCell = learningParam.RewardOpenMultiCell,
                RewardDead = learningParam.RewardDead,

                // Solve
                SolveTrialCount = solveTrialCount,
                SolvedCountUseLearningData = solvedCountUseLearningData,
                SolvedCountUnuseLearningData = solvedCountUnusedLearningData
            };
            learningResultDB.Insert(learningResult);
        }

        static EvaluationValue Learn(LearningParam learningParam)
        {
            MinesweeperLearner leaner = new MinesweeperLearner(learningParam);
            try
            {
                leaner.Learn();
            }
            catch(Exception e)
            {
                logger.Error(e);
            }

            if(learningParam.SaveValueFile)
            {
                leaner.EvaluationValue.SaveToCsvFile(learningParam.ValueCsvPath);
            }
            return leaner.EvaluationValue;
        }

        static int Solve(SolveParam solveParam)
        {
            MinesweeperCom com = new MinesweeperCom(solveParam.EvalutionValue, solveParam.ComRandomSeed, -1);
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
                    var currentAction = com.SelectCommand(game);

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
