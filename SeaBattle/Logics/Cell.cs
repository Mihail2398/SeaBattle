using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavalBattle.Logics
{

    enum CellState
    {
        Empty,
        Ship,
        Miss,
        Hit,
        Sunk
    }

    internal class Cell
    {
        public int X { get; }
        public int Y { get; }
        public CellState CurrentState { get; set; }

        public Cell(int x, int y, CellState initialState = CellState.Empty)
        {
            X = x;
            Y = y;
            CurrentState = initialState;
        }

    }
}
