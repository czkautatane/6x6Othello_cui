namespace OthelloAI;

//ランダムに手を打つAI
public class RandomAI : IAI
{
    private readonly Random _random = new();
    public string Name => "RandomAI";
    public (int row, int col) DecideMove(int[,] board, int currentPlayer)
    {
        var gameBoard = new Board(board);
        var validMoves = gameBoard.GetValidMoves(currentPlayer);

        if (validMoves.Count == 0)
            return (-1, -1); // パスを示す

        // ランダムな手を選択
        return validMoves[_random.Next(validMoves.Count)];
    }
}