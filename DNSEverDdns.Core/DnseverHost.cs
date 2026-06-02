namespace DNSEverDdns.Core;

/// <summary>
/// DNSEver에서 반환한 DDNS 호스트 정보를 표현합니다.
/// </summary>
/// <param name="Name">호스트 이름입니다.</param>
/// <param name="Status">호스트 상태입니다.</param>
/// <param name="IpAddress">현재 등록된 IP 주소입니다.</param>
public sealed record DnseverHost(string Name, string Status, string IpAddress);
