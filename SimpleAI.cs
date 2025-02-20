namespace OthelloAI;

/// <summary>
/// シンプルな評価関数を使用するAI
/// </summary>
public class SimpleAI : IAI
{
    private readonly Random _random = new();
    protected readonly int[,] _positionValues = {
        { 30, -12, 0, 0, -12, 30},
        {-12, -15, -3, -3, -15, -12},
        { 0,  -3,  0,  0,  -3,  0},
        { 0,  -3,  0,  0,  -3,  0},
        {-12, -15, -3, -3, -15, -12},
        { 30, -12, 0, 0, -12, 30}
    };

    // virtual に変更
    public virtual string Name => "SimpleAI";

    // virtual に変更
    public virtual (int row, int col) DecideMove(int[,] board, int currentPlayer)
    {
        var gameBoard = new Board(board);
        var validMoves = gameBoard.GetValidMoves(currentPlayer);

        if (validMoves.Count == 0)
            return (-1, -1); // パスを示す

        int maxEval = int.MinValue;
        var bestMoves = new List<(int row, int col)>();

        // 各手の評価値を計算
        foreach (var move in validMoves)
        {
            var nextBoard = new Board(gameBoard);
            nextBoard.PlaceStone(move.row, move.col, currentPlayer);
            var evaluation = EvaluatePosition(nextBoard, currentPlayer);

            if (evaluation > maxEval)
            {
                maxEval = evaluation;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (evaluation == maxEval)
            {
                bestMoves.Add(move);
            }
        }

        return bestMoves[_random.Next(bestMoves.Count)];
    }

    /// <summary>
    /// 盤面の評価値を計算
    /// </summary>
    // virtualに変更
    protected virtual int EvaluatePosition(Board board, int player)
    {
        int score = 0;

        // 位置の価値を評価
        for (int i = 0; i < Board.Size; i++)
        {
            for (int j = 0; j < Board.Size; j++)
            {
                var cell = board.GetCell(i, j);
                if (cell == player)
                    score += _positionValues[i, j];
                else if (cell == -player)
                    score -= _positionValues[i, j];
            }
        }

        // 終局時は実際のスコアを重視
        if (board.IsGameOver())
        {
            var finalScore = board.GetScore() * player;
            score += finalScore * 100;
            return score; // 終局時は移動可能数を評価しない
        }

        // 合法手の数も評価に加える（GetValidMovesの呼び出しを1回に削減）
        var myMoves = board.GetValidMoves(player).Count;
        var oppMoves = board.GetValidMoves(-player).Count;
        score += (myMoves - oppMoves) * 5;

        return score;
    }
}