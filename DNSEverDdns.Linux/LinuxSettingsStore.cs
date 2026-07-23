using System.Runtime.Versioning;
using System.Text.Json;

namespace DNSEverDdns.Linux;

/// <summary>
/// XDG 규칙에 맞는 Linux 설정 파일의 읽기와 저장을 담당합니다.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxSettingsStore
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// 기본 XDG 설정 경로를 사용하여 저장소를 초기화합니다.
    /// </summary>
    public LinuxSettingsStore()
    {
        SettingsDirectoryPath = ResolveSettingsDirectoryPath();
        SettingsFilePath = Path.Combine(SettingsDirectoryPath, "settings.json");
    }

    /// <summary>
    /// 설정 디렉터리의 전체 경로입니다.
    /// </summary>
    public string SettingsDirectoryPath { get; }

    /// <summary>
    /// 설정 파일의 전체 경로입니다.
    /// </summary>
    public string SettingsFilePath { get; }

    /// <summary>
    /// 저장된 설정을 읽거나 기본 설정을 반환합니다.
    /// </summary>
    public LinuxSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return new LinuxSettings();
        }

        var json = File.ReadAllText(SettingsFilePath);
        return JsonSerializer.Deserialize<LinuxSettings>(json, _jsonOptions) ?? new LinuxSettings();
    }

    /// <summary>
    /// 설정을 JSON으로 저장하고 사용자만 읽고 쓸 수 있도록 권한을 제한합니다.
    /// </summary>
    public void Save(LinuxSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectoryPath);
        File.SetUnixFileMode(
            SettingsDirectoryPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(SettingsFilePath, json);
        File.SetUnixFileMode(SettingsFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    /// <summary>
    /// XDG_CONFIG_HOME 또는 사용자 홈을 기준으로 설정 디렉터리를 결정합니다.
    /// </summary>
    private static string ResolveSettingsDirectoryPath()
    {
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

        if (!string.IsNullOrWhiteSpace(xdgConfigHome))
        {
            return Path.Combine(xdgConfigHome, "DNSEverDdns");
        }

        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(homeDirectory))
        {
            throw new InvalidOperationException("Linux 사용자 홈 디렉터리를 확인할 수 없습니다.");
        }

        return Path.Combine(homeDirectory, ".config", "DNSEverDdns");
    }
}
