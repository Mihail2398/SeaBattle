using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using NavalBattle.Logics;
using NavalBattle.Network;

namespace NavalBattle
{
    public partial class MainWindow : Window
    {
        private NetworkManager _networkManager;
        private Board _myBoard;
        private Board _enemyBoard;
        private bool _isMyTurn;
        private bool _isHost;
        private int _myScore = 0;
        private int _enemyScore = 0;

        public MainWindow()
        {
            InitializeComponent();
            _myBoard = new Board();
            _enemyBoard = new Board();
            _myBoard.AutoPlaceShips();
            _enemyBoard.InitializeBoard();
            RenderBoard(MyGrid, _myBoard, isEnemy: false);
            RenderBoard(EnemyGrid, _enemyBoard, isEnemy: true);
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            BtnStart.IsEnabled = false;
            BtnStart.Content = "Подключение...";

            _networkManager = new NetworkManager();
            _networkManager.OnPacketReceived += NetworkManager_OnPacketReceived;
            _networkManager.OnConnectionLost += NetworkManager_OnConnectionLost;

            int port = int.TryParse(TxtPort.Text, out int p) ? p : 8888;
            _isHost = RbHost.IsChecked == true;

            try
            {
                if (_isHost)
                {
                    _isMyTurn = true;
                    await _networkManager.StartServer(port); 
                }
                else
                {
                    _isMyTurn = false;
                    string ip = TxtIp.Text.Trim();

                    if (string.IsNullOrEmpty(ip))
                    {
                        MessageBox.Show("Введите IP-адрес.");
                        ResetConnectButton();
                        return;
                    }

                    
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        var connectTask = _networkManager.ConnectToServer(ip, port);
                        var delayTask = Task.Delay(-1, cts.Token);

                        if (await Task.WhenAny(connectTask, delayTask) == connectTask)
                        {
                            cts.Cancel();
                            await connectTask;
                        }
                        else
                        {
                            throw new TimeoutException("Время ожидания истекло. Проверьте IP.");
                        }
                    }
                }

                ConnectionMenu.Visibility = Visibility.Collapsed;
                GameScreen.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сети: {ex.Message}");
                ResetConnectButton();
            }
        }

        private void ResetConnectButton()
        {
            BtnStart.IsEnabled = true;
            BtnStart.Content = "Кнопка начать";
        }

        private void RenderBoard(UniformGrid grid, Board board, bool isEnemy)
        {
            grid.Children.Clear();
            for (int r = 0; r < board.Row; r++)
            {
                for (int c = 0; c < board.Column; c++)
                {
                    Cell cell = board.GetCell(c, r);
                    Border cellVisual = new Border
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(58, 161, 194)),
                        BorderThickness = new Thickness(0.5),
                        Tag = cell
                    };

                    cellVisual.Background = cell.CurrentState switch
                    {
                        CellState.Empty => Brushes.Transparent,
                        CellState.Ship => isEnemy ? Brushes.Transparent : Brushes.LightGray,
                        CellState.Miss => Brushes.DarkCyan,
                        CellState.Hit => Brushes.IndianRed,
                        CellState.Sunk => Brushes.Red,
                        _ => Brushes.Transparent
                    };

                    if (isEnemy)
                    {
                        cellVisual.Cursor = Cursors.Hand;
                        cellVisual.MouseLeftButtonDown += EnemyCell_Click;
                    }

                    grid.Children.Add(cellVisual);
                }
            }
        }

        private async void EnemyCell_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_isMyTurn) return;

            var border = sender as Border;
            var cell = border?.Tag as Cell;
            if (cell == null) return;

            if (cell.CurrentState != CellState.Empty) return;

            _isMyTurn = false;

            GamePacket attackPacket = new GamePacket { X = cell.X, Y = cell.Y, Result = CellState.Empty };
            await _networkManager.SendPacket(attackPacket);
        }

        private void NetworkManager_OnPacketReceived(GamePacket packet)
        {
            Dispatcher.Invoke(async () =>
            {
                if (packet.IsGameOver)
                {
                    MessageBox.Show("Вы победили! Все корабли врага уничтожены.");
                    Close();
                    return;
                }

                if (packet.Result == CellState.Empty)
                {
                    CellState shotResult = _myBoard.Shoot(packet.X, packet.Y);
                    RenderBoard(MyGrid, _myBoard, isEnemy: false);

                    if (shotResult == CellState.Hit || shotResult == CellState.Sunk)
                        _enemyScore++;

                    bool lost = _myBoard.AllShipsDestroyed;

                    GamePacket resultPacket = new GamePacket { X = packet.X, Y = packet.Y, Result = shotResult, IsGameOver = lost };
                    await _networkManager.SendPacket(resultPacket);

                    if (lost)
                    {
                        MessageBox.Show("Вы проиграли! Все ваши корабли уничтожены.");
                        Close();
                        return;
                    }

                    if (shotResult == CellState.Miss) _isMyTurn = true;
                }
                else
                {
                    _enemyBoard.GetCell(packet.X, packet.Y).CurrentState = packet.Result;
                    RenderBoard(EnemyGrid, _enemyBoard, isEnemy: true);

                    if (packet.Result == CellState.Hit || packet.Result == CellState.Sunk)
                    {
                        _myScore++;
                        _isMyTurn = true;
                    }
                    else if (packet.Result == CellState.Miss)
                    {
                        _isMyTurn = false;
                    }
                }

                TxtMyScore.Text = _myScore.ToString();
                TxtEnemyScore.Text = _enemyScore.ToString();
            });
        }

        private void NetworkManager_OnConnectionLost()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Соединение с противником разорвано.");
                Close();
            });
        }
    }
}