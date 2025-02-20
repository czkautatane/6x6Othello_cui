using Microsoft.Data.Sqlite;

namespace OthelloAI;

/// <summary>
/// Q値をSQLiteで管理するリポジトリクラス
/// </summary>
public class QValueRepository
{
    private readonly string _dbPath;
    private const string TableName = "qvalues";
    private readonly Dictionary<string, double> _cache;
    private readonly object _lockObject = new();

    public QValueRepository(string dbPath = "othello_qvalues.db")
    {
        _dbPath = dbPath;
        _cache = new Dictionary<string, double>();
        LoadToCache();
    }

    /// <summary>
    /// キャッシュにQ値を読み込み
    /// </summary>
    private void LoadToCache()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {TableName} (
                State TEXT PRIMARY KEY,
                Value REAL
            );
        ";
        command.ExecuteNonQuery();

        command.CommandText = $@"SELECT State, Value FROM {TableName}";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            _cache[reader.GetString(0)] = reader.GetDouble(1);
        }
    }

    /// <summary>
    /// Q値を取得（キャッシュから）
    /// </summary>
    public double GetValue(string state)
    {
        lock (_lockObject)
        {
            return _cache.GetValueOrDefault(state, 0.0);
        }
    }

    /// <summary>
    /// Q値を更新（キャッシュとDB）
    /// </summary>
    public void UpdateValue(string state, double value)
    {
        lock (_lockObject)
        {
            _cache[state] = value;
        }

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $@"
            INSERT OR REPLACE INTO {TableName} (State, Value)
            VALUES (@State, @Value);
        ";
        command.Parameters.AddWithValue("@State", state);
        command.Parameters.AddWithValue("@Value", value);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 複数のQ値を一括更新
    /// </summary>
    public void BatchUpdate(Dictionary<string, double> updates)
    {
        lock (_lockObject)
        {
            foreach (var (state, value) in updates)
            {
                _cache[state] = value;
            }
        }

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $@"
            INSERT OR REPLACE INTO {TableName} (State, Value)
            VALUES (@State, @Value);
        ";
        var stateParam = command.CreateParameter();
        stateParam.ParameterName = "@State";
        command.Parameters.Add(stateParam);
        var valueParam = command.CreateParameter();
        valueParam.ParameterName = "@Value";
        command.Parameters.Add(valueParam);

        try
        {
            foreach (var (state, value) in updates)
            {
                stateParam.Value = state;
                valueParam.Value = value;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

     /// <summary>
    /// キャッシュされているQ値の数を取得
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lockObject)
            {
                return _cache.Count;
            }
        }
    }

    // ClearDatabase メソッドを追加：DBとキャッシュを再初期化
    public void ClearDatabase()
    {
        lock (_lockObject)
        {
            _cache.Clear();
        }

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        // テーブルの再作成
        command.CommandText = $@"DROP TABLE IF EXISTS {TableName};";
        command.ExecuteNonQuery();

        command.CommandText = $@"
            CREATE TABLE {TableName} (
                State TEXT PRIMARY KEY,
                Value REAL
            );
        ";
        command.ExecuteNonQuery();
    }
}