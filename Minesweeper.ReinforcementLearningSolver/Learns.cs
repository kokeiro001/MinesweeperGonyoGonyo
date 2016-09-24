using Minesweeper.Common;
using System;
using System.Collections.Generic;
using System.IO;
using SQLite;

namespace Minesweeper.ReinforcementLearningSolver
{
    class MinesweeperLearner
    {
        public ulong[] boardHashBuf = new ulong[2];
        EvaluationValue value;
        MinesweeperCom com;
        MinesweeperGame game;
        LearningParam learningParam;

        public MinesweeperLearner(MinesweeperGame game, MinesweeperCom com, EvaluationValue value, LearningParam learningParam)
        {
            this.value = value;
            this.game = game;
            this.com = com;
            this.learningParam = learningParam;
        }

        public bool Learn()
        {
            game.ClearBoard();
            game.GenerateRandomBoard();

            while(true)
            {
                var currentAction = com.SelectCommand(game);

                game.Board.MakeHash(boardHashBuf);
                int idx = currentAction.Y * game.Board.Width + currentAction.X;
                var result = game.OpenCell(idx);

                if(result.IsClear)
                {
                    // 状態が変わったセルの数に応じて、報酬を与える
                    if(result.StateChangedCells.Count == 1)
                    {
                        value.Update(boardHashBuf, currentAction, learningParam.RewardOpenOneCell);
                    }
                    else
                    {
                        value.Update(boardHashBuf, currentAction, learningParam.RewardOpenMultiCell);
                    }
                    return true;
                }
                else if(result.IsDead)
                {
                    value.Update(boardHashBuf, currentAction, learningParam.RewardDead);
                    return false;
                }
                else
                {
                    // 状態が変わったセルの数に応じて、報酬を与える
                    if(result.StateChangedCells.Count == 1)
                    {
                        value.Update(boardHashBuf, currentAction, learningParam.RewardOpenOneCell);
                    }
                    else
                    {
                        value.Update(boardHashBuf, currentAction, learningParam.RewardOpenMultiCell);
                    }
                }
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

        public void LoadFromCsvFile(string filePath)
        {
            valueDic = new UlongsDictionary<Dictionary<GameCommand, double>>();

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

    class MinesweeperCom
    {
        double epsilon = 0.1;
        Random random;
        EvaluationValue value;
        public bool learning { get; private set; }

        public MinesweeperCom(EvaluationValue value, bool learning, int randomSeed, double epsilon)
        {
            this.value = value;
            this.learning = learning;
            this.random = new Random(randomSeed);
            this.epsilon = epsilon;
        }

        public GameCommand SelectCommand(MinesweeperGame game)
        {
            var maxCommand = value.GetMaxCommand(game.Board);
            var selectedCommand = maxCommand;

            // 挙動方策（ε-グリーディ）で行動を決定
            if(selectedCommand == null || (learning && random.NextDouble() < epsilon))
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
