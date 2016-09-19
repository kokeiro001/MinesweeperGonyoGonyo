using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minesweeper.Common;
using System.Diagnostics;
using MoreLinq;
using System.Xml.Linq;

namespace Minesweeper.ReinforcementLearningSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            int learnCount = 10000;

            // HACK: 評価関数的な奴。適宜ローカルに保存したりして、それを復元できるようにする
            EvaluationValue value = new EvaluationValue();
            value.Deserialize(XDocument.Load("hoge.xml"));

            List<int> cleardCount = new List<int>();
            MinesweeperLearner leaner = new MinesweeperLearner(value);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for(int i = 0; i < learnCount; i++)
            {
                if(leaner.Learn())
                {
                    cleardCount.Add(i);
                }
            }
            stopwatch.Stop();

            // save
            var xdoc = value.Serialize();
            xdoc.Save("hoge.xml", SaveOptions.None);

            Utils.Output("!!!!!!!!!!!!Clear!!!!!!!!!!!!");
            Utils.Output($"clearCount={cleardCount.Count}");
            Utils.Output(stopwatch.Elapsed.ToString());
            cleardCount.ForEach(cnt => Utils.Output(cnt.ToString("D10")));
        }
    }

    static class MinesweeperExtensions
    {
        public static GameCommand[] ValidCommands(this MinesweeperBoard board)
        {
            return board
                    .Where(c => c.State == CellState.Close)
                    .Select(c => new GameCommand() { Y = c.BoardIndex / 5, X = c.BoardIndex % 5 }) // HACK: hardcorded
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
            Utils.Output(sb.ToString());
        }

    }

    static class Utils
    {
        public static void Output(string text)
        {
            Console.WriteLine(text);
            Debug.WriteLine(text);
        }
    }

    class MinesweeperLearner
    {
        EvaluationValue value;

        public MinesweeperLearner(EvaluationValue value)
        {
            this.value = value;
        }

        public bool Learn()
        {
            bool verbose = false;

            QLearningCom com = new QLearningCom(value, true);
            MinesweeperGame game = new MinesweeperGame(5, 5, 5);
            var board = game.Board;
            while(true)
            {
                var currentAction = com.SelectCommand(board);

                var preActionBoardHash = game.Board.MakeHash();
                var result = game.OpenCell(currentAction.Y * 5 + currentAction.X);

                if(verbose)
                {
                    game.ShowBoardUser();
                }

                if(result.IsClear)
                {
                    com.Learn(preActionBoardHash, currentAction, 1);
                    if(verbose)
                    {
                        Utils.Output("Clear!!!");
                    }
                    return true;
                }
                else if(result.IsDead)
                {
                    com.Learn(preActionBoardHash, currentAction, -1);
                    if(verbose)
                    {
                        Utils.Output("Dead...");
                    }
                    return false;
                }
                else
                {
                    // 状態が変わったセルの数に応じて、報酬を与える
                    if(result.StateChangedCells.Count == 1)
                    {
                        com.Learn(preActionBoardHash, currentAction, 0.05);
                    }
                    else
                    {
                        com.Learn(preActionBoardHash, currentAction, 0.1);
                    }
                }
            }
        }
    }

    class GameCommand
    {
        public int Y;
        public int X;

        public override bool Equals(object obj)
        {
            var tmp = obj as GameCommand;
            if(tmp != null)
            {
                return tmp.X == X && tmp.Y == Y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            // HACK: 
            return Y * 100 + X;
        }
    }

    class EvaluationValue
    {
        static float stepSize = 0.1f;

        // value[boardHash][command] = reward
        Dictionary<string, Dictionary<GameCommand, double>> valueDic = new Dictionary<string, Dictionary<GameCommand, double>>();

        public GameCommand GetMaxCommand(MinesweeperBoard state)
        {
            if(valueDic.Count == 0)
            {
                return null;
            }
            return valueDic
                    .SelectMany(val => val.Value)
                    .OrderBy(innerDic => innerDic.Value)
                    .FirstOrDefault().Key;
        }

        public void Update(string boardHash, GameCommand command, double reward)
        {
            if(!valueDic.ContainsKey(boardHash))
            {
                valueDic.Add(boardHash, new Dictionary<GameCommand, double>());
            }
            if(!valueDic[boardHash].ContainsKey(command))
            {
                valueDic[boardHash].Add(command, 0);
            }
            valueDic[boardHash][command] += stepSize * (reward - valueDic[boardHash][command]);
        }

        public XDocument Serialize()
        {
            XDocument xdoc = new XDocument();
            XElement root = new XElement("root");
            xdoc.Add(root);
            foreach(var pair in valueDic)
            {
                XElement elem = new XElement("boardHash", pair.Key);
                foreach(var item in pair.Value)
                {
                    elem.Add(new XElement($"command",
                        new XAttribute("X", item.Key.X),
                        new XAttribute("Y", item.Key.Y),
                        new XAttribute("value", item.Value)));
                }
                root.Add(elem);
            }
            return xdoc;
        }

        public void Deserialize(XDocument xdoc)
        {
            XElement root = xdoc.Element("root");
            foreach(var boardHashElem in root.Elements("boardHash"))
            {
                var boardHash = boardHashElem.Value;
                valueDic.Add(boardHash, new Dictionary<GameCommand, double>());

                foreach(var commandElem in boardHashElem.Elements("command"))
                {
                    var command = new GameCommand()
                    {
                        X = int.Parse(commandElem.Attribute("X").Value),
                        Y = int.Parse(commandElem.Attribute("Y").Value)
                    };
                    double value = double.Parse(commandElem.Attribute("value").Value);
                    valueDic[boardHash].Add(command, value);
                }
            }
        }
    }

    class QLearningCom
    {
        static Random random = new Random();
        static double epsilon = 0.1;

        EvaluationValue value;
        public bool learning { get; private set; }

        public QLearningCom(EvaluationValue value, bool learning)
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

        public void Learn(string beforeBoardHash, GameCommand command, double reward)
        {
            if(learning)
            {
                value.Update(beforeBoardHash, command, reward);
            }
        }

    }
}
