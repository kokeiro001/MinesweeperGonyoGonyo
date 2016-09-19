using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoreLinq;
using System.Diagnostics;
using System.Collections;

namespace Minesweeper.Common
{
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

        public MinesweeperCell this[int index]
        {
            get
            {
                return cellList[index];
            }
        }

        public IEnumerator<MinesweeperCell> GetEnumerator() => cellList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => cellList.GetEnumerator();

        public void Add(MinesweeperCell cell) => cellList.Add(cell);

        public string MakeHash()
        {
            StringBuilder sb = new StringBuilder();

            Func<MinesweeperCell, string> cell2Hash = c => {
                if(c.State == CellState.Open)
                {
                    return c.Value.ToString();
                }
                else
                {
                    return "?";
                }
            };

            cellList.ForEach(c => sb.Append(cell2Hash(c)));
            return sb.ToString();
        }
    }

    public class MinesweeperGame
    {
        public int Width { get; }
        public int Height { get; }
        public int BombNum { get; }
        public bool IsDead { get; private set; } = false;
        public bool IsClear => !IsDead && Board.Count(c => c.State == CellState.Open) == (Width * Height) - BombNum;

        private BoardOpenResult userControllResult = new BoardOpenResult();

        public MinesweeperBoard Board { get; } = new MinesweeperBoard();

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
            Width = width;
            Height = height;
            BombNum = bombNum;

            CreateBoard(bombNum, seed);
        }

        private void CreateBoard(int bombNum, int seed)
        {
            int totalCellNum = Width * Height;

            var bombIndexes = Enumerable.Range(0, totalCellNum).Shuffle(seed).Take(bombNum);

            for(int i = 0; i < totalCellNum; i++)
            {
                Board.Add(new MinesweeperCell()
                {
                    BoardIndex = i,
                    Value = bombIndexes.Contains(i) ? -1 : 0
                });
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
                    var cell = Board[y * Width + x];
                    cell.EightLink = eightPoints
                                        .Select(p => new Point(y + p.Y, x + p.X))
                                        .Select(pos => GetCellOrDefault(pos.Y, pos.X))
                                        .ToArray();
                    cell.FourLink = fourPoints
                                        .Select(p => new Point(y + p.Y, x + p.X))
                                        .Select(pos => GetCellOrDefault(pos.Y, pos.X))
                                        .ToArray();
                    if(!cell.HasBomb)
                    {
                        cell.Value = cell.EightLink.Count(c => c?.HasBomb == true);
                    }
                }
            }

        }

        private MinesweeperCell GetCellOrDefault(int y, int x)
        {
            if(EnableIndex(y, x))
            {
                return Board[y * Width + x];
            }
            return null;
        }
        private MinesweeperCell GetCellOrDefault(int index)
        {
            if(0 <= index && index < Board.Count())
            {
                return Board[index];
            }
            return null;
        }

        private bool EnableIndex(int y, int x) => (0 <= x && x < Width && 0 <= y && y < Height);

        public BoardOpenResult OpenCell(int index)
        {
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
            GetCellOrDefault(index)
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

        public void Dead()
        {
            IsDead = true;
            // HACK: 全部開く
            Board
                .Where(cell => cell.State == CellState.Close)
                .ForEach(cell => cell.State = CellState.Open);
        }
    }
}
