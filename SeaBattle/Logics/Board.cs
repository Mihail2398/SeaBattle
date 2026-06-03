using System;
using System.Collections.Generic;
using System.Linq;

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
            cells = new Cell[Column, Row];
            ships.Clear();
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
                int c = startCol + (direction == Direction.Horizontal ? i : 0);
                int r = startRow + (direction == Direction.Vertical ? i : 0);

                if (c < 0 || c >= Column || r < 0 || r >= Row) return false;

                for (int dc = -1; dc <= 1; dc++)
                {
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        Cell neighbor = GetCell(c + dc, r + dr);
                        if (neighbor != null && neighbor.CurrentState == CellState.Ship)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool PlaceShip(int startCol, int startRow, int length, Direction direction)
        {
            if (!CanPlaceShip(startCol, startRow, length, direction)) return false;

            List<Cell> shipCells = new List<Cell>();
            for (int i = 0; i < length; i++)
            {
                int c = startCol + (direction == Direction.Horizontal ? i : 0);
                int r = startRow + (direction == Direction.Vertical ? i : 0);
                Cell cell = GetCell(c, r);
                shipCells.Add(cell);
            }

            Ship ship = new Ship(shipCells);
            ships.Add(ship);
            return true;
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

        private void CheckShipSunk(Cell cell)
        {
            var ship = ships.FirstOrDefault(s => s.OccupiedCells.Contains(cell));
            if (ship != null && ship.IsSunk)
            {
                ship.Sink();
                MarkAroundSunk(ship);
            }
        }

        private void MarkAroundSunk(Ship ship)
        {
            foreach (var cell in ship.OccupiedCells)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        Cell neighbor = GetCell(cell.X + dc, cell.Y + dr);
                        if (neighbor != null && neighbor.CurrentState == CellState.Empty)
                        {
                            neighbor.CurrentState = CellState.Miss;
                        }
                    }
                }
            }
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
                    if (PlaceShip(col, row, length, dir))
                    {
                        placed = true;
                    }
                }
            }
        }
    }
}