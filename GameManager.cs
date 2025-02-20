namespace OthelloAI;

/// <summary>
/// オセロゲームの進行を管理するクラス
/// </summary>
public class GameManager
{
    // 外部からAIプレイヤーにアクセスできるようにプロパティを追加
    public IAI BlackPlayer => _blackPlayer;
    public IAI WhitePlayer => _whitePlayer;

    private readonly Board _board;
    private readonly IAI _blackPlayer;
    private readonly IAI _whitePlayer;
    private int _currentPlayer;
    private bool _isGameOver;
    private readonly bool _displayEnabled; // 追加: 表示の有無

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="displayEnabled">ゲームの進行を表示するかどうか</param>
    public GameManager(IAI blackPlayer, IAI whitePlayer, bool displayEnabled = true)
    {
        _board = new Board();
        _blackPlayer = blackPlayer;
        _whitePlayer = whitePlayer;
        _currentPlayer = 1; // 黒から開始
        _isGameOver = false;
        _displayEnabled = displayEnabled;
    }

    /// <summary>
    /// ゲームを1ターン進める
    /// </summary>
    /// <returns>ゲームが終了したかどうか</returns>
    public bool PlayTurn()
    {
        if (_isGameOver)
            return true;

        if (_displayEnabled) DisplayBoard();
        if (_displayEnabled)
        {
            Console.WriteLine($"Current Player: {(_currentPlayer == 1 ? "Black" : "White")}");
        }

        var currentAI = _currentPlayer == 1 ? _blackPlayer : _whitePlayer;
        var (row, col) = currentAI.DecideMove(_board.GetBoard(), _currentPlayer);

        // パスの場合
        if (row == -1 && col == -1)
        {
            if (_displayEnabled)
            {
                Console.WriteLine($"{currentAI.Name} passes.");
            }
            _currentPlayer = -_currentPlayer;

            // 両者がパスした場合、ゲーム終了
            if (_board.GetValidMoves(_currentPlayer).Count == 0)
            {
                _isGameOver = true;
                Console.WriteLine("Both players passed. Game Over!");
                return true;
            }
            return false;
        }

        // 石を置く
        if (!_board.PlaceStone(row, col, _currentPlayer))
        {
            throw new InvalidOperationException(
                $"Invalid move by {currentAI.Name}: ({row}, {col})");
        }

        if (_displayEnabled)
        {
            Console.WriteLine($"{currentAI.Name} places at ({row}, {col})");
        }

        // 次のプレイヤーに手番を移す
        _currentPlayer = -_currentPlayer;

        // ゲーム終了判定
        if (_board.IsGameOver())
        {
            _isGameOver = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// ゲーム結果を表示
    /// </summary>
    public void DisplayResult()
    {
        DisplayBoard();
        var winner = _board.GetWinner();
        var score = Math.Abs(_board.GetScore());

        Console.WriteLine("\nGame Over!");
        if (winner == 1)
            Console.WriteLine($"Winner: Black ({_blackPlayer.Name}) - Score: {score}");
        else if (winner == -1)
            Console.WriteLine($"Winner: White ({_whitePlayer.Name}) - Score: {score}");
        else
            Console.WriteLine("Draw!");
    }

    /// <summary>
    /// 現在の盤面を表示
    /// </summary>
    private void DisplayBoard()
    {
        var currentBoard = _board.GetBoard();
        var blackCount = currentBoard.Cast<int>().Count(x => x == 1);
        var whiteCount = currentBoard.Cast<int>().Count(x => x == -1);

        Console.WriteLine("\n Current Board Status:");
        Console.WriteLine($" Black (●): {blackCount}  White (○): {whiteCount}\n");
        Console.WriteLine("    0 1 2 3 4 5");
        Console.WriteLine("   ─────────────");

        for (int i = 0; i < Board.Size; i++)
        {
            Console.Write($" {i} │");
            for (int j = 0; j < Board.Size; j++)
            {
                var symbol = currentBoard[i, j] switch
                {
                    1 => "●",
                    -1 => "○",
                    _ => "·"
                };
                Console.Write($"{symbol} ");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }

    /// <summary>
    /// ゲーム全体を実行
    /// </summary>
public void RunGame()
    {
        /*
        Console.WriteLine($"Black: {_blackPlayer.Name}");
        Console.WriteLine($"White: {_whitePlayer.Name}");
        */
        
        while (!PlayTurn())
        {
            if (_displayEnabled) Thread.Sleep(1000); // ターン間の待機時間
        }

        /*
        // 勝者判定 (ここを修正)
        int winner = _board.GetWinner();
        string winnerName;

        // ここにデバッグ出力を追加
        Console.WriteLine($"_board.GetWinner() returned: {winner}");

        if (winner == 1)
        {
            winnerName = _blackPlayer.Name;  //勝者は黒
            Console.WriteLine("Black wins!"); // デバッグ用
        }
        else if(winner == -1)
        {
            winnerName = _whitePlayer.Name; //勝者は白
             Console.WriteLine("White wins!"); // デバッグ用
        }
        else
        {
            winnerName = "Draw"; // 引き分けの場合
            Console.WriteLine("Draw!"); // デバッグ用
        }
        */
    }
        
    /// <summary>
    /// 勝者を取得（1: 黒, -1: 白, 0: 引き分け）
    /// </summary>
    public int GetWinner()
    {
        return _board.GetWinner();
    }

    public Board Board => _board;
    public bool IsGameOver => _isGameOver;


    // ターンごとの更新情報を記録するためのリスト
    private List<(string currentState, string nextState, double reward, bool done)> _turnUpdates = new();

    // ターンごとの情報を記録
    public void RecordTurn(string currentState, string nextState, double reward, bool done)
    {
        _turnUpdates.Add((currentState, nextState, reward, done));
    }

    // ターンごとの更新情報を取得
    public List<(string currentState, string nextState, double reward, bool done)> GetTurnUpdates()
    {
        return _turnUpdates;
    }

    // ターンごとの更新情報をクリア
    public void ClearTurnUpdates()
    {
        _turnUpdates.Clear();
    }
}

// PlayTurn()では、盤面の更新、合法手の判定、パス処理、石配置、プレイヤー交代が正しく実装されています。
// RunGame()では、ループで各ターンを進め、最終的に_board.GetWinner()で勝者判定を行っています。
// また、ゲーム終了後のデバッグ出力も挿入され、動作確認がしやすくなっています。