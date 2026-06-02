namespace DNSEverDdns.Win;

/// <summary>
/// Windows 클라이언트 실행 설정을 표현합니다.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// 기본 설정으로 새 인스턴스를 초기화합니다.
    /// </summary>
    public AppSettings()
    {
    }

    /// <summary>
    /// DNSEver 사용자 아이디입니다.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// DPAPI로 암호화된 DNSEver DDNS 인증 코드입니다.
    /// </summary>
    public string ProtectedAuthCode { get; set; } = string.Empty;

    /// <summary>
    /// DNSEver에서 조회했거나 사용자가 입력한 전체 호스트 이름 목록입니다.
    /// </summary>
    public List<string> AvailableHostNames { get; set; } = new();

    /// <summary>
    /// DDNS 업데이트 대상으로 선택된 호스트 이름 목록입니다.
    /// </summary>
    public List<string> SelectedHostNames { get; set; } = new();

    /// <summary>
    /// 이전 버전 설정과 호환하기 위한 선택 호스트 이름 목록입니다.
    /// </summary>
    public List<string> HostNames
    {
        get => SelectedHostNames;
        set
        {
            SelectedHostNames = value ?? new List<string>();

            if (AvailableHostNames.Count == 0)
            {
                AvailableHostNames = SelectedHostNames.ToList();
            }
        }
    }

    /// <summary>
    /// 강제로 지정할 IP 주소입니다. 비어 있으면 DNSEver가 감지한 현재 IP를 사용합니다.
    /// </summary>
    public string OverrideIpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 업데이트 주기(분)입니다.
    /// </summary>
    public int UpdateIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// 어두운 테마 사용 여부입니다.
    /// </summary>
    public bool UseDarkTheme { get; set; } = true;
}
