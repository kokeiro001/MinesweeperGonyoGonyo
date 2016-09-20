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

        static void Main(string[] args)
        {
            LogInitializer.InitLog("MainLog");

            Stopwatch learnStopwatch = new Stopwatch();
            learnStopwatch.Start();
            Learn();
            learnStopwatch.Stop();

            int clearCount = Solve();

            logger.Info("| 要素 | 値");
            logger.Info("------------ | -------------");
            logger.Info($"学習回数|{LearningParam.LearnCount}|");
            logger.Info($"ボードサイズ|{Config.BoardWidth}x{Config.BoardHeight}|");
            logger.Info($"爆弾の数|{Config.BombCount}|");
            logger.Info($"性能確認回数回数|{SolveParam.SolveCount}|");
            logger.Info($"クリア回数|{cleardCount.Count}|");
            logger.Info($"クリア率|{cleardCount.Count / (double)SolveParam.SolveCount}|");
            logger.Info($"学習にかかった時間|{learnStopwatch.Elapsed.ToString()}|");

            logger.Info($"GC.CollectionCount(0)|{GC.CollectionCount(0).ToString()}|");
            logger.Info($"GC.CollectionCount(1)|{GC.CollectionCount(1).ToString()}|");
            logger.Info($"GC.CollectionCount(2)|{GC.CollectionCount(2).ToString()}|");

            logger.Debug("-------------------------");
            //cleardCount.ForEach(cnt => logger.Debug(cnt.ToString("D10")));
        }

        static void Learn()
        {
            EvaluationValue value = new EvaluationValue();

            if(LearningParam.LoadValueFile)
            {
                value.LoadFromCsvFile(LearningParam.ValueCsvPath);
            }

            LearningCom com = new LearningCom(value, true, LearningParam.ComRandomSeed);
            MinesweeperGame game = new MinesweeperGame(Config.BoardWidth, Config.BoardHeight, Config.BombCount, LearningParam.BoardRandomSeed);
            MinesweeperLearner leaner = new MinesweeperLearner(game, com, value);

            try
            {
                for(int i = 0; i < LearningParam.LearnCount; i++)
                {
                    leaner.Learn();
                    LearningProgressView(i);
                }
            }
            catch(Exception e)
            {
                logger.Error("メモリなくなったんじゃないかな(´・ω・`)", e);
            }

            // save
            if(LearningParam.SaveValueFile)
            {
                value.SaveToCsvFile(LearningParam.ValueCsvPath);
            }
        }

        static int learningProgress = 0;
        static void LearningProgressView(int currentLeanCount)
        {
            float per = ((float)currentLeanCount / LearningParam.LearnCount) * 100f;
            if(per >= learningProgress)
            {
                learningProgress += 1;
                logger.Debug($"Progress={learningProgress}%, LearnCount={currentLeanCount}");
            }
        }


        static int Solve()
        {
            EvaluationValue value = new EvaluationValue();
            if(SolveParam.LoadValueFile)
            {
                value.LoadFromCsvFile(SolveParam.ValueCsvPath);
            }

            LearningCom com = new LearningCom(value, false, SolveParam.ComRandomSeed);
            MinesweeperGame game = new MinesweeperGame(Config.BoardWidth, Config.BoardHeight, Config.BombCount, SolveParam.GameRandomSeed);

            ulong[] boardHashBuf = new ulong[2];
            for(int i = 0; i < SolveParam.SolveCount; i++)
            {
                game.ClearBoard();
                game.GenerateRandomBoard();

                while(true)
                {
                    var currentAction = com.SelectCommand(game.Board);

                    game.Board.MakeHash(boardHashBuf);
                    int idx = currentAction.Y * Config.BoardWidth + currentAction.X;
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
                    .Select(c => new GameCommand(c.BoardIndex / Config.BoardWidth, c.BoardIndex % Config.BoardWidth, GameCommandType.Open))
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
