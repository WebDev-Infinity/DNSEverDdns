namespace DNSEverDdns.Win;

/// <summary>
/// 로컬 로그 파일에 실행 이력을 기록합니다.
/// </summary>
public sealed class AppLogger
{
    private readonly string _logDirectoryPath;

    /// <summary>
    /// 기본 로그 경로를 사용하여 로거를 초기화합니다.
    /// </summary>
    public AppLogger()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDirectoryPath = Path.Combine(appDataPath, "DNSEverDdns", "logs");
        Directory.CreateDirectory(_logDirectoryPath);
    }

    /// <summary>
    /// 정보 로그 메시지를 기록합니다.
    /// </summary>
    public void Info(string message)
    {
        Write("INFO", message);
    }

    /// <summary>
    /// 오류 로그 메시지를 기록합니다.
    /// </summary>
    public void Error(string message)
    {
        Write("ERROR", message);
    }

    /// <summary>
    /// 날짜별 로그 파일에 메시지를 추가합니다.
    /// </summary>
    private void Write(string level, string message)
    {
        var path = Path.Combine(_logDirectoryPath, $"{DateTime.Now:yyyyMMdd}.log");
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
        File.AppendAllText(path, line);
    }
}
