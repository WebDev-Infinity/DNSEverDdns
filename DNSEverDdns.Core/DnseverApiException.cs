using System.Net;

namespace DNSEverDdns.Core;

/// <summary>
/// DNSEver API 호출 중 발생한 서버 응답 오류를 표현합니다.
/// </summary>
public sealed class DnseverApiException : Exception
{
    /// <summary>
    /// HTTP 상태 코드와 오류 메시지를 사용하여 예외를 초기화합니다.
    /// </summary>
    public DnseverApiException(HttpStatusCode statusCode, string message, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// DNSEver 서버가 반환한 HTTP 상태 코드입니다.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// DNSEver 서버가 반환한 원본 응답 본문입니다.
    /// </summary>
    public string ResponseBody { get; }
}
