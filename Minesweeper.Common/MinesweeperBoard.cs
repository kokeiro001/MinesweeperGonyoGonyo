using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;

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

    public class MinesweeperBoard
    {
        public int Width { get; }
        public int Height { get; }
        public int BombNum { get; }
        public bool IsDead { get; private set; } = false;
        public bool IsClear => !IsDead && board.Count(c => c.State == CellState.Close) == BombNum;

        private List<MinesweeperCell> board = new List<MinesweeperCell>();

        public MinesweeperCell this[int index]
        {
            get { return board[index]; }
        }

        public MinesweeperBoard(int width, int height, int bombNum)
            : this(width, height, bombNum, new Random().Next())
        {
        }
        public MinesweeperBoard(int width, int height, int bombNum, int seed)
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
                board.Add(new MinesweeperCell()
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
                    var cell = board[y * Width + x];
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
                return board[y * Width + x];
            }
            return null;
        }  
        private MinesweeperCell GetCellOrDefault(int index)
        {
            if(0 <= index && index < board.Count)
            {
                return board[index];
            }
            return null;
        }

        private bool EnableIndex(int y, int x) => (0 <= x && x < Width && 0 <= y && y < Height);

        public bool OpenCell(int index)
        {
            var cell = board[index];
            if(cell.State == CellState.Flag)
            {
                return true;
            }
            if(cell.HasBomb)
            {
                Dead();
                return false;
            }

            // 開く
            cell.State = CellState.Open;
            if(cell.Value == 0)
            {
                // 周囲のセルを開ける
                cell.FourLink
                    .Where(c => c?.State == CellState.Close)
                    .ToList()
                    .ForEach(c => OpenCell(c.BoardIndex));
            }
            return true;
        }

        public bool OpenEightCell(int index)
        {
            return GetCellOrDefault(index)
                .EightLink
                .Where(c => c != null)
                .All(c => OpenCell(c.BoardIndex));
        }

        public void ToggleFlag(int index)
        {
            var cell = board[index];
            if(cell.State == CellState.Close)
            {
                cell.State = CellState.Flag;
            }
            else if(cell.State == CellState.Flag)
            {
                cell.State = CellState.Close;
            }
        }

        public void Dead()
        {
            IsDead = true;
            // HACK: 全部開く
            board
                .Where(cell => cell.State == CellState.Close)
                .ForEach(cell => cell.State = CellState.Open);
        }
    }
}
