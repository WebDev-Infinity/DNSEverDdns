namespace DNSEverDdns.Core;

/// <summary>
/// DNSEver 다이나믹 DNS API 접속 주소를 보관합니다.
/// </summary>
public sealed class DnseverApiOptions
{
    /// <summary>
    /// 기본 API 접속 주소를 사용하여 옵션을 초기화합니다.
    /// </summary>
    public DnseverApiOptions()
    {
    }

    /// <summary>
    /// 현재 공인 IP 조회 주소입니다.
    /// </summary>
    public Uri IpUrl { get; set; } = new("http://dyna.dnsever.com/getip.php");

    /// <summary>
    /// 등록된 DDNS 호스트 목록 조회 주소입니다.
    /// </summary>
    public Uri HostUrl { get; set; } = new("http://dyna.dnsever.com/gethost.php");

    /// <summary>
    /// DDNS IP 업데이트 주소입니다.
    /// </summary>
    public Uri UpdateUrl { get; set; } = new("http://dyna.dnsever.com/update.php");
}
