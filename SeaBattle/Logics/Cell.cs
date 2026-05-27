using System;

namespace NavalBattle.Logics
{
    public enum CellState
    {
        Empty,
        Ship,
        Miss,
        Hit,
        Sunk
    }

    public class Cell
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