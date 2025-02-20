using System.Text.Json;

namespace OthelloAI;

/// <summary>
/// 学習セッション情報を管理するクラス
/// </summary>
public class TrainingSession
{
    public int TotalGames { get; set; }
    public int CompletedGames { get; set; }
    public int Wins { get; set; }
    // 追加: 対戦相手側の勝利数をカウントするプロパティ
    public int OpponentWins { get; set; } = 0;
    public DateTime StartTime { get; set; }
    public DateTime? LastSaveTime { get; set; }
    public string Ai1Name { get; set; } = "";
    public string Ai2Name { get; set; } = "";

    /// <summary>
    /// セッション情報を保存
    /// </summary>
    public void Save()
    {
        var settings = GetSettings();
        var sessionFile = settings.GetProperty("game")
            .GetProperty("trainingSession")
            .GetProperty("sessionFile")
            .GetString() ?? "training_session.json";

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        File.WriteAllText(sessionFile, json);
        LastSaveTime = DateTime.Now;
    }

    /// <summary>
    /// 保存されたセッション情報を読み込み
    /// </summary>
    public static TrainingSession? Load()
    {
        var settings = GetSettings();
        var sessionFile = settings.GetProperty("game")
            .GetProperty("trainingSession")
            .GetProperty("sessionFile")
            .GetString() ?? "training_session.json";

        if (!File.Exists(sessionFile))
            return null;

        var json = File.ReadAllText(sessionFile);
        return JsonSerializer.Deserialize<TrainingSession>(json);
    }

    /// <summary>
    /// セッションファイルが存在するかチェック
    /// </summary>
    public static bool Exists()
    {
        var settings = GetSettings();
        var sessionFile = settings.GetProperty("game")
            .GetProperty("trainingSession")
            .GetProperty("sessionFile")
            .GetString() ?? "training_session.json";

        return File.Exists(sessionFile);
    }

    /// <summary>
    /// 保存されたセッションを削除
    /// </summary>
    public static void Clear()
    {
        var settings = GetSettings();
        var sessionFile = settings.GetProperty("game")
            .GetProperty("trainingSession")
            .GetProperty("sessionFile")
            .GetString() ?? "training_session.json";

        if (File.Exists(sessionFile))
            File.Delete(sessionFile);
    }

    /// <summary>
    /// 設定を取得
    /// </summary>
    private static JsonElement GetSettings()
    {
        var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException("設定ファイルが見つかりません", settingsPath);
        }

        var jsonString = File.ReadAllText(settingsPath);
        return JsonSerializer.Deserialize<JsonElement>(jsonString);
    }
}
