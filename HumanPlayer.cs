namespace OthelloAI;

/// <summary>
/// 人間プレイヤーの実装
/// </summary>
public class HumanPlayer : IAI
{
    public string Name => "Human Player";

    /// <summary>
    /// プレイヤーからの入力を受け取り、次の手を決定する
    /// </summary>
    public (int row, int col) DecideMove(int[,] board, int currentPlayer)
    {
        var validMoves = GetValidMoves(board, currentPlayer);
        if (validMoves.Count == 0)
            return (-1, -1); // パス

        // 有効な手を表示
        Console.WriteLine("\nValid moves:");
        foreach (var move in validMoves)
        {
            Console.WriteLine($"({move.row}, {move.col})");
        }

        while (true)
        {
            try
            {
                Console.Write("\nEnter your move (row col), or -1 -1 to pass: ");
                var input = Console.ReadLine()?.Trim().Split(' ');
                if (input == null || input.Length != 2)
                {
                    Console.WriteLine("Invalid input format. Please enter two numbers separated by space.");
                    continue;
                }

                var row = int.Parse(input[0]);
                var col = int.Parse(input[1]);

                // パスの確認
                if (row == -1 && col == -1)
                {
                    if (validMoves.Count > 0)
                    {
                        Console.WriteLine("You cannot pass when there are valid moves available.");
                        continue;
                    }
                    return (row, col);
                }

                // 入力値の検証
                if (row < 0 || row >= Board.Size || col < 0 || col >= Board.Size)
                {
                    Console.WriteLine($"Invalid position. Row and column must be between 0 and {Board.Size - 1}.");
                    continue;
                }

                // 有効な手かどうかの確認
                if (!validMoves.Any(m => m.row == row && m.col == col))
                {
                    Console.WriteLine("Invalid move. Please choose from the valid moves listed above.");
                    continue;
                }

                return (row, col);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid input format. Please enter numbers only.");
            }
        }
    }

    /// <summary>
    /// 有効な手の一覧を取得
    /// </summary>
    private List<(int row, int col)> GetValidMoves(int[,] board, int currentPlayer)
    {
        var moves = new List<(int row, int col)>();
        for (int i = 0; i < Board.Size; i++)
        {
            for (int j = 0; j < Board.Size; j++)
            {
                if (IsValidMove(board, i, j, currentPlayer))
                {
                    moves.Add((i, j));
                }
            }
        }
        return moves;
    }

    /// <summary>
    /// 指定された位置に石を置けるかどうかを判定
    /// </summary>
    private bool IsValidMove(int[,] board, int row, int col, int currentPlayer)
    {
        if (board[row, col] != 0)
            return false;

        var directions = new[]
        {
            (-1, -1), (-1, 0), (-1, 1),
            (0, -1),           (0, 1),
            (1, -1),  (1, 0),  (1, 1)
        };

        foreach (var (dr, dc) in directions)
        {
            var r = row + dr;
            var c = col + dc;
            var foundOpponent = false;

            while (r >= 0 && r < Board.Size && c >= 0 && c < Board.Size)
            {
                if (board[r, c] == 0)
                    break;
                if (board[r, c] == currentPlayer)
                {
                    if (foundOpponent)
                        return true;
                    break;
                }
                foundOpponent = true;
                r += dr;
                c += dc;
            }
        }

        return false;
    }
}
