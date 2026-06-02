namespace DNSEverDdns.Core;

/// <summary>
/// DNSEver XML 응답의 호스트별 결과 정보를 표현합니다.
/// </summary>
/// <param name="Name">호스트 이름입니다.</param>
/// <param name="Code">호스트별 결과 코드입니다.</param>
/// <param name="Message">호스트별 결과 메시지입니다.</param>
public sealed record DnseverResultHost(string Name, string Code, string Message)
{
    /// <summary>
    /// 호스트별 성공 코드 여부를 반환합니다.
    /// </summary>
    public bool IsSuccess => Code is "720" or "721";
}
