using System.Text.Json;

namespace OthelloAI;

public class Program
{
    private static Random _random = new Random(); //追加

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== オセロゲーム ===\n");
            Console.WriteLine("1: 人間 vs AI");
            Console.WriteLine("2: AI vs AI (1回対戦)");
            Console.WriteLine("3: AI vs AI (複数回対戦・強化学習)");
            Console.WriteLine("4: 終了");
            Console.Write("\n選択してください: ");

            if (!int.TryParse(Console.ReadLine(), out int choice))
            {
                Console.WriteLine("無効な入力です。");
                continue;
            }

            switch (choice)
            {
                case 1:
                    PlayHumanVsAI();
                    break;
                case 2:
                    PlaySingleAIBattle();
                    break;
                case 3:
                    PlayMultipleAIBattles();
                    break;
                case 4:
                    return;
                default:
                    Console.WriteLine("無効な選択です。");
                    break;
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private static void PlayHumanVsAI()
    {
        Console.Clear();
        Console.WriteLine("=== 人間 vs AI ===\n");
        Console.WriteLine("1: 人間が黒（先手）");
        Console.WriteLine("2: 人間が白（後手）");
        Console.Write("\n選択してください: ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || (choice != 1 && choice != 2))
        {
            Console.WriteLine("無効な選択です。メインメニューに戻ります。");
            return;
        }

        Console.WriteLine("\nAIを選択してください：");
        var availableAIs = AIFactory.GetAvailableAIs();
        for (int i = 0; i < availableAIs.Count; i++)
        {
            var (_, displayName, description) = availableAIs[i];
            Console.WriteLine($"{i + 1}: {displayName} - {description}");
        }

        Console.Write("\n選択してください: ");
        if (!int.TryParse(Console.ReadLine(), out int aiChoice) ||
            aiChoice < 1 || aiChoice > availableAIs.Count)
        {
            Console.WriteLine("無効な選択です。メインメニューに戻ります。");
            return;
        }

        var human = new HumanPlayer();
        var ai = AIFactory.CreateAI(availableAIs[aiChoice - 1].name);

        GameManager game;
        if (choice == 1)
        {
            game = new GameManager(human, ai);
            Console.WriteLine("\n人間（黒）vs AI（白）");
        }
        else
        {
            game = new GameManager(ai, human);
            Console.WriteLine("\nAI（黒）vs 人間（白）");
        }

        game.RunGame();
    }

     private static void PlaySingleAIBattle()
    {
      Console.Clear();
      Console.WriteLine("=== AI Battle ===\n");

      // 利用可能なAIのリストを表示
      var availableAIs = AIFactory.GetAvailableAIs();
      Console.WriteLine("AIを選択してください（黒）：");
      for (int i = 0; i < availableAIs.Count; i++)
      {
          var (_, displayName, description) = availableAIs[i];
          Console.WriteLine($"{i + 1}: {displayName} - {description}");
      }
      Console.Write("選択: ");
      int ai1Choice = int.Parse(Console.ReadLine() ?? "0") - 1;


      Console.WriteLine("\nAIを選択してください（白）：");
      for (int i = 0; i < availableAIs.Count; i++)
      {
            //黒で選択したものと同じものは表示しない
            if(i == ai1Choice) continue;
          var (_, displayName, description) = availableAIs[i];
          Console.WriteLine($"{i + 1}: {displayName} - {description}");
      }
      Console.Write("選択: ");
      int ai2Choice = int.Parse(Console.ReadLine() ?? "0") - 1;

        // 選択されたAIのインスタンスを作成
        var ai1 = AIFactory.CreateAI(availableAIs[ai1Choice].name);
        var ai2 = AIFactory.CreateAI(availableAIs[ai2Choice].name);

        // GameManagerを使って対局
        var game = new GameManager(ai1, ai2);
        game.RunGame();
    }


    private static void PlayMultipleAIBattles()
    {
        Console.Clear();
        Console.WriteLine("=== AI Training Battle ===\n");

        // 保存されたセッションをチェック
        if (TrainingSession.Exists())
        {
            Console.Write("保存された学習セッションが見つかりました。再開しますか？ (Y/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() == "Y")
            {
                ResumeTrainingSession();
                return;
            }
            else
            {
                TrainingSession.Clear();
            }
        }

        StartNewTrainingSession();
    }

    private static void StartNewTrainingSession()
    {
        // 設定ファイルから対戦回数を取得
        var settings = GetSettings();

        //対戦回数
        int rounds = settings.GetProperty("game")
            .GetProperty("training")
            .GetProperty("numGames")
            .GetInt32();

        Console.Write("\n盤面を表示しますか？ (Y/N, デフォルト: N): ");
        var displayEnabled = Console.ReadLine()?.Trim().ToUpper() == "Y";

        // リセットするかどうか確認
        Console.Write("データベースをリセットしますか？ (Y/N, デフォルト: N): ");
        var resetDatabase = Console.ReadLine()?.Trim().ToUpper() == "Y";


        // 強化学習AIを常に使用 (AIの選択は不要)
        IAI ai1 = AIFactory.CreateAI("ReinforcementLearningAI"); // 強化学習AI


        // QValueRepositoryの初期化（リセットする場合）
        if (resetDatabase && ai1 is ReinforcementLearningAI rlAi)
        {
            rlAi.ClearQValueRepository();
        }

        //TrainingSessionの準備
        var session = new TrainingSession
        {
            TotalGames = rounds,
            CompletedGames = 0,
            Wins = 0,
            StartTime = DateTime.Now,
            Ai1Name = ai1.Name,  // 強化学習AI
            Ai2Name = "OpponentAI" // 仮の名前
        };

        Console.WriteLine($"\n{rounds}回の対戦を開始します...\n");

        RunTrainingSession(session, ai1, displayEnabled);
    }

    private static void ResumeTrainingSession()
    {
        var session = TrainingSession.Load();
        if (session == null)
        {
            Console.WriteLine("セッションの読み込みに失敗しました。新しいセッションを開始します。");
            StartNewTrainingSession();
            return;
        }

        Console.Write("\n盤面を表示しますか？ (Y/N, デフォルト: N): ");
        var displayEnabled = Console.ReadLine()?.Trim().ToUpper() == "Y";

        // 強化学習AIは常に使用
        var ai1 = AIFactory.CreateAI(session.Ai1Name);

        Console.WriteLine($"\n学習を再開します（進捗: {session.CompletedGames}/{session.TotalGames}）...\n");

        RunTrainingSession(session, ai1, displayEnabled);
    }

    private static void RunTrainingSession(TrainingSession session, IAI ai1, bool displayEnabled)
    {
        // 設定ファイルから保存間隔を取得
        var settings = GetSettings();
        var saveInterval = settings.GetProperty("game")
            .GetProperty("trainingSession")
            .GetProperty("saveInterval")
            .GetInt32();

        // ログファイルのパスを設定
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "training_log.txt");

        // ε-greedyのパラメータ
        double initialEpsilon = ((ReinforcementLearningAI)ai1).Epsilon;  //初期値
        double epsilonDecayRate = settings.GetProperty("ai").GetProperty("reinforcementLearning").GetProperty("epsilonDecayRate").GetDouble();

        //対戦相手の割合に関するパラメータ
        var aiRatios = settings.GetProperty("game").GetProperty("training").GetProperty("aiRatios");
        double simpleAIRatio = aiRatios.GetProperty("SimpleAI").GetDouble();
        double simpleAIVariantRatio = aiRatios.GetProperty("SimpleAIVariant").GetDouble();
        double randomAIRatio = aiRatios.GetProperty("RandomAI").GetDouble();

        //SimpleAIと対戦する際の学習率
        double learningRateMultiplierForSimpleAI = settings.GetProperty("ai").GetProperty("reinforcementLearning").GetProperty("learningRateMultiplierForSimpleAI").GetDouble();

        //RandomAIと対戦する回数
        int randomOpponentGames = settings.GetProperty("game").GetProperty("training").GetProperty("randomOpponentGames").GetInt32();


        try
        {
            var startTime = session.StartTime;
            for (int i = session.CompletedGames; i < session.TotalGames; i++)
            {

                // 進捗表示と定期保存
                if (!displayEnabled && (i + 1) % saveInterval == 0)
                {
                    var elapsed = DateTime.Now - startTime;
                    var estimatedTotal = elapsed.TotalSeconds / (i + 1) * session.TotalGames;
                    var remaining = TimeSpan.FromSeconds(estimatedTotal - elapsed.TotalSeconds);

                    Console.WriteLine($"進捗: {i + 1}/{session.TotalGames} ゲーム完了 " +
                        $"(勝率: {(double)session.Wins / (i + 1) * 100:F1}% " +
                        $"残り時間: {remaining:hh\\:mm\\:ss})");

                    // セッション情報を保存
                    session.CompletedGames = i + 1;
                    session.Save();
                }
                else if (displayEnabled)
                {
                    Console.WriteLine($"\nGame {i + 1}/{session.TotalGames}");
                }


                // 対戦相手を決定
                IAI ai2;
                double learningRate = ((ReinforcementLearningAI)ai1).LearningRate; // 通常の学習率

                //最初のrandomOpponentGames回は、RandomAIと対戦
                if(i < randomOpponentGames)
                {
                    ai2 = new RandomAI();
                }
                else
                {
                    // それ以降は、設定された割合で対戦相手を決定
                    double randomValue = _random.NextDouble();
                    if (randomValue < simpleAIRatio)
                    {
                        ai2 = new SimpleAI();
                        // SimpleAIと対戦する場合は学習率を下げる
                        learningRate *= learningRateMultiplierForSimpleAI;
                    }
                    else if (randomValue < simpleAIRatio + simpleAIVariantRatio)
                    {
                        ai2 = new SimpleAIVariant();
                    }
                    else
                    {
                        ai2 = new RandomAI();
                    }
                }

                // 先手後手をランダムに決定
                IAI firstPlayer, secondPlayer;
                if (_random.Next(2) == 0)
                {
                    firstPlayer = ai1;
                    secondPlayer = ai2;
                }
                else
                {
                    firstPlayer = ai2;
                    secondPlayer = ai1;
                }
                var game = new GameManager(firstPlayer, secondPlayer, displayEnabled);


                // 各ターンの状態を記録
                if (ai1 is ReinforcementLearningAI rlAi)
                {

                    // 最初のターンを実行(黒の手番)
                    game.PlayTurn();
                    //var currentState = GetBoardState(game.Board);  //ここを修正
                    var currentState = rlAi.GetBoardState(game.Board);

                    // ゲーム終了までループ
                    while (!game.IsGameOver)
                    {

                        // 白（強化学習AI）の手番
                        (int row, int col) = ai1.DecideMove(game.Board.GetBoard(), -1);
                        var nextBoard = new Board(game.Board.GetBoard());
                        nextBoard.PlaceStone(row, col, -1);  //仮に次の一手を打つ
                        //var nextState = GetBoardState(nextBoard); //ここを修正
                        var nextState = rlAi.GetBoardState(nextBoard);
                        var reward = CalculateReward(nextBoard, -1); // 中間報酬+最終報酬

                        bool done = game.IsGameOver;
                        //game.RecordTurn(currentState, nextState, reward, done); //ここを修正
                        game.RecordTurn(currentState, nextState, reward, done); //rlAiを渡す
                        currentState = nextState;

                        //ゲームを1ターン進める。
                        game.PlayTurn(); //


                    }
                    // Q値の更新 (現在の学習率を使用)
                    var updates = game.GetTurnUpdates();
                    rlAi.BatchUpdateQValues(updates, learningRate);  // 学習率を渡す
                    game.ClearTurnUpdates(); // 蓄積されたターン情報をクリア
                }
                else
                {
                }

                // ゲーム終了後の処理
                int winner = game.GetWinner();  // 勝敗の取得
                string winnerRole = (winner == 1) ? "先手" : (winner == -1 ? "後手" : "引き分け");
                string winnerName = (winner == 1) ? game.BlackPlayer.Name : (winner == -1 ? game.WhitePlayer.Name : "引き分け");

                // 勝利数を集計（引き分けは除く）
                if (winnerName == "ReinforcementLearningAI")
                {
                    session.Wins++;
                }
                else if (winnerName != "引き分け")
                {
                    session.OpponentWins++; // 対戦相手側の勝利数をカウント（TrainingSession に OpponentWins プロパティを追加してください）
                }
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Game {i + 1}/{session.TotalGames}: 対戦: 先手({game.BlackPlayer.Name}) vs 後手({game.WhitePlayer.Name}), " +
                                  $"勝者: {winnerRole}({winnerName}), 強化学習AI勝利数: {session.Wins}, 対戦相手勝利数: {session.OpponentWins}\n";
                File.AppendAllText(logPath, logEntry);

                // εの値を更新（勝敗に応じて）
                if (ai1 is ReinforcementLearningAI rlAi2)
                {
                    if (winner == -1)
                    {
                        // 勝った場合はεを少しだけ減らす（探索を減らす）
                        rlAi2.UpdateEpsilon(epsilonDecayRate);

                    }
                    else
                    {
                        // 負けた場合はεを少し増やす（探索を増やす）
                        // ただし、初期値よりは大きくならない
                        rlAi2.UpdateEpsilon(1 / epsilonDecayRate);
                        if (rlAi2.Epsilon > initialEpsilon)
                        {
                            rlAi2.SetEpsilon(initialEpsilon); // 初期値に戻す
                        }
                    }
                }

                game.RunGame();

            }

            var totalTime = DateTime.Now - startTime;
            Console.WriteLine($"\n=== 結果 ===");
            Console.WriteLine($"総対戦数: {session.TotalGames}");
            Console.WriteLine($"強化学習AIの勝利数: {session.Wins}");
            Console.WriteLine($"勝率: {(double)session.Wins / session.TotalGames * 100:F2}%");
            Console.WriteLine($"総実行時間: {totalTime:hh\\:mm\\:ss}");

            // セッション完了時に保存ファイルを削除
            TrainingSession.Clear();
        }
        catch (Exception ex)
        {
            // エラー発生時にセッション情報を保存
            session.CompletedGames = session.CompletedGames + 1;
            session.Save();
            Console.WriteLine($"\nエラーが発生しました: {ex.Message}");
            Console.WriteLine("セッション情報を保存しました。後で再開できます。");
        }
    }

    /// <summary>
    /// 報酬を計算
    /// </summary>
    private static double CalculateReward(Board board, int player)
    {
        if (board.IsGameOver())
        {
            var winner = board.GetWinner();
            if (winner == player) return 100.0;
            if (winner == -player) return -100.0;
            return 0.0;
        }
        //中間報酬
        var settings = GetSettings();
        var config = settings.GetProperty("ai").GetProperty("reinforcementLearning");
        var intermediateRewardScale = config.GetProperty("intermediateRewardScale").GetDouble();
        var cornerWeight = config.GetProperty("cornerWeight").GetDouble();
        var legalMoveWeight = config.GetProperty("legalMoveWeight").GetDouble();

        return (EvaluateBoard(board, player, cornerWeight, legalMoveWeight) - EvaluateBoard(board, -player, cornerWeight, legalMoveWeight)) * intermediateRewardScale;

    }

    // 盤面評価関数（中間報酬用）
    private static double EvaluateBoard(Board board, int player, double cornerWeight, double legalMoveWeight)
    {
        double score = 0.0;
        // 例: コーナー戦略の重み付け
        var corners = new[] { (0, 0), (0, 5), (5, 0), (5, 5) };
        foreach (var (r, c) in corners)
        {
            var cell = board.GetCell(r, c);
            if (cell == player)
                score += cornerWeight;
            else if (cell == -player)
                score -= cornerWeight;
        }
        // 例: 合法手数の差を評価
        var myMoves = board.GetValidMoves(player).Count;
        var oppMoves = board.GetValidMoves(-player).Count;
        score += (myMoves - oppMoves) * legalMoveWeight;
        return score;
    }


    /// <summary>
    /// 設定ファイルから設定を読み込む
    /// </summary>
    private static JsonElement GetSettings()
    {
        var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException("設定ファイルが見つかりません", settingsPath);
        }
        return JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(settingsPath));
    }
}
