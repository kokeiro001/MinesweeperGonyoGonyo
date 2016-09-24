//#define LOG_LEARN_PROGRESS
using Minesweeper.Common;
using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using log4net;
using System.Diagnostics;

namespace Minesweeper.ReinforcementLearningSolver
{
    class MinesweeperLearner
    {
        static private ILog logger = LogManager.GetLogger("MainLog");

        public EvaluationValue EvaluationValue { get; }

        ulong[] boardHashBuf = new ulong[2];
        MinesweeperCom com;
        MinesweeperGame game;
        LearningParam learningParam;

        public MinesweeperLearner(LearningParam learningParam)
        {
            this.learningParam = learningParam;
            EvaluationValue = learningParam.LoadValueFile ?
                    EvaluationValue.LoadFromCsvFile(learningParam.ValueCsvPath) :
                    new EvaluationValue();

            com = new MinesweeperCom(EvaluationValue, learningParam.ComRandomSeed, learningParam.Epsilon);
            game = new MinesweeperGame(
                learningParam.BoardConfig.BoardWidth,
                learningParam.BoardConfig.BoardHeight,
                learningParam.BoardConfig.BombCount,
                learningParam.BoardRandomSeed);

        }

        public void Learn()
        {
            learningProgress = 0;
            for(int i = 0; i < learningParam.LearnCount; i++)
            {
                game.ClearBoard();
                game.GenerateRandomBoard();
                LearnOneStep();
                LearningProgressView(i, learningParam.LearnCount);
            }
        }

        private bool LearnOneStep()
        {
            while(true)
            {
                var currentAction = com.SelectCommand(game);

                game.Board.MakeHash(boardHashBuf);
                int cellIndex = currentAction.Y * game.Board.Width + currentAction.X;
                var result = game.OpenCell(cellIndex);

                if(result.IsClear)
                {
                    // 状態が変わったセルの数に応じて、報酬を与える
                    if(result.StateChangedCells.Count == 1)
                    {
                        EvaluationValue.Update(boardHashBuf, currentAction, learningParam.RewardOpenOneCell);
                    }
                    else
                    {
                        EvaluationValue.Update(boardHashBuf, currentAction, learningParam.RewardOpenMultiCell);
                    }
                    return true;
                }
                else if(result.IsDead)
                {
                    EvaluationValue.Update(boardHashBuf, currentAction, learningParam.RewardDead);
                    return false;
                }
                else
                {
                    // 状態が変わったセルの数に応じて、報酬を与える
                    if(result.StateChangedCells.Count == 1)
                    {
                        EvaluationValue.Update(boardHashBuf, currentAction, learningParam.RewardOpenOneCell);
                    }
                    else
                    {
                        EvaluationValue.Update(boardHashBuf, currentAction, learningParam.RewardOpenMultiCell);
                    }
                }
            }
        }

        int learningProgress = 0;
        [Conditional("LOG_LEARN_PROGRESS")]
        private void LearningProgressView(int currentLeanCount, int learnCount)
        {
            float per = ((float)currentLeanCount / learnCount) * 100f;
            if(per >= learningProgress)
            {
                learningProgress += 1;
                logger.Debug($"Progress={learningProgress}%, LearnCount={currentLeanCount}");
            }
        }

    }

    class EvaluationValue
    {
        ulong[] boardHashBuf = new ulong[2];

        UlongsDictionary<Dictionary<GameCommand, double>> valueDic = new UlongsDictionary<Dictionary<GameCommand, double>>();

        public EvaluationValue()
        {
        }

        public GameCommand GetMaxCommand(MinesweeperBoard board)
        {
            // HACK: 未知の値があるかどうかを考慮すべき。
            // 現在は、期待値が０以上のコマンドを返却している。
            // すべて探索済みで、全部マイナスの場合、一番軽症で済みそうなやつを返却すべき。
            // 未知の場合は、とりあえず期待値を０にする。

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
            valueDic.Get(boardHashBuf)[command] += (reward - valueDic.Get(boardHashBuf)[command]);
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
                        csvWriter.WriteLine($"{boardHash[0]},{boardHash[1]},{commandValuePair.Key.Y},{commandValuePair.Key.X},{commandValuePair.Value}");
                    }
                }
            }
        }


        public static EvaluationValue LoadFromCsvFile(string filePath)
        {
            var evalutionValue = new EvaluationValue();

            using(StreamReader csvReader = new StreamReader(filePath))
            {
                while(!csvReader.EndOfStream)
                {
                    var values = csvReader.ReadLine().Split(',');

                    evalutionValue.boardHashBuf[0] = ulong.Parse(values[0]);
                    evalutionValue.boardHashBuf[1] = ulong.Parse(values[1]);

                    if(!evalutionValue.valueDic.ContainsKey(evalutionValue.boardHashBuf))
                    {
                        evalutionValue.valueDic.Add(evalutionValue.boardHashBuf, new Dictionary<GameCommand, double>());
                    }

                    var command = new GameCommand(
                        int.Parse(values[2]),
                        int.Parse(values[3]),
                        GameCommandType.Open);
                    double value = double.Parse(values[4]);
                    evalutionValue.valueDic.Get(evalutionValue.boardHashBuf).Add(command, value);
                }
            }
            return evalutionValue;
        }

    }

    class MinesweeperCom
    {
        double selectRandomCommandRate = 0.1;
        Random random;
        EvaluationValue value;

        public MinesweeperCom(EvaluationValue value, int randomSeed, double selectRandomCommandRate)
        {
            this.value = value;
            this.random = new Random(randomSeed);
            this.selectRandomCommandRate = selectRandomCommandRate;
        }

        public GameCommand SelectCommand(MinesweeperGame game)
        {
            var maxCommand = value.GetMaxCommand(game.Board);
            var selectedCommand = maxCommand;

            if(selectedCommand == null || (selectRandomCommandRate >= 0 && random.NextDouble() < selectRandomCommandRate))
            {
                var validCommands = game.ValidCommands();
                int randomIndex = random.Next(validCommands.Length);
                selectedCommand = validCommands[randomIndex];
            }

            return selectedCommand;
        }
    }

    class LearningResult
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string AlgorithmVersion { get; set; }
        public DateTime CreatedAt { get; set; }

        // Board
        public int BoardWidth { get; set; }
        public int BoardHeight { get; set; }
        public int BombCount { get; set; }

        // Learn
        public int LearnCount { get; set; }
        public uint LearnSeconds { get; set; }
        public float LearnEpsilion { get; set; }
        public float RewardOpenOneCell { get; set; }
        public float RewardOpenMultiCell { get; set; }
        public float RewardDead { get; set; }

        // Solve
        public int SolveTrialCount { get; set; }
        public int SolvedCountUnuseLearningData { get; set; }
        public int SolvedCountUseLearningData { get; set; }
    }
}
