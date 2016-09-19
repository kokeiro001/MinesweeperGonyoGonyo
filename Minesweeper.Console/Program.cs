using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minesweeper.Common;
using System.Diagnostics;
using Microsoft.VisualBasic;

namespace Minesweeper.ConsoleApp
{
    class Program
    {
        static MinesweeperGame board = new MinesweeperGame(10, 10, 10, 2);

        static void Main(string[] args)
        {

            while(!board.IsDead && !board.IsClear)
            {
                ShowBoardRaw();
                ShowBoardUser();

                GameCommand inputCommand;
                try
                {
                    inputCommand = GameCommand.FromUserInput(Console.ReadLine());
                }
                catch(Exception e)
                {
                    Output(e.Message);
                    continue;
                }
                switch(inputCommand.Type)
                {
                    case GameCommandType.Open:
                        if(board.OpenCell(Position2BoardIndex(inputCommand.Y, inputCommand.X)).IsDead)
                        {
                            Output("DEAD!!!!!!!!!!!!!");
                        }
                        break;
                    case GameCommandType.OpenEight:
                        if(!board.OpenEightCell(Position2BoardIndex(inputCommand.Y, inputCommand.X)).IsDead)
                        {
                            Output("DEAD!!!!!!!!!!!!!");
                        }
                        break;
                    case GameCommandType.ToggleFlag:
                        board.ToggleFlag(Position2BoardIndex(inputCommand.Y, inputCommand.X));
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                if(board.IsClear)
                {
                    Output("Clear!!!");
                }

            }

            ShowBoardRaw();
            ShowBoardUser();
            Console.ReadLine();
        }

        static int Position2BoardIndex(int y, int x) => y * board.Width + x;

        static void ShowBoardRaw()
        {
            StringBuilder sb = new StringBuilder();
            for(int y = 0; y < board.Height; y++)
            {
                for(int x = 0; x < board.Width; x++)
                {
                    var cell = board[Position2BoardIndex(y, x)];
                    if(cell.HasBomb)
                    {
                        sb.Append("*");
                    }
                    else
                    {
                        sb.Append(cell.Value);
                    }
                }
                sb.AppendLine();
            }
            Output(sb.ToString());
        }

        static void ShowBoardUser()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" |" + string.Join("", Enumerable.Range(0, board.Width).Select(num => num.ToString())));
            sb.AppendLine(" +" + string.Join("", Enumerable.Range(0, board.Width).Select(_ => "-")));
            for(int y = 0; y < board.Height; y++)
            {
                sb.Append($"{y}|");
                for(int x = 0; x < board.Width; x++)
                {
                    var cell = board[Position2BoardIndex(y, x)];
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
            Output(sb.ToString());
        }

        static void Output(string text)
        {
            Console.WriteLine(text);
            Debug.WriteLine(text);
        }
    }
}
