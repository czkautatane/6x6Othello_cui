namespace OthelloAI;

/// <summary>
/// オセロAIのインターフェース
/// </summary>
public interface IAI
{
    /// <summary>
    /// 現在の盤面状態から次の手を決定する
    /// </summary>
    /// <param name="board">現在の盤面</param>
    /// <param name="currentPlayer">現在のプレイヤー（1: 黒, -1: 白）</param>
    /// <returns>次の手の座標 (row, col)</returns>
    (int row, int col) DecideMove(int[,] board, int currentPlayer);

    /// <summary>
    /// AIの名前を取得
    /// </summary>
    string Name { get; }
}
