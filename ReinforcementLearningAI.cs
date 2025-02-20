using System.Text.Json;

namespace OthelloAI;

/// <summary>
/// Q学習を使用した強化学習AI
/// </summary>
public class ReinforcementLearningAI : IAI
{
    private readonly Random _random = new();
    private readonly QValueRepository _qRepository;
    private readonly double _learningRate;
    private readonly double _discountFactor;
    private readonly double _intermediateRewardScale;
    private double _epsilon;

    // 評価関数の重みパラメータ（appsettings.jsonから読み込む）
    private readonly double _cornerWeight;
    private readonly double _legalMoveWeight;

    // 経験リプレイ用のバッファ
    private List<(string currentState, string nextState, double reward, bool done)> _replayBuffer = new();
    private int _replayBufferSize = 10000; // バッファサイズ
    private int _batchSize = 32;  // バッチサイズ


    public ReinforcementLearningAI(
        string dbPath = "othello_qvalues.db",
        double learningRate = 0.1,
        double discountFactor = 0.9,
        double epsilon = 0.1,
        double intermediateRewardScale = 1.0)
    {
        _qRepository = new QValueRepository(dbPath);
        _learningRate = learningRate;
        _discountFactor = discountFactor;
        _epsilon = epsilon;
        _intermediateRewardScale = intermediateRewardScale;

        // appsettings.jsonから設定を読み込む
        var settings = JsonSerializer.Deserialize<JsonElement>(
            File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json")));

        var config = settings.GetProperty("ai").GetProperty("reinforcementLearning");

        _cornerWeight = config.GetProperty("cornerWeight").GetDouble();
        _legalMoveWeight = config.GetProperty("legalMoveWeight").GetDouble();
        _replayBufferSize = config.GetProperty("replayBufferSize").GetInt32();
        _batchSize = config.GetProperty("batchSize").GetInt32();
    }
    public double Epsilon
    {
        get { return _epsilon; }
        private set { _epsilon = value; }
    }

     public double LearningRate => _learningRate; // 追加


    public void SetEpsilon(double value)
    {
        _epsilon = value;
    }


    public string Name => "ReinforcementLearningAI";

    public (int row, int col) DecideMove(int[,] board, int currentPlayer)
    {
        var gameBoard = new Board(board);
        var validMoves = gameBoard.GetValidMoves(currentPlayer);

        if (validMoves.Count == 0)
            return (-1, -1); // パスを示す

        // ε-greedy法による行動選択
        if (_random.NextDouble() < _epsilon)
        {
            // ランダムな行動を選択（探索）
            return validMoves[_random.Next(validMoves.Count)];
        }

        // 現在の状態のハッシュ値を取得
        var currentState = GetBoardState(gameBoard);
        double maxQ = double.MinValue;
        var bestMoves = new List<(int row, int col)>();

        foreach (var move in validMoves)
        {
            var nextBoard = new Board(gameBoard);
            nextBoard.PlaceStone(move.row, move.col, currentPlayer);

            // 次の状態に対するQ値を取得
            var nextState = GetBoardState(nextBoard);
            var qValue = GetQValue(nextState);

            if (qValue > maxQ)
            {
                maxQ = qValue;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (Math.Abs(qValue - maxQ) < 0.0001)
            {
                bestMoves.Add(move);
            }
        }

        // 最終的な行動を選択
        var selectedMove = bestMoves[_random.Next(bestMoves.Count)];
        return selectedMove;
    }

    /// <summary>
    /// 盤面の状態を文字列として取得
    /// </summary>
    public string GetBoardState(Board board)
    {
        var rows = new List<string>();
        for (int i = 0; i < Board.Size; i++)
        {
            var cells = new List<string>();
            for (int j = 0; j < Board.Size; j++)
            {
                cells.Add(board.GetCell(i, j).ToString());
            }
            rows.Add(string.Join(",", cells));
        }
        return string.Join(";", rows);
    }

    /// <summary>
    /// Q値を取得
    /// </summary>
    private double GetQValue(string state)
    {
        return _qRepository.GetValue(state);
    }

    // ε減衰のためのメソッド
    public void UpdateEpsilon(double decayRate)
    {
        _epsilon = Math.Max(_epsilon * decayRate, 0.01); // 下限値を0.01とする
    }

    // Q値を一括更新するメソッド（Experience Replay）
    //学習率を引数で渡せるように変更
    public void BatchUpdateQValues(List<(string currentState, string nextState, double reward, bool done)> updates, double learningRate)
    {
        // 経験をリプレイバッファに追加
        foreach (var (currentState, nextState, reward, done) in updates)
        {
            _replayBuffer.Add((currentState, nextState, reward, done));
            if (_replayBuffer.Count > _replayBufferSize)
            {
                _replayBuffer.RemoveAt(0); // 古い経験を削除
            }
        }

        // リプレイバッファが十分大きくなってから、経験リプレイを開始
        if (_replayBuffer.Count < _batchSize) return;

        // 経験リプレイによるバッチ更新
        var batch = new List<(string currentState, string nextState, double reward, bool done)>();

        // ミニバッチを作成(シャッフル)
        for (int i = 0; i < _batchSize; i++)
        {
            int index = _random.Next(_replayBuffer.Count);
            batch.Add(_replayBuffer[index]);
        }

        // 各状態への複数回更新を累積し、平均値で反映
        var aggregatedUpdates = new Dictionary<string, (double sum, int count)>();
        foreach (var (currentState, nextState, reward, done) in batch)
        {
            var currentQ = _qRepository.GetValue(currentState);
            double nextQ;

            if (done)
            {
                nextQ = 0; // ゲーム終了時は次状態の価値を0にする
            }
            else
            {
                nextQ = GetQValue(nextState);
            }
            //TD error = reward + discountFactor * nextQ - currentQ
            var newQ = currentQ + learningRate * (reward + (_discountFactor * nextQ) - currentQ);

            if (aggregatedUpdates.ContainsKey(currentState))
            {
                var (sum, count) = aggregatedUpdates[currentState];
                aggregatedUpdates[currentState] = (sum + newQ, count + 1);
            }
            else
            {
                aggregatedUpdates.Add(currentState, (newQ, 1));
            }
        }

        // Qテーブルの更新
        var qValueUpdates = new Dictionary<string, double>();
        foreach (var kvp in aggregatedUpdates)
        {
            qValueUpdates[kvp.Key] = kvp.Value.sum / kvp.Value.count;
        }
        _qRepository.BatchUpdate(qValueUpdates);
    }

    // QValueRepositoryをクリアするメソッド
    public void ClearQValueRepository()
    {
        _qRepository.ClearDatabase();
    }
}