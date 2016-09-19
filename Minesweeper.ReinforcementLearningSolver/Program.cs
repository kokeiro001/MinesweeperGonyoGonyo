using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minesweeper.Common;
using System.Diagnostics;
using MoreLinq;

namespace Minesweeper.ReinforcementLearningSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            int learnCount = 10000000;

            // HACK: 評価関数的な奴。適宜ローカルに保存したりして、それを復元できるようにする
            ValueClass value = new ValueClass();

            List<int> cleardCount = new List<int>();
            MinesweeperLearner leaner = new MinesweeperLearner(value);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for(int i = 0; i < learnCount; i++)
            {
                if(leaner.Learn())
                {
                    cleardCount.Add(i);
                    //Utils.Output($"{i:D10} clear!!!");
                }
                else
                {
                    //Utils.Output($"{i:D10} dead...");
                }
            }
            
            Utils.Output("!!!!!!!!!!!!Clear!!!!!!!!!!!!");
            Utils.Output($"clearCount={cleardCount.Count}");
            stopwatch.Stop();
            Utils.Output(stopwatch.Elapsed.ToString());
            cleardCount.ForEach(cnt => Utils.Output(cnt.ToString()));
        }
    }

    static class MinesweeperExtensions
    {
        public static ActionClass[] ValidActions(this MinesweeperBoard board)
        {
            return board
                    .Where(c => c.State == CellState.Close)
                    .Select(c => new ActionClass() { Y = c.BoardIndex / 5, X = c.BoardIndex % 5 }) // HACK: hardcorded
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
        ValueClass value;

        public MinesweeperLearner(ValueClass value)
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
                var currentAction = com.SelectAction(board);

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
                    com.Learn(preActionBoardHash, currentAction, 0);
                }
            }
        }
    }

    class ActionClass
    {
        public int Y;
        public int X;

        public override bool Equals(object obj)
        {
            var tmp = obj as ActionClass;
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

    class ValueClass
    {
        static float stepSize = 0.1f;

        // value[boardHash][actionHash] = reward
        Dictionary<string, Dictionary<ActionClass, double>> value = new  Dictionary<string, Dictionary<ActionClass, double>>();

        public ActionClass GetMaxAction(MinesweeperBoard state)
        {
            if(value.Count == 0)
            {
                return null;
            }
            return value
                    .SelectMany(val => val.Value)
                    .OrderBy(innerDic => innerDic.Value)
                    .FirstOrDefault().Key;
        }

        public void Update(string boardHash, ActionClass action, double reward)
        {
            if(!value.ContainsKey(boardHash))
            {
                value.Add(boardHash, new Dictionary<ActionClass, double>());
            }
            if(!value[boardHash].ContainsKey(action))
            {
                value[boardHash].Add(action, 0);
            }
            value[boardHash][action] += stepSize * (reward - value[boardHash][action]);
        }
    }

   class QLearningCom {

        static Random random = new Random();
        static double epsilon = 0.1;

        ValueClass value;
        public bool learning { get; private set; }

        public QLearningCom(ValueClass value, bool learning)
        {
            this.value = value;
            this.learning = learning;
        }

        public ActionClass SelectAction(MinesweeperBoard state)
        {
            var maxAction = value.GetMaxAction(state);
            var selectedAction = maxAction;

            if(learning)
            {
                // 挙動方策（ε-グリーディ）で行動を決定
                if(selectedAction == null || random.NextDouble() < epsilon)
                {
                    int randomIndex = random.Next(state.ValidActions().Length);
                    selectedAction = state.ValidActions()[randomIndex];
                }
            }
            return selectedAction;
        }

        public void Learn(string beforeBoardHash, ActionClass action, double reward)
        {
            if(learning)
            {
                value.Update(beforeBoardHash, action, reward);
            }
        }

    }
}
