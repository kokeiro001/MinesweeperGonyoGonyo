using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minesweeper.Common;
using System.Diagnostics;
using MoreLinq;
using System.Xml.Linq;
using log4net;
using System.IO;

namespace Minesweeper.ReinforcementLearningSolver
{
    static class LogInitializer
    {
        public static void InitLog(string loggerName)
        {
            var ilogger = LogManager.GetLogger(loggerName);

            var layout = new log4net.Layout.PatternLayout(@"%-5level %date{yyyy/MM/dd_HH:mm:ss,fff} [%thread] %logger - %message%newline");

            var fileAppender = new log4net.Appender.FileAppender()
            {
                Layout = layout,
                File = $"{loggerName}.txt",
                AppendToFile = true,
            };
            var consoleAppender = new log4net.Appender.ConsoleAppender()
            {
                Layout = layout,
            };
            var debugAppender = new log4net.Appender.DebugAppender()
            {
                Layout = layout,
            };

            var logger = ilogger.Logger as log4net.Repository.Hierarchy.Logger;
            logger.Level = log4net.Core.Level.All;
            logger.AddAppender(fileAppender);
            logger.AddAppender(consoleAppender);
            logger.AddAppender(debugAppender);

            logger.Repository.Configured = true;

            fileAppender.ActivateOptions();
            consoleAppender.ActivateOptions();
            debugAppender.ActivateOptions();
        }
    }

    class Program
    {
        static private ILog logger = LogManager.GetLogger("MainLog");

        static private int LearnCount = 1000;

        static void Main(string[] args)
        {
            LogInitializer.InitLog("MainLog");

            EvaluationValue value = new EvaluationValue();
            //value.LoadFromCsvFile("value.csv");
            List<int> cleardCount = new List<int>();

            LearningCom com = new LearningCom(value, true);
            MinesweeperGame game = new MinesweeperGame(5, 5, 5, 0);
            MinesweeperLearner leaner = new MinesweeperLearner(game, com, value);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();


            try
            {
                for(int i = 0; i < LearnCount; i++)
                {
                    if(leaner.Learn())
                    {
                        cleardCount.Add(i);
                    }
                    ProgressView(i);
                }
            }
            catch(Exception e)
            {
                logger.Error("メモリなくなったんじゃないかな(´・ω・`)", e);
            }
            stopwatch.Stop();

            // save
            value.SaveToCsvFile("value.csv");

            logger.Info("| 要素 | 値");
            logger.Info("------------ | -------------");
            logger.Info($"学習回数|{LearnCount}|");
            logger.Info($"ボードサイズ|5x5|");
            logger.Info($"爆弾の数|5|");
            logger.Info($"総クリア回数|{cleardCount.Count}|");
            logger.Info($"学習にかかった時間|{stopwatch.Elapsed.ToString()}|");

            logger.Info($"GC.CollectionCount(0)|{GC.CollectionCount(0).ToString()}|");
            logger.Info($"GC.CollectionCount(1)|{GC.CollectionCount(1).ToString()}|");
            logger.Info($"GC.CollectionCount(2)|{GC.CollectionCount(2).ToString()}|");

            logger.Info("-------------------------");
            cleardCount.ForEach(cnt => logger.Debug(cnt.ToString("D10")));
            logger.Info("-------------------------");
        }

        static int progress = 0;
        static void ProgressView(int currentLeanCount)
        {
            float per = ((float)currentLeanCount / LearnCount) * 100f;
            if(per >= progress)
            {
                progress += 1;
                logger.Debug($"Progress={progress}%");
            }
        }
    }

