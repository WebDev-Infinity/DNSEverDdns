using Microsoft.Win32;

namespace DNSEverDdns.Win;

/// <summary>
/// 현재 Windows 사용자의 로그인 시작프로그램 등록 상태를 관리합니다.
/// </summary>
internal static class WindowsStartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DNSEverDdns";

    /// <summary>
    /// 현재 사용자의 시작프로그램에 DNSEver DDNS가 등록되어 있는지 확인합니다.
    /// </summary>
    public static bool IsRegistered()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return runKey?.GetValue(ValueName) is string command && !string.IsNullOrWhiteSpace(command);
    }

    /// <summary>
    /// 선택 상태에 따라 현재 사용자의 시작프로그램을 등록하거나 해제합니다.
    /// </summary>
    public static void SetRegistration(bool register)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Windows 시작프로그램 레지스트리를 열 수 없습니다.");

        if (!register)
        {
            runKey.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executablePath = Application.ExecutablePath;
        var command = $"\"{executablePath}\" --tray";
        runKey.SetValue(ValueName, command, RegistryValueKind.String);
    }
}
