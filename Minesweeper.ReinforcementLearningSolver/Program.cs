using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Minesweeper.Common;
using System.Diagnostics;
using log4net;

namespace Minesweeper.ReinforcementLearningSolver
{
    class Program
    {
        static private ILog logger = LogManager.GetLogger("MainLog");

        static List<int> cleardCount = new List<int>();

        class Param
        {
            public int LearnCount;
            public float LearnStep;
            public float Epsilon;
            public int Width;
            public int Height;
            public int BombCount;
        }

        static void Main(string[] args)
        {

            const int SolveCount = 10000;

            var learnCounts = new int[] { 1000, 10000, 100000, };
            var learnSteps = new float[] { 0.01f, 0.05f, 0.1f, 0.15f, 0.2f };
            var epsilons = new float[] { 0.01f, 0.05f, 0.1f, 0.15f, 0.2f };
            var widths = new int[] { 3, 4, 5 };
            var heights = new int[] { 3, 4, 5 };
            var bombCounts = new int[] { 2, 3, 4, 5, 6};

            List<Param> paramList = new List<Param>();
            foreach(var learnCount in learnCounts)
            {
                foreach(var step in learnSteps)
                {
                    foreach(var epsilon in epsilons)
                    {
                        foreach(var w in widths)
                        {
                            foreach(var h in heights)
                            {
                                foreach(var b in bombCounts)
                                {
                                    paramList.Add(new Param()
                                    {
                                        LearnCount = learnCount,
                                        LearnStep = step,
                                        Epsilon = epsilon,
                                        Width = w,
                                        Height = h,
                                        BombCount = b
                                    });
                                }
                            }
                        }
                    }
                }
            }


            for(int i = 0; i < paramList.Count; i++)
            {
                Param param = paramList[i];
                BoardConfig boardConfig = new BoardConfig(param.Width, param.Height, param.BombCount);
                LearningParam learningParam = new LearningParam(boardConfig, param.LearnCount, param.LearnStep, param.Epsilon);
                LogInitializer.InitLog("MainLog", learningParam.LogPath);

                Stopwatch learnStopwatch = new Stopwatch();
                learnStopwatch.Start();
                Learn(learningParam);
                learnStopwatch.Stop();

                // 学習結果を用いないで
                SolveParam solveParamNotLearning = new SolveParam(boardConfig, SolveCount, "", param.Epsilon);
                int clearCountNotLearning = Solve(solveParamNotLearning);

                // 学習結果を用いて
                SolveParam solveParamUseLearning = new SolveParam(boardConfig, SolveCount, learningParam.ValueCsvPath, param.Epsilon);
                int clearCountUseLearning = Solve(solveParamUseLearning);


                logger.Info("| 要素 | 値");
                logger.Info("------------ | -------------");
                logger.Info($"ボードサイズ|{boardConfig.BoardWidth}x{boardConfig.BoardHeight}|");
                logger.Info($"爆弾の数|{boardConfig.BombCount}|");
                logger.Info($"学習回数|{learningParam.LearnCount}|");
                logger.Info($"LearnStep|{learningParam.LearnStepSize}|");
                logger.Info($"Epsilion|{learningParam.Epsilon}|");
                logger.Info($"学習にかかった時間|{learnStopwatch.Elapsed.ToString()}|");

                logger.Info($"性能確認回数回数|{SolveCount}|");
                logger.Info($"学習無しクリア回数|{clearCountNotLearning}|");
                logger.Info($"学習無しクリア率|{clearCountNotLearning / (double)SolveCount}|");
                logger.Info($"学習有りクリア回数|{clearCountUseLearning}|");
                logger.Info($"学習有りクリア率|{clearCountUseLearning / (double)SolveCount}|");

                Console.WriteLine($"{i}/{paramList.Count}");
            }

            logger.Info($"GC.CollectionCount(0)|{GC.CollectionCount(0).ToString()}|");
            logger.Info($"GC.CollectionCount(1)|{GC.CollectionCount(1).ToString()}|");
            logger.Info($"GC.CollectionCount(2)|{GC.CollectionCount(2).ToString()}|");
        }

        static void Learn(LearningParam learningParam)
        {
            EvaluationValue value = new EvaluationValue(learningParam.LearnStepSize);

            if(learningParam.LoadValueFile)
            {
                value.LoadFromCsvFile(learningParam.ValueCsvPath);
            }

            LearningCom com = new LearningCom(value, true, learningParam.ComRandomSeed, learningParam.Epsilon);
            MinesweeperGame game = new MinesweeperGame(
                learningParam.BoardConfig.BoardWidth, 
                learningParam.BoardConfig.BoardHeight, 
                learningParam.BoardConfig.BombCount, 
                learningParam.BoardRandomSeed);
            MinesweeperLearner leaner = new MinesweeperLearner(game, com, value);

            try
            {
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


        static int Solve(SolveParam solveParam)
        {
            cleardCount.Clear();
            EvaluationValue value = new EvaluationValue(0);
            if(solveParam.LoadValueFile)
            {
                value.LoadFromCsvFile(solveParam.ValueCsvPath);
            }

            LearningCom com = new LearningCom(value, false, solveParam.ComRandomSeed, solveParam.Epsilon);
            MinesweeperGame game = new MinesweeperGame(
                solveParam.BoardConfig.BoardWidth, 
                solveParam.BoardConfig.BoardHeight,
                solveParam.BoardConfig.BombCount, 
                solveParam.GameRandomSeed);

            ulong[] boardHashBuf = new ulong[2];
            for(int i = 0; i < solveParam.SolveCount; i++)
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
                        cleardCount.Add(i);
                        break;
                    }
                    else if(result.IsDead)
                    {
                        break;
                    }
                }
            }
            return cleardCount.Count;
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
