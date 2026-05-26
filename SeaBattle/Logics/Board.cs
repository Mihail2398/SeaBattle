using System;
using NavalBattle.Logics;

namespace NavalBattle.Logics
{
    internal class Board
    {
        public int Column { get; }
        public int Row { get; }
        private Cell[,] cells;
        private List<Ship> ships;
        public bool AllShipsDestroyed => ships.Count > 0 && ships.All(s => s.IsSunk);


        public Board(int column = 10, int row = 10)
        {
            Column = column;
            Row = row;
            cells = new Cell[column, row]; 
            ships = new List<Ship>();

            InitializeBoard(); 
        }

        public void InitializeBoard()
        {
            for (int c = 0; c < Column; c++)
            {
                for (int r = 0; r < Row; r++)
                {
                    cells[c, r] = new Cell(c, r);
                }
            }
        }

        public Cell GetCell(int c, int r)
        {
            if (c < 0 || c >= Column || r < 0 || r >= Row) return null;
            return cells[c, r];
        }

        private bool CanPlaceShip(int startCol, int startRow, int length, Direction direction)
        {
            for (int i = 0; i < length; i++)
            {
                int c = direction == Direction.Horizontal ? startCol + i : startCol;
                int r = direction == Direction.Vertical ? startRow + i : startRow;

                if (c < 0 || c >= Column || r < 0 || r >= Row) return false;

                if (!IsAreaEmpty(c, r)) return false;
            }
            return true;
        }

        private bool IsAreaEmpty(int col, int row)
        {
            for (int c = col - 1; c <= col + 1; c++)
            {
                for (int r = row - 1; r <= row + 1; r++)
                {
                    if (c < 0 || c >= Column || r < 0 || r >= Row) continue;

                    if (cells[c, r].CurrentState != CellState.Empty) return false;
                }
            }
            return true;
        }


        public bool PlaceShip(int startCol, int startRow, int length, Direction direction)
        {
            if (!CanPlaceShip(startCol, startRow, length, direction))
                return false;

            List<Cell> shipCells = new List<Cell>();
            for (int i = 0; i < length; i++)
            {
                int c = direction == Direction.Horizontal ? startCol + i : startCol;
                int r = direction == Direction.Vertical ? startRow + i : startRow;
                shipCells.Add(cells[c, r]);
            }

            ships.Add(new Ship(shipCells));

            return true;
        }

        private void CheckShipSunk(Cell hitCell)
        {
            var ship = ships.FirstOrDefault(s => s.OccupiedCells.Contains(hitCell));

            if (ship != null && ship.IsSunk)
            {
                ship.Sink(); 
                Console.WriteLine("Корабль потоплен!");
            }
        }


        public CellState Shoot(int c, int r)
        {
            Cell cell = GetCell(c, r);
            if (cell == null) return CellState.Empty;

            if (cell.CurrentState == CellState.Miss ||
                cell.CurrentState == CellState.Hit ||
                cell.CurrentState == CellState.Sunk)
            {
                return cell.CurrentState;
            }

            if (cell.CurrentState == CellState.Empty)
            {
                cell.CurrentState = CellState.Miss;
                return CellState.Miss;
            }

            if (cell.CurrentState == CellState.Ship)
            {
                cell.CurrentState = CellState.Hit;

                CheckShipSunk(cell);

                return cell.CurrentState;
            }

            return cell.CurrentState;
        }

        public void AutoPlaceShips()
        {
            Random random = new Random();
            int[] shipLengths = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

            foreach (int length in shipLengths)
            {
                bool placed = false;
                while (!placed)
                {
                    int col = random.Next(0, Column);
                    int row = random.Next(0, Row);
                    Direction dir = (Direction)random.Next(0, 2);

                    placed = PlaceShip(col, row, length, dir);
                }
            }
        }



    }
}