    static class MinesweeperExtensions
    {
        public static GameCommand[] ValidCommands(this MinesweeperBoard board)
        {
            return board
                    .Where(c => c.State == CellState.Close)
                    .Select(c => new GameCommand(c.BoardIndex / 5, c.BoardIndex % 5, GameCommandType.Open)) // HACK: hardcorded
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


    class MinesweeperLearner
    {
        public static ulong[] boardHashBuf = new ulong[2];
        EvaluationValue value;
        LearningCom com;
        MinesweeperGame game;

        public MinesweeperLearner(MinesweeperGame game, LearningCom com, EvaluationValue value)
        {
            this.value = value;
            this.game = game;
            this.com = com;
        }

        public bool Learn()
        {
            game.ClearBoard();
            game.GenerateRandomBoard();

            while(true)
            {
                var currentAction = com.SelectCommand(game.Board);

                game.Board.MakeHash(boardHashBuf);
                int idx = currentAction.Y * 5 + currentAction.X; // HACK: hardcorded
                var result = game.OpenCell(idx);

                if(result.IsClear)
                {
                    com.Learn(boardHashBuf, currentAction, 1);
                    return true;
                }
                else if(result.IsDead)
                {
                    com.Learn(boardHashBuf, currentAction, -1);
                    return false;
                }
                else
                {
                    // 状態が変わったセルの数に応じて、報酬を与える
                    if(result.StateChangedCells.Count == 1)
                    {
                        com.Learn(boardHashBuf, currentAction, 0.05);
                    }
                    else
                    {
                        com.Learn(boardHashBuf, currentAction, 0.1);
                    }
                }
            }
        }
    }


    class UlongsDictionary<T>
    {
        private readonly int ArraySize = 2;

        Dictionary<ulong, Dictionary<ulong, T>> dictionary = new Dictionary<ulong, Dictionary<ulong, T>>();

        public void Add(ulong[] key, T value)
        {
            Debug.Assert(key.Length == ArraySize);
            Debug.Assert(!(dictionary.ContainsKey(key[0]) && dictionary[key[0]].ContainsKey(key[1])));

            if(!dictionary.ContainsKey(key[0]))
            {
                dictionary.Add(key[0], new Dictionary<ulong, T>());
            }

            dictionary[key[0]].Add(key[1], value);
        }

        public bool ContainsKey(ulong[] key)
        {
            return dictionary.ContainsKey(key[0]) && dictionary[key[0]].ContainsKey(key[1]);
        }

        public T Get(ulong[] key)
        {
            return dictionary[key[0]][key[1]];
        }

        public IEnumerable<ulong[]> CalculateKeys()
        {
            foreach(var item in dictionary)
            {
                foreach(var item2 in item.Value)
                {
                    yield return new ulong[] 
                    {
                        item.Key,
                        item2.Key
                    };
                }
            }
        }
    }

    class EvaluationValue
    {
        static float stepSize = 0.1f;

        UlongsDictionary<Dictionary<GameCommand, double>> valueDic = new UlongsDictionary<Dictionary<GameCommand, double>>();

        static ulong[] boardHashBuf = new ulong[2];

        public GameCommand GetMaxCommand(MinesweeperBoard board)
        {
            board.MakeHash(boardHashBuf);
            if(!valueDic.ContainsKey(boardHashBuf))
            {
                valueDic.Add(boardHashBuf, new Dictionary<GameCommand, double>());
            }

            double maxValue = 0.0;
            GameCommand maxCommand = null;
            foreach(var item in valueDic.Get(boardHashBuf))
            {
                if(item.Value >= maxValue)
                {
                    maxCommand = item.Key;
                    maxValue = item.Value;
                }
            }
            return maxCommand;
        }

        public void Update(ulong[] boardHash, GameCommand command, double reward)
        {
            if(!valueDic.ContainsKey(boardHash))
            {
                valueDic.Add(boardHash, new Dictionary<GameCommand, double>());
            }
            if(!valueDic.Get(boardHashBuf).ContainsKey(command))
            {
                valueDic.Get(boardHashBuf).Add(command, 0);
            }
            valueDic.Get(boardHashBuf)[command] += stepSize * (reward - valueDic.Get(boardHashBuf)[command]);
        }

        public void SaveToCsvFile(string filePath)
        {
            using(StreamWriter csvWriter = new StreamWriter(filePath))
            {
                foreach(var boardHash in valueDic.CalculateKeys())
                {
                    ulong hash0 = boardHash[0];
                    ulong hash1 = boardHash[1];

                    foreach(var commandValuePair in valueDic.Get(boardHash))
                    {
                        GameCommand command = commandValuePair.Key;
                        double value = commandValuePair.Value;
                        csvWriter.WriteLine($"{boardHash[0]},{boardHash[1]},{commandValuePair.Key.X},{commandValuePair.Key.Y},{commandValuePair.Value}");
                    }
                }
            }
        }

        public void LoadFromCsvFile(string filePath)
        {
            using(StreamReader csvReader = new StreamReader(filePath))
            {
                while(!csvReader.EndOfStream)
                {
                    var values = csvReader.ReadLine().Split(',');

                    boardHashBuf[0] = ulong.Parse(values[0]);
                    boardHashBuf[1] = ulong.Parse(values[1]);

                    if(!valueDic.ContainsKey(boardHashBuf))
                    {
                        valueDic.Add(boardHashBuf, new Dictionary<GameCommand, double>());
                    }

                    var command = new GameCommand(
                        int.Parse(values[2]),
                        int.Parse(values[3]),
                        GameCommandType.Open);
                    double value = double.Parse(values[4]);
                    valueDic.Get(boardHashBuf).Add(command, value);
                }
            }
        }
    }

    class LearningCom
    {
        static Random random = new Random(0);
        static double epsilon = 0.1;

        EvaluationValue value;
        public bool learning { get; private set; }

        public LearningCom(EvaluationValue value, bool learning)
        {
            this.value = value;
            this.learning = learning;
        }

        public GameCommand SelectCommand(MinesweeperBoard state)
        {
            var maxCommand = value.GetMaxCommand(state);
            var selectedCommand = maxCommand;

            if(learning)
            {
                // 挙動方策（ε-グリーディ）で行動を決定
                if(selectedCommand == null || random.NextDouble() < epsilon)
                {
                    int randomIndex = random.Next(state.ValidCommands().Length);
                    selectedCommand = state.ValidCommands()[randomIndex];
                }
            }
            return selectedCommand;
        }

        public void Learn(ulong[] beforeBoardHash, GameCommand command, double reward)
        {
            if(learning)
            {
                value.Update(beforeBoardHash, command, reward);
            }
        }

    }
}
