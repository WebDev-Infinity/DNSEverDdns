using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace DNSEverDdns.Core;

/// <summary>
/// DNSEver 다이나믹 DNS API를 호출합니다.
/// </summary>
public sealed class DnseverClient
{
    private readonly HttpClient _httpClient;
    private readonly DnseverApiOptions _options;

    /// <summary>
    /// HTTP 클라이언트와 API 옵션을 사용하여 클라이언트를 초기화합니다.
    /// </summary>
    public DnseverClient(HttpClient httpClient, DnseverApiOptions? options = null)
    {
        _httpClient = httpClient;
        _options = options ?? new DnseverApiOptions();
    }

    /// <summary>
    /// DNSEver 서버가 인식하는 현재 공인 IP 주소를 조회합니다.
    /// </summary>
    public async Task<string> GetCurrentIpAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(_options.IpUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return content.Trim();
    }

    /// <summary>
    /// DNSEver 계정에 등록된 DDNS 호스트 목록을 조회합니다.
    /// </summary>
    public async Task<IReadOnlyList<DnseverHost>> GetHostsAsync(DnseverCredentials credentials, CancellationToken cancellationToken = default)
    {
        var document = await ExecuteXmlAsync(_options.HostUrl, credentials, cancellationToken);
        var hosts = document.Root?
            .Element("result")?
            .Elements("host")
            .Select(element => new DnseverHost(
                ReadAttribute(element, "name"),
                ReadAttribute(element, "status"),
                ReadAttribute(element, "ip")))
            .ToArray();

        return hosts ?? Array.Empty<DnseverHost>();
    }

    /// <summary>
    /// 지정한 호스트들의 IP 주소를 DNSEver에 업데이트합니다.
    /// </summary>
    public async Task<DnseverResult> UpdateAsync(DnseverCredentials credentials, IReadOnlyList<string> hostNames, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        if (hostNames.Count == 0)
        {
            throw new ArgumentException("업데이트할 호스트가 없습니다.", nameof(hostNames));
        }

        var url = BuildUpdateUri(hostNames, ipAddress);
        var document = await ExecuteXmlAsync(url, credentials, cancellationToken);
        return ParseResult(document);
    }

    /// <summary>
    /// Basic 인증을 포함하여 XML 응답 API를 호출합니다.
    /// </summary>
    private async Task<XDocument> ExecuteXmlAsync(Uri url, DnseverCredentials credentials, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = BuildBasicAuthenticationHeader(credentials);
        request.Headers.UserAgent.ParseAdd("DDNS Client");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateApiException(response.StatusCode, content);
        }

        return XDocument.Parse(content);
    }

    /// <summary>
    /// DNSEver 업데이트 API 호출 주소를 생성합니다.
    /// </summary>
    private Uri BuildUpdateUri(IReadOnlyList<string> hostNames, string? ipAddress)
    {
        var builder = new UriBuilder(_options.UpdateUrl);
        var query = new StringBuilder();

        foreach (var hostName in hostNames.Where(hostName => !string.IsNullOrWhiteSpace(hostName)))
        {
            if (query.Length > 0)
            {
                query.Append('&');
            }

            query.Append("host[");
            query.Append(Uri.EscapeDataString(hostName.Trim()));
            query.Append(']');

            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                query.Append('=');
                query.Append(Uri.EscapeDataString(ipAddress.Trim()));
            }
        }

        builder.Query = query.ToString();
        return builder.Uri;
    }

    /// <summary>
    /// DNSEver Basic 인증 헤더를 생성합니다.
    /// </summary>
    private static AuthenticationHeaderValue BuildBasicAuthenticationHeader(DnseverCredentials credentials)
    {
        var raw = $"{credentials.UserId}:{credentials.AuthCode}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    /// <summary>
    /// DNSEver HTTP 오류 응답을 사용자 친화적인 API 예외로 변환합니다.
    /// </summary>
    private static DnseverApiException CreateApiException(System.Net.HttpStatusCode statusCode, string responseBody)
    {
        var message = statusCode == System.Net.HttpStatusCode.Unauthorized
            ? "DNSEver 인증에 실패했습니다. 아이디와 다이나믹 DNS 인증 코드를 확인하세요. 로그인 비밀번호가 아니라 DDNS 인증 코드가 필요합니다."
            : $"DNSEver 서버 응답 오류: {(int)statusCode} {statusCode}";

        return new DnseverApiException(statusCode, message, responseBody);
    }

    /// <summary>
    /// DNSEver XML 결과를 도메인 결과 객체로 변환합니다.
    /// </summary>
    private static DnseverResult ParseResult(XDocument document)
    {
        var result = document.Root?.Element("result")
            ?? throw new InvalidOperationException("DNSEver 응답에서 result 요소를 찾을 수 없습니다.");

        var hosts = result.Elements("host")
            .Select(element => new DnseverResultHost(
                ReadAttribute(element, "name"),
                ReadAttribute(element, "code"),
                ReadAttribute(element, "msg")))
            .ToArray();

        return new DnseverResult(
            ReadAttribute(result, "type"),
            ReadAttribute(result, "code"),
            ReadAttribute(result, "msg"),
            hosts);
    }

    /// <summary>
    /// XML 요소의 속성 값을 안전하게 읽습니다.
    /// </summary>
    private static string ReadAttribute(XElement element, string name)
    {
        return element.Attribute(name)?.Value ?? string.Empty;
    }
}
