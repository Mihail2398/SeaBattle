using System;
using System.Threading.Tasks;
using NavalBattle.Logics;
using NavalBattle.Network;

namespace NavalBattle
{
    class Program
    {
        static Board myBoard = new Board();
        static Board enemyView = new Board();
        static NetworkManager net = new NetworkManager();
        static bool myTurn;
        static bool gameOver = false;
        static TaskCompletionSource<GamePacket> packetTask;

        static async Task Main(string[] args)
        {
            myBoard.AutoPlaceShips();
            enemyView.InitializeBoard();

            Console.WriteLine("=== NAVAL BATTLE ===");
            Console.WriteLine("1. Start Server (Host)");
            Console.WriteLine("2. Connect to Server (Client)");
            var choice = Console.ReadLine();

            if (choice == "1")
            {
                Console.WriteLine("Waiting for connection on port 8888...");
                await net.StartServer(8888);
                myTurn = true;
            }
            else
            {
                Console.Write("Enter Server IP (e.g. 127.0.0.1): ");
                string ip = Console.ReadLine();
                await net.ConnectToServer(ip, 8888);
                myTurn = false;
            }

            net.OnPacketReceived += (p) => packetTask?.TrySetResult(p);
            net.OnConnectionLost += () => { Console.WriteLine("\nConnection lost!"); Environment.Exit(0); };

            while (!gameOver)
            {
                DrawBoards();
                if (myTurn)
                {
                    Console.WriteLine("\nYOUR TURN! Enter coordinates X Y (0-9, space separated):");
                    try
                    {
                        var input = Console.ReadLine().Split(' ');
                        int x = int.Parse(input[0]);
                        int y = int.Parse(input[1]);

                        if (enemyView.GetCell(x, y).CurrentState != CellState.Empty)
                        {
                            Console.WriteLine("Already shot there! Try again.");
                            await Task.Delay(1000);
                            continue;
                        }

                        packetTask = new TaskCompletionSource<GamePacket>();
                        await net.SendPacket(new GamePacket { X = x, Y = y });

                        var response = await packetTask.Task;
                        enemyView.GetCell(x, y).CurrentState = response.Result;

                        if (response.Result == CellState.Miss)
                            myTurn = false;

                        if (response.IsGameOver)
                        {
                            DrawBoards();
                            Console.WriteLine("\nVICTORY! ALL ENEMY SHIPS SUNK!");
                            gameOver = true;
                        }
                    }
                    catch { Console.WriteLine("Invalid input!"); await Task.Delay(1000); }
                }
                else
                {
                    Console.WriteLine("\nENEMY TURN. Waiting for shot...");
                    packetTask = new TaskCompletionSource<GamePacket>();
                    var shot = await packetTask.Task;

                    var result = myBoard.Shoot(shot.X, shot.Y);
                    bool lost = myBoard.AllShipsDestroyed;

                    await net.SendPacket(new GamePacket
                    {
                        X = shot.X,
                        Y = shot.Y,
                        Result = result,
                        IsGameOver = lost
                    });

                    if (result == CellState.Miss)
                        myTurn = true;

                    if (lost)
                    {
                        DrawBoards();
                        Console.WriteLine("\nDEFEAT! ALL YOUR SHIPS SUNK!");
                        gameOver = true;
                    }
                }
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void DrawBoards()
        {
            Console.Clear();
            Console.WriteLine("      MY SHIPS                ENEMY BOARD");
            Console.WriteLine("   0 1 2 3 4 5 6 7 8 9      0 1 2 3 4 5 6 7 8 9");
            for (int y = 0; y < 10; y++)
            {
                Console.Write(y + " ");
                for (int x = 0; x < 10; x++) Console.Write(GetChar(myBoard.GetCell(x, y).CurrentState) + " ");
                Console.Write("   " + y + " ");
                for (int x = 0; x < 10; x++) Console.Write(GetChar(enemyView.GetCell(x, y).CurrentState) + " ");
                Console.WriteLine();
            }
        }

        static char GetChar(CellState state) => state switch
        {
            CellState.Empty => '.',
            CellState.Ship => 'S',
            CellState.Miss => 'O',
            CellState.Hit => 'X',
            CellState.Sunk => '#',
            _ => '?'
        };
    }
}