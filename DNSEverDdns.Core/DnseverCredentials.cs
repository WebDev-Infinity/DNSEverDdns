namespace DNSEverDdns.Core;

/// <summary>
/// DNSEver Basic 인증에 사용할 계정 정보를 표현합니다.
/// </summary>
/// <param name="UserId">DNSEver 사용자 아이디입니다.</param>
/// <param name="AuthCode">DNSEver DDNS 인증 코드입니다.</param>
public sealed record DnseverCredentials(string UserId, string AuthCode);
