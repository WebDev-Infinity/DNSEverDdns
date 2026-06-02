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
    /// DNSEver 성공 코드 여부를 반환합니다.
    /// </summary>
    public bool IsSuccess => Code is "700" or "701";
}
