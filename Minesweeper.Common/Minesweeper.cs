using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Diagnostics;
using System.Collections;

namespace Minesweeper.Common
{
    public enum GameCommandType
    {
        Open,
        OpenEight,
        ToggleFlag
    }

    public class GameCommand
    {
        public GameCommandType Type { get; }
        public int Y { get; }
        public int X { get; }

        public GameCommand(int y, int x, GameCommandType type)
        {
            Y = y;
            X = x;
            Type = type;
        }

        public static GameCommand FromUserInput(string parseText)
        {
            var input = parseText.Split(' ');
            if(input.Length != 3)
            {
                throw new InvalidOperationException();
            }
            GameCommandType type;
            switch(input[0])
            {
                case "open":
                    type = GameCommandType.Open;
                    break;
                case "openEight":
                    type = GameCommandType.OpenEight;
                    break;
                case "flag":
                    type = GameCommandType.ToggleFlag;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            int y = int.Parse(input[1]);
            int x = int.Parse(input[2]);
            return new GameCommand(y, x, type);
        }
    }

    public class Point
    {
        public int X;
        public int Y;

        public Point(int y, int x)
        {
            Y = y;
            X = x;
        }
    }

    public enum CellState
    {
        Close,
        Open,
        Flag
    }

    public class MinesweeperCell
    {
        public int BoardIndex;
        public int Value = 0;
        public CellState State = CellState.Close;
        public bool HasBomb => Value < 0;

        public MinesweeperCell[] EightLink = new MinesweeperCell[8];
        public MinesweeperCell[] FourLink = new MinesweeperCell[4];
    }

    public class BoardOpenResult
    {
        public bool IsClear;
        public bool IsDead;
        public List<MinesweeperCell> StateChangedCells = new List<MinesweeperCell>();
    }

    public class MinesweeperBoard : IEnumerable<MinesweeperCell>
    {
        private List<MinesweeperCell> cellList = new List<MinesweeperCell>();

        public int Width { get; }
        public int Height { get; }

        public MinesweeperCell this[int index]
        {
            get
            {
                return cellList[index];
            }
        }

        public MinesweeperBoard(int width, int height)
        {
            Width = width;
            Height = height;

            for(int i = 0; i < Height * Width; i++)
            {
                cellList.Add(new MinesweeperCell() { BoardIndex = i });
            }
            var eightPoints = new Point[] {
                    new Point (-1, -1), new Point(-1, 0), new Point(-1, 1),
                    new Point ( 0, -1),                   new Point( 0, 1),
                    new Point ( 1, -1), new Point( 1, 0), new Point( 1, 1),
                };

            var fourPoints = new Point[] {
                    new Point(-1, 0), new Point ( 0, -1), new Point( 0, 1), new Point( 1, 0),
                };

            for(int y = 0; y < Height; y++)
            {
                for(int x = 0; x < Width; x++)
                {
                    var cell = cellList[y * Width + x];
                    cell.EightLink = eightPoints
                                        .Select(p => new Point(y + p.Y, x + p.X))
                                        .Select(pos => GetCellOrDefault(pos.Y, pos.X))
                                        .ToArray();
                    cell.FourLink = fourPoints
                                        .Select(p => new Point(y + p.Y, x + p.X))
                                        .Select(pos => GetCellOrDefault(pos.Y, pos.X))
                                        .ToArray();
                }
            }
        }

        private bool EnableIndex(int y, int x) => (0 <= x && x < Width && 0 <= y && y < Height);

        private MinesweeperCell GetCellOrDefault(int y, int x)
        {
            if(EnableIndex(y, x))
            {
                return cellList[y * Width + x];
            }
            return null;
        }
        public MinesweeperCell GetCellOrDefault(int index)
        {
            if(0 <= index && index < cellList.Count())
            {
                return cellList[index];
            }
            return null;
        }

        public IEnumerator<MinesweeperCell> GetEnumerator() => cellList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => cellList.GetEnumerator();

        public void MakeHash(ulong[] hash)
        {
            hash[0] = 0;
            hash[1] = 0;

            int cellIndex = 0;
            int arrayIndex = 0;
            do
            {
                hash[arrayIndex] <<= 4;
                uint tmp = 0;
                if(cellList[cellIndex].State == CellState.Close)
                {
                    tmp = 15;
                }
                else if(cellList[cellIndex].State == CellState.Open)
                {
                    tmp = (uint)cellList[cellIndex].Value;
                }
                hash[arrayIndex] |= tmp;

                cellIndex++;
                if(cellIndex % 16 == 0)
                {
                    arrayIndex++;
                }

            } while(cellIndex < cellList.Count);
        }
    }

