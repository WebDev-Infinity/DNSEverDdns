using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DNSEverDdns.Win;

/// <summary>
/// 애플리케이션 설정 파일 저장과 인증 코드 보호를 담당합니다.
/// </summary>
public sealed class AppSettingsStore
{
    private readonly string _settingsPath;

    /// <summary>
    /// 기본 로컬 애플리케이션 데이터 경로를 사용하여 저장소를 초기화합니다.
    /// </summary>
    public AppSettingsStore()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directoryPath = Path.Combine(appDataPath, "DNSEverDdns");
        Directory.CreateDirectory(directoryPath);

        _settingsPath = Path.Combine(directoryPath, "settings.json");
    }

    /// <summary>
    /// 저장된 설정을 읽거나 기본 설정을 반환합니다.
    /// </summary>
    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        var json = File.ReadAllText(_settingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    /// <summary>
    /// 현재 설정을 JSON 파일로 저장합니다.
    /// </summary>
    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    /// <summary>
    /// 인증 코드를 현재 Windows 사용자 범위로 암호화합니다.
    /// </summary>
    public string ProtectAuthCode(string authCode)
    {
        if (string.IsNullOrEmpty(authCode))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(authCode);
        var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    /// <summary>
    /// 현재 Windows 사용자 범위로 암호화된 인증 코드를 복호화합니다.
    /// </summary>
    public string UnprotectAuthCode(string protectedAuthCode)
    {
        if (string.IsNullOrEmpty(protectedAuthCode))
        {
            return string.Empty;
        }

        var protectedBytes = Convert.FromBase64String(protectedAuthCode);
        var bytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
