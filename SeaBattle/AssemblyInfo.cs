using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
                    await _networkManager.ConnectToServer(TxtIp.Text, port);
                }

                ConnectionMenu.Visibility = Visibility.Collapsed;
                GameScreen.Visibility = Visibility.Visible;
                StartGame();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сети: {ex.Message}");
                BtnStart.IsEnabled = true;
                BtnStart.Content = "Кнопка начать";
            }
        }


        private void StartGame()
        {
            _myBoard = new Board(10, 10);
            _enemyBoard = new Board(10, 10); 

            _myBoard.AutoPlaceShips(); 

            RenderBoard(MyGrid, _myBoard, isEnemy: false);
            RenderBoard(EnemyGrid, _enemyBoard, isEnemy: true);

            UpdateTurnUI();
        }

        private void RenderBoard(UniformGrid grid, Board board, bool isEnemy)
        {
            grid.Children.Clear();

            for (int r = 0; r < board.Row; r++)
            {
                for (int c = 0; c < board.Column; c++)
                {
                    Cell cellInfo = board.GetCell(c, r);

                    Border cellUI = new Border
                    {
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3AA1C2")),
                        BorderThickness = new Thickness(0.5),
                        Background = GetCellColor(cellInfo.CurrentState, isEnemy)
                    };

                    if (isEnemy)
                    {
                        cellUI.Cursor = Cursors.Hand;
                        cellUI.Tag = new Point(c, r); 
                        cellUI.MouseLeftButtonDown += EnemyCell_Click;
                    }

                    grid.Children.Add(cellUI);
                }
            }
        }

        private Brush GetCellColor(CellState state, bool hideShips)
        {
            switch (state)
            {
                case CellState.Ship:
                    return hideShips ? Brushes.Transparent : Brushes.LightGray; 
                case CellState.Miss:
                    return Brushes.DarkCyan; 
                case CellState.Hit:
                case CellState.Sunk:
                    return Brushes.IndianRed; 
                default:
                    return Brushes.Transparent;
            }
        }

        private void UpdateTurnUI()
        {
            if (_isMyTurn)
            {
                TurnIndicatorPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D7151"));
                TxtTurn.Text = "ТВОЙ ХОД";
            }
            else
            {
                TurnIndicatorPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A44040"));
                TxtTurn.Text = "ХОД ПРОТИВНИКА";
            }
            TxtMyScore.Text = _myScore.ToString();
            TxtEnemyScore.Text = _enemyScore.ToString();
        }


        private async void EnemyCell_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_isMyTurn) return; 

            Border clickedCell = sender as Border;
            Point coords = (Point)clickedCell.Tag;
            int x = (int)coords.X;
            int y = (int)coords.Y;

            if (_enemyBoard.GetCell(x, y).CurrentState != CellState.Empty) return;

            _isMyTurn = false;
            UpdateTurnUI();

            GamePacket attackPacket = new GamePacket { X = x, Y = y, Result = CellState.Empty };
            await _networkManager.SendPacket(attackPacket);
        }


        private void NetworkManager_OnPacketReceived(GamePacket packet)
        {
            Dispatcher.Invoke(async () =>
            {
                if (packet.Result == CellState.Empty)
                {
                    CellState shotResult = _myBoard.Shoot(packet.X, packet.Y);
                    RenderBoard(MyGrid, _myBoard, isEnemy: false);

                    if (shotResult == CellState.Hit || shotResult == CellState.Sunk)
                        _enemyScore++; 

                    if (_myBoard.AllShipsDestroyed)
                    {
                        MessageBox.Show("Вы проиграли! Все ваши корабли уничтожены.");
                        Close();
                        return;
                    }

                    GamePacket resultPacket = new GamePacket { X = packet.X, Y = packet.Y, Result = shotResult };
                    await _networkManager.SendPacket(resultPacket);

                    if (shotResult == CellState.Miss)
                    {
                        _isMyTurn = true;
                    }
                    UpdateTurnUI();
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

                    UpdateTurnUI();
                }
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