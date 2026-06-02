namespace DNSEverDdns.Win;

/// <summary>
/// 애플리케이션에서 사용할 아이콘을 제공합니다.
/// </summary>
internal static class AppIcon
{
    /// <summary>
    /// 실행 파일에 포함된 애플리케이션 아이콘을 로드합니다.
    /// </summary>
    public static Icon Load()
    {
        try
        {
            var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            if (icon is not null)
            {
                return icon;
            }
        }
        catch
        {
            // 아이콘 추출에 실패하면 Windows 기본 애플리케이션 아이콘을 사용합니다.
        }

        return new Icon(SystemIcons.Application, SystemIcons.Application.Size);
    }
}