    public class MinesweeperGame
    {
        public int Width => Board.Width;
        public int Height => Board.Height;
        public int BombNum { get; }
        public int RandomSeed { get; }
        public bool IsDead { get; private set; } = false;
        public bool IsClear => !IsDead && Board.Count(c => c.State == CellState.Open) == (Width * Height) - BombNum;
        public MinesweeperBoard Board { get; }

        private BoardOpenResult userControllResult = new BoardOpenResult();
        private Random random;

        public MinesweeperCell this[int index]
        {
            get { return Board[index]; }
        }

        public MinesweeperGame(int width, int height, int bombNum)
            : this(width, height, bombNum, new Random().Next())
        {
        }
        public MinesweeperGame(int width, int height, int bombNum, int seed)
        {
            random = new Random(seed);
            Board = new MinesweeperBoard(width, height);
            BombNum = bombNum;
            RandomSeed = seed;
        }

        private void CreateBoard(int bombNum, int seed)
        {
            int totalCellNum = Width * Height;

            var bombIndexes = Enumerable.Range(0, totalCellNum).Shuffle(seed).Take(bombNum);

            for(int i = 0; i < totalCellNum; i++)
            {
                Board[i].State = CellState.Close;
                Board[i].Value = bombIndexes.Contains(i) ? -1 : 0;
            }

            for(int y = 0; y < Height; y++)
            {
                for(int x = 0; x < Width; x++)
                {
                    var cell = Board[y * Width + x];

                    if(!cell.HasBomb)
                    {
                        cell.Value = cell.EightLink.Count(c => c?.HasBomb == true);
                    }
                }
            }
        }


        public BoardOpenResult OpenCell(int index)
        {
            Debug.Assert(Board[index].State == CellState.Close);
            OpenCellSub(index, 0);
            userControllResult.IsClear = IsClear;
            userControllResult.IsDead = IsDead;
            return userControllResult;
        }

        private void OpenCellSub(int index, int depth)
        {
            if(depth == 0)
            {
                userControllResult = new BoardOpenResult();
            }

            var cell = Board[index];
            if(cell.State != CellState.Close)
            {
                return;
            }
            if(cell.HasBomb)
            {
                Dead();
            }

            // 開く
            cell.State = CellState.Open;
            userControllResult.StateChangedCells.Add(cell);
            if(cell.Value == 0)
            {
                // 周囲のセルを開ける
                cell.EightLink
                    .Where(c => c?.State == CellState.Close)
                    .ToList()
                    .ForEach(c => OpenCellSub(c.BoardIndex, depth + 1));
            }
        }

        public BoardOpenResult OpenEightCell(int index)
        {
            userControllResult = new BoardOpenResult();
            Board.GetCellOrDefault(index)
                .EightLink
                .Where(c => c != null)
                .ForEach(c => OpenCellSub(c.BoardIndex, 1));
            userControllResult.IsClear = IsClear;
            userControllResult.IsDead = IsDead;
            return userControllResult;
        }

        public BoardOpenResult ToggleFlag(int index)
        {
            userControllResult = new BoardOpenResult();
            var cell = Board[index];
            if(cell.State == CellState.Close)
            {
                cell.State = CellState.Flag;
            }
            else if(cell.State == CellState.Flag)
            {
                cell.State = CellState.Close;
            }
            userControllResult.StateChangedCells.Add(cell);
            return userControllResult;
        }

        public GameCommand[] ValidCommands()
        {
            // HACK: 毎回ゴミ生成しちゃうんで、適当なUpdateとかで配列に持たせたりなんだりする
            return Board
                    .Where(c => c.State == CellState.Close)
                    .Select(c => new GameCommand(c.BoardIndex / Width, c.BoardIndex % Width, GameCommandType.Open))
                    .ToArray();
        }

        public void Dead()
        {
            IsDead = true;
            Board
                .Where(cell => cell.State == CellState.Close)
                .ForEach(cell => cell.State = CellState.Open);
        }

        public void ClearBoard()
        {
            IsDead = false;
        }

        public void GenerateRandomBoard()
        {
            CreateBoard(BombNum, random.Next());
        }

    }
}
