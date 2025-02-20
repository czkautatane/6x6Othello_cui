namespace OthelloAI;

public class SimpleAIVariant : SimpleAI
{
    private readonly Random _random = new();
    private readonly double _randomFactor;
    private readonly int[,] _positionValuesVariant;

    public SimpleAIVariant(double randomFactor = 0.1) : base()
    {
        _randomFactor = randomFactor;
        // positionValuesを少しランダムに変えたバージョンを作成
        _positionValuesVariant = (int[,])_positionValues.Clone(); //DeepCopy
        for(int i=0; i< Board.Size; ++i)
        {
            for(int j=0; j<Board.Size; ++j)
            {
                _positionValuesVariant[i, j] += _random.Next(-5, 6); // -5から5のランダムな値を加算
            }
        }

    }

    public override string Name => "SimpleAIVariant";

    public override (int row, int col) DecideMove(int[,] board, int currentPlayer)
    {
      //SimpleAIのDecideMoveを呼び出す。
      (int row, int col) = base.DecideMove(board, currentPlayer);

      //最善手が複数あった場合に、_randomFactorの確率でランダムに手を選ぶ
      if(_random.NextDouble() < _randomFactor)
      {
            var gameBoard = new Board(board);
            var validMoves = gameBoard.GetValidMoves(currentPlayer);
            if (validMoves.Count > 0)
            {
                return validMoves[_random.Next(validMoves.Count)];
            }
      }
      return (row, col);

    }

    protected override int EvaluatePosition(Board board, int player)
    {
      //_positionValuesVariantを使って評価値を計算
      int score = 0;

      // 位置の価値を評価
      for (int i = 0; i < Board.Size; i++)
      {
          for (int j = 0; j < Board.Size; j++)
          {
              var cell = board.GetCell(i, j);
              if (cell == player)
                  score += _positionValuesVariant[i, j]; // こちらを変更
              else if (cell == -player)
                  score -= _positionValuesVariant[i, j]; // こちらを変更
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