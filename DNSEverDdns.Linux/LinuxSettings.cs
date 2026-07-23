namespace DNSEverDdns.Linux;

/// <summary>
/// Linux DNSEver DDNS 클라이언트의 사용자 설정을 표현합니다.
/// </summary>
public sealed class LinuxSettings
{
    /// <summary>
    /// 기본값으로 Linux 설정을 초기화합니다.
    /// </summary>
    public LinuxSettings()
    {
    }

    /// <summary>
    /// DNSEver 사용자 아이디입니다.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// DNSEver 다이나믹 DNS 인증 코드입니다.
    /// </summary>
    public string AuthCode { get; set; } = string.Empty;

    /// <summary>
    /// DDNS 업데이트 대상 호스트 목록입니다.
    /// </summary>
    public List<string> HostNames { get; set; } = new();

    /// <summary>
    /// 강제로 지정할 IP 주소입니다. 비어 있으면 DNSEver가 감지한 공인 IP를 사용합니다.
    /// </summary>
    public string OverrideIpAddress { get; set; } = string.Empty;

    /// <summary>
    /// DDNS 업데이트 주기(분)입니다.
    /// </summary>
    public int UpdateIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// 필수 설정값이 모두 입력되어 있는지 반환합니다.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(UserId)
        && !string.IsNullOrWhiteSpace(AuthCode)
        && HostNames.Count > 0;
}
