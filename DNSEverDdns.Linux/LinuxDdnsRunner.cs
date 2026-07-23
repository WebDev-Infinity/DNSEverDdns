using DNSEverDdns.Core;

namespace DNSEverDdns.Linux;

/// <summary>
/// Linux 환경에서 DNSEver DDNS 업데이트를 실행합니다.
/// </summary>
public sealed class LinuxDdnsRunner
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 재사용할 HTTP 클라이언트로 실행기를 초기화합니다.
    /// </summary>
    public LinuxDdnsRunner(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 현재 설정으로 DDNS 업데이트를 한 번 실행합니다.
    /// </summary>
    public async Task<bool> RunOnceAsync(LinuxSettings settings, CancellationToken cancellationToken = default)
    {
        EnsureConfigured(settings);

        var client = new DnseverClient(_httpClient);
        var credentials = new DnseverCredentials(settings.UserId, settings.AuthCode);
        var overrideIp = string.IsNullOrWhiteSpace(settings.OverrideIpAddress)
            ? null
            : settings.OverrideIpAddress.Trim();
        var result = await client.UpdateAsync(credentials, settings.HostNames, overrideIp, cancellationToken);

        Console.WriteLine(
            $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} " +
            $"[{result.Code}] {result.Message}");

        foreach (var host in result.Hosts)
        {
            Console.WriteLine($"  {host.Name}: [{host.Code}] {host.Message}");
        }

        return result.IsSuccess;
    }

    /// <summary>
    /// 설정된 주기에 따라 취소될 때까지 DDNS 업데이트를 반복합니다.
    /// </summary>
    public async Task RunContinuouslyAsync(LinuxSettings settings, CancellationToken cancellationToken)
    {
        EnsureConfigured(settings);
        var interval = TimeSpan.FromMinutes(Math.Max(1, settings.UpdateIntervalMinutes));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(settings, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} DDNS 업데이트 실패: {ex.Message}");
            }

            await Task.Delay(interval, cancellationToken);
        }
    }

    /// <summary>
    /// 업데이트 실행에 필요한 설정이 준비되어 있는지 확인합니다.
    /// </summary>
    private static void EnsureConfigured(LinuxSettings settings)
    {
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException(
                "설정이 완료되지 않았습니다. 먼저 'configure' 명령을 실행하세요.");
        }
    }
}
