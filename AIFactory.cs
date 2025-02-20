using System.Text.Json;

namespace OthelloAI;

/// <summary>
/// AI生成を管理するファクトリークラス
/// </summary>
public class AIFactory
{
    private static JsonElement? _settings;

    /// <summary>
    /// 設定を初期化
    /// </summary>
    private static void InitializeSettings()
    {
        if (_settings != null) return;

        var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException("設定ファイルが見つかりません", settingsPath);
        }

        var jsonString = File.ReadAllText(settingsPath);
        _settings = JsonSerializer.Deserialize<JsonElement>(jsonString);
    }

    /// <summary>
    /// 利用可能なAIの一覧を取得
    /// </summary>
    public static List<(string name, string displayName, string description)> GetAvailableAIs()
    {
        InitializeSettings();
        var aiList = new List<(string, string, string)>();

        if (_settings.HasValue)
        {
            var ais = _settings.Value.GetProperty("game").GetProperty("availableAIs");
            if (ais.ValueKind == JsonValueKind.Array)
            {
            foreach (var ai in ais.EnumerateArray())
            {
                aiList.Add((
                    ai.GetProperty("name").GetString() ?? "",
                    ai.GetProperty("displayName").GetString() ?? "",
                    ai.GetProperty("description").GetString() ?? ""
                ));
            }
            }
        }

        return aiList.Count > 0 ? aiList : throw new InvalidOperationException("利用可能なAIが設定されていません");
    }

    /// <summary>
    /// 指定された名前のAIを生成
    /// </summary>
    public static IAI CreateAI(string name)
    {
        InitializeSettings();

        return name switch
        {
            "SimpleAI" => new SimpleAI(),
            "SimpleAIVariant" => new SimpleAIVariant(), // 追加
            "RandomAI" => new RandomAI(), // 追加
            "ReinforcementLearningAI" => CreateReinforcementLearningAI(),
            _ => throw new ArgumentException($"不明なAI: {name}", nameof(name))
        };
    }

    /// <summary>
    /// 強化学習AIを設定から生成
    /// </summary>
    private static ReinforcementLearningAI CreateReinforcementLearningAI()
    {
        if (!_settings.HasValue)
        {
            throw new InvalidOperationException("設定が初期化されていません");
        }

        var config = _settings.Value.GetProperty("ai").GetProperty("reinforcementLearning");
        if (config.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("強化学習AIの設定が見つかりません");
        }

        var dbPath = config.GetProperty("dbPath").GetString() ?? "othello_qvalues.db";
        var learningRate = config.GetProperty("learningRate").GetDouble();
        var discountFactor = config.GetProperty("discountFactor").GetDouble();
        var epsilon = config.GetProperty("epsilon").GetDouble();
        //中間報酬
        var intermediateRewardScale = config.GetProperty("intermediateRewardScale").GetDouble();

        return new ReinforcementLearningAI(dbPath, learningRate, discountFactor, epsilon, intermediateRewardScale);
    }
}