using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavalBattle.Logics
{
    enum Direction {
        Horizontal, 
        Vertical 
    }

    internal class Ship
    {
        public List<Cell> OccupiedCells { get; } = new List<Cell>();

        public bool IsSunk
        {
            get
            {
                return OccupiedCells.All(c => c.CurrentState == CellState.Hit || c.CurrentState == CellState.Sunk);
            }
        }

        public Ship(List<Cell> cells)
        {
            OccupiedCells = cells;

            foreach (var cell in OccupiedCells)
            {
                cell.CurrentState = CellState.Ship;
            }
        }

        public void Sink()
        {
            foreach (var cell in OccupiedCells)
            {
                cell.CurrentState = CellState.Sunk;
            }
        }


    }
}


