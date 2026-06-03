using System;
using System.Collections.Generic;
using System.Linq;
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

        private List<int> _shipsToPlace;
        private Direction _currentDirection = Direction.Horizontal;
        private bool _isPlacementPhase = false;

        private int _lastHoveredC = -1;
        private int _lastHoveredR = -1;

        private string _serverIp;
        private int _serverPort;

        public MainWindow()
        {
            InitializeComponent();
            ConnectionMenu.Visibility = Visibility.Visible;
            GameScreen.Visibility = Visibility.Collapsed;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _isHost = RbHost.IsChecked == true;
            _serverIp = TxtIp.Text;
            _serverPort = int.TryParse(TxtPort.Text, out int p) ? p : 8888;

            ConnectionMenu.Visibility = Visibility.Collapsed;
            GameScreen.Visibility = Visibility.Visible;

            StartPlacementPhase();
        }

        private void StartPlacementPhase()
        {
            _myBoard = new Board();
            _enemyBoard = new Board();
            _enemyBoard.InitializeBoard();

            _myScore = 0;
            _enemyScore = 0;
            TxtMyScore.Text = "0";
            TxtEnemyScore.Text = "0";

            _shipsToPlace = new List<int> { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
            _isPlacementPhase = true;
            _currentDirection = Direction.Horizontal;

            UpdatePlacementText();

            RenderBoard(MyGrid, _myBoard, isEnemy: false);
            RenderBoard(EnemyGrid, _enemyBoard, isEnemy: true);
        }

        private void UpdatePlacementText()
        {
            if (_shipsToPlace.Count > 0)
            {
                string dir = _currentDirection == Direction.Horizontal ? "√ÓËÁ." : "¬ÂÚ.";
                TxtTurn.Text = $" Œ–¿¡À‹: {_shipsToPlace[0]} œ¿À”¡\n({dir})\n[œ Ã - œŒ¬Œ–Œ“]";
            }
        }

        private void RenderBoard(UniformGrid grid, Board board, bool isEnemy)
        {
            grid.Children.Clear();
            for (int r = 0; r < board.Row; r++)
            {
                for (int c = 0; c < board.Column; c++)
                {
                    Cell cell = board.GetCell(c, r);
                    if (cell == null) continue;

                    Border cellVisual = new Border
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0xA1, 0xC2)),
                        BorderThickness = new Thickness(0.5),
                        Tag = cell
                    };

                    Grid.SetColumn(cellVisual, c);
                    Grid.SetRow(cellVisual, r);

                    cellVisual.Background = cell.CurrentState switch
                    {
                        CellState.Empty => Brushes.Transparent,
                        CellState.Ship => isEnemy ? Brushes.Transparent : Brushes.Gray,
                        CellState.Miss => new SolidColorBrush(Color.FromRgb(0x1A, 0x4A, 0x62)),
                        CellState.Hit => Brushes.Orange,
                        CellState.Sunk => Brushes.Red,
                        _ => Brushes.Transparent
                    };

                    if (isEnemy)
                    {
                        cellVisual.Cursor = Cursors.Hand;
                        cellVisual.MouseLeftButtonDown += EnemyCell_Click;
                    }
                    else
                    {
                        cellVisual.MouseLeftButtonDown += MyCell_Click;
                        cellVisual.MouseRightButtonDown += MyCell_RightClick;
                        cellVisual.MouseEnter += MyCell_MouseEnter;
                        cellVisual.MouseLeave += MyCell_MouseLeave;
                    }

                    grid.Children.Add(cellVisual);
                }
            }
        }

        private void MyCell_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!_isPlacementPhase) return;
            Border border = sender as Border;
            if (border == null) return;

            _lastHoveredC = Grid.GetColumn(border);
            _lastHoveredR = Grid.GetRow(border);
            DrawPreview();
        }

        private void MyCell_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isPlacementPhase) return;
            _lastHoveredC = -1;
            _lastHoveredR = -1;
            ClearPreview();
        }

        private void MyCell_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isPlacementPhase) return;
            _currentDirection = _currentDirection == Direction.Horizontal ? Direction.Vertical : Direction.Horizontal;
            UpdatePlacementText();
            DrawPreview();
        }

        private void ClearPreview()
        {
            if (_myBoard == null) return;
            foreach (UIElement child in MyGrid.Children)
            {
                if (child is Border border && border.Tag is Cell cell)
                {
                    border.Background = cell.CurrentState switch
                    {
                        CellState.Empty => Brushes.Transparent,
                        CellState.Ship => Brushes.Gray,
                        CellState.Miss => new SolidColorBrush(Color.FromRgb(0x1A, 0x4A, 0x62)),
                        CellState.Hit => Brushes.Orange,
                        CellState.Sunk => Brushes.Red,
                        _ => Brushes.Transparent
                    };
                }
            }
        }

        private void DrawPreview()
        {
            ClearPreview();
            if (!_isPlacementPhase || _shipsToPlace == null || _shipsToPlace.Count == 0 || _lastHoveredC == -1 || _lastHoveredR == -1) return;

            int length = _shipsToPlace[0];
            bool isValid = true;
            List<(int c, int r)> points = new List<(int, int)>();

            for (int i = 0; i < length; i++)
            {
                int tc = _currentDirection == Direction.Horizontal ? _lastHoveredC + i : _lastHoveredC;
                int tr = _currentDirection == Direction.Vertical ? _lastHoveredR + i : _lastHoveredR;

                if (tc < 0 || tc >= _myBoard.Column || tr < 0 || tr >= _myBoard.Row)
                {
                    isValid = false;
                    continue;
                }

                points.Add((tc, tr));

                for (int nc = tc - 1; nc <= tc + 1; nc++)
                {
                    for (int nr = tr - 1; nr <= tr + 1; nr++)
                    {
                        Cell neighbor = _myBoard.GetCell(nc, nr);
                        if (neighbor != null && neighbor.CurrentState != CellState.Empty)
                        {
                            isValid = false;
                        }
                    }
                }
            }

            Brush previewBrush = isValid
                ? new SolidColorBrush(Color.FromArgb(100, 46, 204, 113))
                : new SolidColorBrush(Color.FromArgb(100, 231, 76, 60));

            foreach (UIElement child in MyGrid.Children)
            {
                if (child is Border border)
                {
                    int bc = Grid.GetColumn(border);
                    int br = Grid.GetRow(border);

                    if (points.Contains((bc, br)))
                    {
                        border.Background = previewBrush;
                    }
                }
            }
        }

        private async void MyCell_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_isPlacementPhase || _shipsToPlace.Count == 0) return;

            Border border = sender as Border;
            if (border == null) return;

            int c = Grid.GetColumn(border);
            int r = Grid.GetRow(border);
            int currentShipLength = _shipsToPlace[0];

            if (_myBoard.PlaceShip(c, r, currentShipLength, _currentDirection))
            {
                _shipsToPlace.RemoveAt(0);
                RenderBoard(MyGrid, _myBoard, isEnemy: false);

                if (_shipsToPlace.Count == 0)
                {
                    _isPlacementPhase = false;
                    _lastHoveredC = -1;
                    _lastHoveredR = -1;
                    await InitializeNetworkConnection();
                }
                else
                {
                    UpdatePlacementText();
                    DrawPreview();
                }
            }
        }

        private async Task InitializeNetworkConnection()
        {
            TxtTurn.Text = "—≈“‹...";

            _networkManager = new NetworkManager();
            _networkManager.OnPacketReceived += NetworkManager_OnPacketReceived;
            _networkManager.OnConnectionLost += NetworkManager_OnConnectionLost;

            try
            {
                if (_isHost)
                {
                    _isMyTurn = true;
                    TxtTurn.Text = "∆ƒ≈Ã »√–Œ ¿";
                    await _networkManager.StartServer(_serverPort);
                    TxtTurn.Text = "¬¿ÿ ’Œƒ";
                }
                else
                {
                    _isMyTurn = false;
                    TxtTurn.Text = "œŒƒ Àﬁ◊≈Õ»≈";
                    await _networkManager.ConnectToServer(_serverIp, _serverPort);
                    TxtTurn.Text = "’Œƒ ¬–¿√¿";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Œ¯Ë·Í‡ ÒÂÚË: {ex.Message}");
                ResetToMainMenu();
            }
        }

        private async void EnemyCell_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_isMyTurn || _isPlacementPhase || _networkManager == null) return;

            Border border = sender as Border;
            if (border == null) return;

            int c = Grid.GetColumn(border);
            int r = Grid.GetRow(border);

            Cell cell = _enemyBoard.GetCell(c, r);
            if (cell == null || cell.CurrentState != CellState.Empty) return;

            GamePacket packet = new GamePacket { X = c, Y = r };
            await _networkManager.SendPacket(packet);

            _isMyTurn = false;
            TxtTurn.Text = "’Œƒ ¬–¿√¿";
        }

        private void NetworkManager_OnPacketReceived(GamePacket packet)
        {
            Dispatcher.Invoke(async () =>
            {
                if (packet.Result == CellState.Empty)
                {
                    CellState shotResult = _myBoard.Shoot(packet.X, packet.Y);

                    if (shotResult == CellState.Sunk)
                    {
                        Cell hitCell = _myBoard.GetCell(packet.X, packet.Y);
                        if (hitCell != null)
                        {
                            List<Cell> shipCells = new List<Cell>();
                            Queue<Cell> queue = new Queue<Cell>();
                            HashSet<Cell> visited = new HashSet<Cell>();

                            queue.Enqueue(hitCell);
                            visited.Add(hitCell);

                            while (queue.Count > 0)
                            {
                                Cell current = queue.Dequeue();
                                shipCells.Add(current);

                                int[] dc = { -1, 1, 0, 0 };
                                int[] dr = { 0, 0, -1, 1 };

                                for (int i = 0; i < 4; i++)
                                {
                                    int nc = current.X + dc[i];
                                    int nr = current.Y + dr[i];

                                    Cell neighbor = _myBoard.GetCell(nc, nr);
                                    if (neighbor != null && !visited.Contains(neighbor) && neighbor.CurrentState == CellState.Sunk)
                                    {
                                        visited.Add(neighbor);
                                        queue.Enqueue(neighbor);
                                    }
                                }
                            }

                            foreach (Cell shipCell in shipCells)
                            {
                                for (int nc = shipCell.X - 1; nc <= shipCell.X + 1; nc++)
                                {
                                    for (int nr = shipCell.Y - 1; nr <= shipCell.Y + 1; nr++)
                                    {
                                        Cell neighbor = _myBoard.GetCell(nc, nr);
                                        if (neighbor != null && neighbor.CurrentState == CellState.Empty)
                                        {
                                            neighbor.CurrentState = CellState.Miss;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    RenderBoard(MyGrid, _myBoard, isEnemy: false);

                    bool lost = _myBoard.AllShipsDestroyed;
                    GamePacket resultPacket = new GamePacket { X = packet.X, Y = packet.Y, Result = shotResult, IsGameOver = lost };
                    await _networkManager.SendPacket(resultPacket);

                    if (lost)
                    {
                        EndGame("¬˚ ÔÓË„‡ÎË! ¬ÒÂ ‚‡¯Ë ÍÓ‡·ÎË ÛÌË˜ÚÓÊÂÌ˚.");
                        return;
                    }

                    if (shotResult == CellState.Miss)
                    {
                        _isMyTurn = true;
                        TxtTurn.Text = "¬¿ÿ ’Œƒ";
                    }
                }
                else
                {
                    Cell cell = _enemyBoard.GetCell(packet.X, packet.Y);
                    if (cell != null)
                    {
                        cell.CurrentState = packet.Result;

                        if (packet.Result == CellState.Sunk)
                        {
                            List<Cell> shipCells = new List<Cell>();
                            Queue<Cell> queue = new Queue<Cell>();
                            HashSet<Cell> visited = new HashSet<Cell>();

                            queue.Enqueue(cell);
                            visited.Add(cell);

                            while (queue.Count > 0)
                            {
                                Cell current = queue.Dequeue();
                                shipCells.Add(current);

                                int[] dc = { -1, 1, 0, 0 };
                                int[] dr = { 0, 0, -1, 1 };

                                for (int i = 0; i < 4; i++)
                                {
                                    int nc = current.X + dc[i];
                                    int nr = current.Y + dr[i];

                                    Cell neighbor = _enemyBoard.GetCell(nc, nr);
                                    if (neighbor != null && !visited.Contains(neighbor))
                                    {
                                        if (neighbor.CurrentState == CellState.Hit || neighbor.CurrentState == CellState.Sunk)
                                        {
                                            visited.Add(neighbor);
                                            queue.Enqueue(neighbor);
                                        }
                                    }
                                }
                            }

                            foreach (Cell shipCell in shipCells)
                            {
                                shipCell.CurrentState = CellState.Sunk;
                            }

                            foreach (Cell shipCell in shipCells)
                            {
                                for (int nc = shipCell.X - 1; nc <= shipCell.X + 1; nc++)
                                {
                                    for (int nr = shipCell.Y - 1; nr <= shipCell.Y + 1; nr++)
                                    {
                                        Cell neighbor = _enemyBoard.GetCell(nc, nr);
                                        if (neighbor != null && neighbor.CurrentState == CellState.Empty)
                                        {
                                            neighbor.CurrentState = CellState.Miss;
                                        }
                                    }
                                }
                            }
                        }

                        RenderBoard(EnemyGrid, _enemyBoard, isEnemy: true);
                    }

                    if (packet.IsGameOver)
                    {
                        EndGame("¬˚ ÔÓ·Â‰ËÎË! ¬ÒÂ ÍÓ‡·ÎË ‚‡„‡ ÛÌË˜ÚÓÊÂÌ˚.");
                        return;
                    }

                    if (packet.Result == CellState.Hit || packet.Result == CellState.Sunk)
                    {
                        _myScore++;
                        _isMyTurn = true;
                        TxtTurn.Text = "¬¿ÿ ’Œƒ";
                    }
                    else if (packet.Result == CellState.Miss)
                    {
                        _isMyTurn = false;
                        TxtTurn.Text = "’Œƒ ¬–¿√¿";
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
                EndGame("—ÓÂ‰ËÌÂÌËÂ Ò ÔÓÚË‚ÌËÍÓÏ ‡ÁÓ‚‡ÌÓ.");
            });
        }

        private void EndGame(string message)
        {
            MessageBox.Show(message);
            if (_networkManager != null)
            {
                _networkManager.OnConnectionLost -= NetworkManager_OnConnectionLost;
                _networkManager.OnPacketReceived -= NetworkManager_OnPacketReceived;
                _networkManager.Stop();
            }
            ResetToMainMenu();
        }

        private void ResetToMainMenu()
        {
            _isPlacementPhase = false;
            _isMyTurn = false;
            GameScreen.Visibility = Visibility.Collapsed;
            ConnectionMenu.Visibility = Visibility.Visible;
        }
    }
}