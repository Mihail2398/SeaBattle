using System;
using NavalBattle.Logics;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== ЗАПУСК UNIT-ТЕСТОВ ЯДРА ===\n");

        TestBoundaries();
        TestShipSinking();
        TestPlacementOverlap();

        Console.WriteLine("\n=== ТЕСТИРОВАНИЕ ЗАВЕРШЕНО ===");
        Console.ReadKey();
    }

    static void TestBoundaries()
    {
        Board board = new Board(10, 10);
        try
        {
            var result = board.Shoot(99, 99); 
            Console.WriteLine("[OK] Тест границ: Выстрел вне поля обработан корректно.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAIL] Тест границ: Система упала! {ex.Message}");
        }
    }

    static void TestShipSinking()
    {
        Board board = new Board(10, 10);
        board.PlaceShip(0, 0, 1, Direction.Horizontal);

        board.Shoot(0, 0); 

        var cell = board.GetCell(0, 0);
        if (cell.CurrentState == CellState.Sunk && board.AllShipsDestroyed)
        {
            Console.WriteLine("[OK] Тест потопления: Корабль уничтожен, статус 'Победа' активен.");
        }
        else
        {
            Console.WriteLine("[FAIL] Тест потопления: Корабль не сменил статус на Sunk.");
        }
    }

    
    static void TestPlacementOverlap()
    {
        Board board = new Board(10, 10);
        board.PlaceShip(5, 5, 2, Direction.Horizontal); 

        bool result = board.PlaceShip(5, 5, 1, Direction.Vertical);


        bool resultAdjacent = board.PlaceShip(6, 6, 1, Direction.Horizontal);

        if (!result && !resultAdjacent)
        {
            Console.WriteLine("[OK] Тест коллизий: Система запрещает ставить корабли вплотную.");
        }
        else
        {
            Console.WriteLine("[FAIL] Тест коллизий: Корабли слиплись или перекрылись.");
        }
    }
}
