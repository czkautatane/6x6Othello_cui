namespace OthelloAI;

/// <summary>
/// 6x6オセロの盤面を管理するクラス
/// </summary>
public class Board
{
    /// <summary>
    /// 盤面のサイズ（6x6）
    /// </summary>
    public const int Size = 6;

    /// <summary>
    /// 盤面の状態（0: 空, 1: 黒, -1: 白）
    /// </summary>
    private readonly int[,] _board;

    /// <summary>
    /// 各方向のベクトル（上から時計回り）
    /// </summary>
    private static readonly (int dr, int dc)[] Directions = new[]
    {
        (-1, 0), (-1, 1), (0, 1), (1, 1),
        (1, 0), (1, -1), (0, -1), (-1, -1)
    };

    /// <summary>
    /// コンストラクタ - 初期配置で盤面を初期化
    /// </summary>
    public Board()
    {
        _board = new int[Size, Size];
        InitializeBoard();
    }

    /// <summary>
    /// コピーコンストラクタ
    /// </summary>
    public Board(Board other)
    {
        _board = new int[Size, Size];
        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
                _board[i, j] = other._board[i, j];
    }

    /// <summary>
    /// 盤面を直接設定するコンストラクタ
    /// </summary>
    public Board(int[,] board)
    {
        _board = new int[Size, Size];
        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
                _board[i, j] = board[i, j];
    }

    /// <summary>
    /// 盤面の初期化
    /// </summary>
    private void InitializeBoard()
    {
        // 中央に初期配置（6x6の場合、(2,2), (2,3), (3,2), (3,3)に配置）
        _board[2, 2] = -1;
        _board[2, 3] = 1;
        _board[3, 2] = 1;
        _board[3, 3] = -1;
    }

    /// <summary>
    /// 盤面の状態を取得
    /// </summary>
    public int[,] GetBoard()
    {
        // 参照を直接返す - 呼び出し側で必要な場合のみコピーを作成する
        return _board;
    }

    /// <summary>
    /// 盤面の状態のコピーを取得
    /// </summary>
    public int[,] GetBoardCopy()
    {
        var copy = new int[Size, Size];
        Array.Copy(_board, copy, Size * Size);
        return copy;
    }

    /// <summary>
    /// 指定位置に石を置けるかチェック
    /// </summary>
    public bool IsValidMove(int row, int col, int player)
    {
        // 範囲外または既に石がある場合は無効
        if (!IsInBoard(row, col) || _board[row, col] != 0)
            return false;

        // 8方向のいずれかで石が裏返せる場合は有効
        return Directions.Any(dir => CanFlip(row, col, player, dir.dr, dir.dc));
    }

    /// <summary>
    /// 指定位置に石を置く
    /// </summary>
    public bool PlaceStone(int row, int col, int player)
    {
        if (!IsValidMove(row, col, player))
            return false;

        _board[row, col] = player;
        foreach (var dir in Directions)
        {
            if (CanFlip(row, col, player, dir.dr, dir.dc))
                FlipStones(row, col, dir.dr, dir.dc);
        }
        return true;
    }

    /// <summary>
    /// プレイヤーの合法手をすべて取得
    /// </summary>
    public List<(int row, int col)> GetValidMoves(int player)
    {
        var moves = new List<(int row, int col)>();
        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
                if (IsValidMove(i, j, player))
                    moves.Add((i, j));
        return moves;
    }

    /// <summary>
    /// ゲームが終了したかチェック
    /// </summary>
    public bool IsGameOver()
    {
        return GetValidMoves(1).Count == 0 && GetValidMoves(-1).Count == 0;
    }

    /// <summary>
    /// 勝者を取得（1: 黒の勝ち, -1: 白の勝ち, 0: 引き分け）
    /// </summary>
    public int GetWinner()
    {
        var score = GetScore();
        if (score > 0) return 1;
        if (score < 0) return -1;
        return 0;
    }

    /// <summary>
    /// スコアを取得（正: 黒が優勢, 負: 白が優勢）
    /// </summary>
    public int GetScore()
    {
        return _board.Cast<int>().Sum();
    }

    /// <summary>
    /// 特定の位置の状態を取得
    /// </summary>
    public int GetCell(int row, int col)
    {
        return _board[row, col];
    }

    // 以下、内部ヘルパーメソッド

    private bool IsInBoard(int row, int col)
    {
        return row >= 0 && row < Size && col >= 0 && col < Size;
    }

    private bool FlipStonesInDirection(int row, int col, int player, int dr, int dc, bool actuallyFlip = true)
    {
        int r = row + dr;
        int c = col + dc;
        bool hasOpponent = false;
        var stonesToFlip = new List<(int r, int c)>();

        while (IsInBoard(r, c) && _board[r, c] == -player)
        {
            hasOpponent = true;
            stonesToFlip.Add((r, c));
            r += dr;
            c += dc;
        }

        bool canFlip = hasOpponent && IsInBoard(r, c) && _board[r, c] == player;

        if (canFlip && actuallyFlip)
        {
            foreach (var (flipR, flipC) in stonesToFlip)
            {
                _board[flipR, flipC] = player;
            }
        }

        return canFlip;
    }

    private bool CanFlip(int row, int col, int player, int dr, int dc)
    {
        return FlipStonesInDirection(row, col, player, dr, dc, false);
    }

    private void FlipStones(int row, int col, int dr, int dc)
    {
        FlipStonesInDirection(row, col, _board[row, col], dr, dc);
    }
}
