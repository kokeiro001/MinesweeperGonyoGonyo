using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using Minesweeper.Common;
using UniRx;
using UniRx.Triggers;

public class AppManager : MonoBehaviour
{
    private static readonly int BoardWidth = 10;
    private static readonly int BoardHeight = 10;
    private static readonly int BoardSeed = 10;
    private static readonly int BoardBombNum = 10;

    private readonly MinesweeperGame board;

    private GameObject cellPrefab;
    private GameObject boardRowPrefab;

    private List<Button> cellButtonList = new List<Button>();

    public AppManager()
    {
        board = new MinesweeperGame(BoardWidth, BoardHeight, BoardBombNum, BoardSeed);
    }

    private void Awake()
    {
        cellPrefab = Resources.Load<GameObject>("Prefabs/CellButton");
        boardRowPrefab = Resources.Load<GameObject>("Prefabs/BoardRow");
    }

    private void Start()
    {
        var columnsObj = GameObject.Find("BoardColumns");

        for(int y = 0; y < BoardHeight; y++)
        {
            var rowObj = Instantiate(boardRowPrefab);
            rowObj.transform.SetParent(columnsObj.transform);

            for(int x = 0; x < BoardWidth; x++)
            {
                var cellObj = Instantiate(cellPrefab);
                cellObj.transform.SetParent(rowObj.transform);

                var button = cellObj.GetComponent<Button>();
                cellButtonList.Add(button);

                //var tmp = (object)(y * BoardWidth + x);
                var cell = board[y * BoardWidth + x];
                button.OnClickAsObservable().Subscribe(_ => OnClicked(cell));
           }
        }
    }

    private void OnClicked(MinesweeperCell cell)
    {
        //var idx = (int)index;
        //Debug.Log("y=" + y + " x=" + x);
        //Debug.Log("idx=" + idx);
        BoardOpenResult res = null;
        if(Input.GetKey(KeyCode.Space))
        {
            res = board.ToggleFlag(cell.BoardIndex);
        }
        else
        {
            res = board.OpenCell(cell.BoardIndex);
        }

        if(res.IsDead)
        {
            Debug.LogError("dead");
        }
        else if(res.IsClear)
        {
            Debug.LogError("clear");
        }
        else
        {
            res.StateChangedCells.ForEach(c => {
                switch(c.State)
                {
                    case CellState.Open:
                        cellButtonList[c.BoardIndex].enabled = false;
                        cellButtonList[c.BoardIndex].GetComponentInChildren<Text>().text = c.Value.ToString();
                        break;
                    case CellState.Flag:
                        cellButtonList[c.BoardIndex].GetComponentInChildren<Text>().text = "旗";
                        break;
                    case CellState.Close:
                        cellButtonList[c.BoardIndex].GetComponentInChildren<Text>().text = "button";
                        break;
                    default:
                        break;
                }
            });
        }
    }

}
