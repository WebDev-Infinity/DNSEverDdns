namespace DNSEverDdns.Core;

/// <summary>
/// DNSEver XML 응답의 최상위 결과 정보를 표현합니다.
/// </summary>
/// <param name="Type">응답 유형입니다.</param>
/// <param name="Code">결과 코드입니다.</param>
/// <param name="Message">결과 메시지입니다.</param>
/// <param name="Hosts">호스트별 결과 목록입니다.</param>
public sealed record DnseverResult(string Type, string Code, string Message, IReadOnlyList<DnseverResultHost> Hosts)
{
    /// <summary>
    /// DNSEver 요청이 성공했거나 모든 호스트가 이미 동일한 값으로 반영되어 있는지 반환합니다.
    /// </summary>
    public bool IsSuccess =>
        Code is "700" or "701"
        || (Hosts.Count > 0 && Hosts.All(host => host.Code is "720" or "721"));
}
