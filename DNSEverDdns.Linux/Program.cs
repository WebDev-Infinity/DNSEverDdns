using DNSEverDdns.Core;
using System.Runtime.Versioning;

namespace DNSEverDdns.Linux;

/// <summary>
/// DNSEver DDNS Linux 명령줄 클라이언트의 진입점을 제공합니다.
/// </summary>
[SupportedOSPlatform("linux")]
internal static class Program
{
    /// <summary>
    /// 명령줄 인자를 해석하여 요청한 Linux 클라이언트 기능을 실행합니다.
    /// </summary>
    private static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsLinux())
        {
            Console.Error.WriteLine("이 프로그램은 Linux에서만 실행할 수 있습니다.");
            return 1;
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        try
        {
            var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
            var settingsStore = new LinuxSettingsStore();

            return command switch
            {
                "configure" => await ConfigureAsync(settingsStore, cancellationTokenSource.Token),
                "update" => await UpdateAsync(settingsStore, cancellationTokenSource.Token),
                "run" => await RunAsync(settingsStore, cancellationTokenSource.Token),
                "status" => ShowStatus(settingsStore),
                "install-service" => await InstallServiceAsync(cancellationTokenSource.Token),
                "uninstall-service" => await UninstallServiceAsync(cancellationTokenSource.Token),
                "service-status" => await ShowServiceStatusAsync(cancellationTokenSource.Token),
                "help" or "--help" or "-h" => ShowHelp(),
                _ => ShowUnknownCommand(command)
            };
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"오류: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// 사용자 입력과 DNSEver 호스트 조회 결과로 설정 파일을 생성합니다.
    /// </summary>
    private static async Task<int> ConfigureAsync(
        LinuxSettingsStore settingsStore,
        CancellationToken cancellationToken)
    {
        var currentSettings = settingsStore.Load();
        var userId = ReadValue("DNSEver 아이디", currentSettings.UserId, required: true);
        var authCode = ReadSecret("DDNS 인증 코드", currentSettings.AuthCode);

        using var httpClient = new HttpClient();
        var client = new DnseverClient(httpClient);
        var credentials = new DnseverCredentials(userId, authCode);
        var hosts = await client.GetHostsAsync(credentials, cancellationToken);

        if (hosts.Count == 0)
        {
            throw new InvalidOperationException("DNSEver에서 사용 가능한 DDNS 호스트를 찾지 못했습니다.");
        }

        Console.WriteLine();
        Console.WriteLine("사용 가능한 호스트:");

        for (var index = 0; index < hosts.Count; index++)
        {
            var host = hosts[index];
            var selectedMark = currentSettings.HostNames.Contains(
                host.Name,
                StringComparer.OrdinalIgnoreCase)
                ? "*"
                : " ";
            Console.WriteLine($"  {index + 1,2}. [{selectedMark}] {host.Name} ({host.IpAddress})");
        }

        var defaultSelection = string.Join(
            ",",
            hosts
                .Select((host, index) => new { host.Name, Number = index + 1 })
                .Where(item => currentSettings.HostNames.Contains(
                    item.Name,
                    StringComparer.OrdinalIgnoreCase))
                .Select(item => item.Number));
        var selectionText = ReadValue(
            "업데이트할 호스트 번호(쉼표 구분)",
            defaultSelection,
            required: true);
        var selectedHostNames = ParseHostSelection(selectionText, hosts);
        var overrideIpAddress = ReadValue(
            "지정 IP(자동 감지는 비워 둠)",
            currentSettings.OverrideIpAddress,
            required: false);
        var intervalText = ReadValue(
            "업데이트 주기(분)",
            Math.Max(1, currentSettings.UpdateIntervalMinutes).ToString(),
            required: true);

        if (!int.TryParse(intervalText, out var intervalMinutes) || intervalMinutes < 1)
        {
            throw new InvalidOperationException("업데이트 주기는 1 이상의 정수여야 합니다.");
        }

        var settings = new LinuxSettings
        {
            UserId = userId,
            AuthCode = authCode,
            HostNames = selectedHostNames,
            OverrideIpAddress = overrideIpAddress,
            UpdateIntervalMinutes = intervalMinutes
        };

        settingsStore.Save(settings);
        Console.WriteLine($"설정을 저장했습니다: {settingsStore.SettingsFilePath}");
        return 0;
    }

    /// <summary>
    /// 저장된 설정으로 DDNS 업데이트를 한 번 실행합니다.
    /// </summary>
    private static async Task<int> UpdateAsync(
        LinuxSettingsStore settingsStore,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var runner = new LinuxDdnsRunner(httpClient);
        var succeeded = await runner.RunOnceAsync(settingsStore.Load(), cancellationToken);
        return succeeded ? 0 : 1;
    }

    /// <summary>
    /// 저장된 주기에 따라 DDNS 업데이트를 반복 실행합니다.
    /// </summary>
    private static async Task<int> RunAsync(
        LinuxSettingsStore settingsStore,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var runner = new LinuxDdnsRunner(httpClient);
        await runner.RunContinuouslyAsync(settingsStore.Load(), cancellationToken);
        return 0;
    }

    /// <summary>
    /// 인증 코드를 제외한 현재 설정을 콘솔에 표시합니다.
    /// </summary>
    private static int ShowStatus(LinuxSettingsStore settingsStore)
    {
        var settings = settingsStore.Load();

        Console.WriteLine($"설정 파일: {settingsStore.SettingsFilePath}");
        Console.WriteLine($"설정 완료: {(settings.IsConfigured ? "예" : "아니요")}");
        Console.WriteLine($"아이디: {settings.UserId}");
        Console.WriteLine($"호스트: {string.Join(", ", settings.HostNames)}");
        Console.WriteLine(
            $"지정 IP: {(string.IsNullOrWhiteSpace(settings.OverrideIpAddress) ? "자동 감지" : settings.OverrideIpAddress)}");
        Console.WriteLine($"업데이트 주기: {settings.UpdateIntervalMinutes}분");
        return settings.IsConfigured ? 0 : 1;
    }

    /// <summary>
    /// 현재 실행 파일을 systemd 사용자 서비스로 등록합니다.
    /// </summary>
    private static async Task<int> InstallServiceAsync(CancellationToken cancellationToken)
    {
        var settingsStore = new LinuxSettingsStore();

        if (!settingsStore.Load().IsConfigured)
        {
            throw new InvalidOperationException(
                "설정이 완료되지 않았습니다. 먼저 'configure' 명령을 실행하세요.");
        }

        if (string.Equals(
            Path.GetFileNameWithoutExtension(Environment.ProcessPath),
            "dotnet",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "systemd 서비스는 게시된 Linux 실행 파일에서 등록해야 합니다.");
        }

        var manager = new SystemdServiceManager();
        await manager.InstallAsync(cancellationToken);
        return 0;
    }

    /// <summary>
    /// systemd 사용자 서비스를 중지하고 등록을 해제합니다.
    /// </summary>
    private static async Task<int> UninstallServiceAsync(CancellationToken cancellationToken)
    {
        var manager = new SystemdServiceManager();
        await manager.UninstallAsync(cancellationToken);
        return 0;
    }

    /// <summary>
    /// systemd 사용자 서비스 상태를 표시합니다.
    /// </summary>
    private static async Task<int> ShowServiceStatusAsync(CancellationToken cancellationToken)
    {
        var manager = new SystemdServiceManager();
        await manager.ShowStatusAsync(cancellationToken);
        return 0;
    }

    /// <summary>
    /// 대화형 입력값을 읽고 기본값과 필수값 규칙을 적용합니다.
    /// </summary>
    private static string ReadValue(string label, string defaultValue, bool required)
    {
        while (true)
        {
            var defaultText = string.IsNullOrWhiteSpace(defaultValue) ? string.Empty : $" [{defaultValue}]";
            Console.Write($"{label}{defaultText}: ");
            var value = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                value = defaultValue;
            }

            if (!required || !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            Console.WriteLine("값을 입력해야 합니다.");
        }
    }

    /// <summary>
    /// 인증 코드를 화면에 노출하지 않고 입력받습니다.
    /// </summary>
    private static string ReadSecret(string label, string currentValue)
    {
        if (Console.IsInputRedirected)
        {
            return ReadValue(label, currentValue, required: true);
        }

        Console.Write(
            string.IsNullOrWhiteSpace(currentValue)
                ? $"{label}: "
                : $"{label} (기존 값을 유지하려면 Enter): ");
        var characters = new List<char>();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0)
                {
                    characters.RemoveAt(characters.Count - 1);
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                characters.Add(key.KeyChar);
                Console.Write('*');
            }
        }

        var value = new string(characters.ToArray());

        if (string.IsNullOrWhiteSpace(value))
        {
            value = currentValue;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("DDNS 인증 코드를 입력해야 합니다.");
        }

        return value;
    }

    /// <summary>
    /// 쉼표로 구분한 호스트 번호를 실제 호스트 이름 목록으로 변환합니다.
    /// </summary>
    private static List<string> ParseHostSelection(
        string selectionText,
        IReadOnlyList<DnseverHost> hosts)
    {
        var selectedHostNames = new List<string>();

        foreach (var token in selectionText.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(token.Trim(), out var number) || number < 1 || number > hosts.Count)
            {
                throw new InvalidOperationException($"올바르지 않은 호스트 번호입니다: {token.Trim()}");
            }

            var hostName = hosts[number - 1].Name;

            if (!selectedHostNames.Contains(hostName, StringComparer.OrdinalIgnoreCase))
            {
                selectedHostNames.Add(hostName);
            }
        }

        if (selectedHostNames.Count == 0)
        {
            throw new InvalidOperationException("업데이트할 호스트를 하나 이상 선택해야 합니다.");
        }

        return selectedHostNames;
    }

    /// <summary>
    /// 지원하는 명령과 사용법을 콘솔에 출력합니다.
    /// </summary>
    private static int ShowHelp()
    {
        Console.WriteLine("DNSEver DDNS Linux Client");
        Console.WriteLine();
        Console.WriteLine("사용법: DNSEverDdns <command>");
        Console.WriteLine();
        Console.WriteLine("  configure         계정, 호스트 및 업데이트 주기 설정");
        Console.WriteLine("  update            DDNS 업데이트 즉시 실행");
        Console.WriteLine("  run               설정된 주기로 계속 실행");
        Console.WriteLine("  status            저장된 설정 확인");
        Console.WriteLine("  install-service   systemd 사용자 서비스 등록 및 시작");
        Console.WriteLine("  uninstall-service systemd 사용자 서비스 중지 및 해제");
        Console.WriteLine("  service-status    systemd 사용자 서비스 상태 확인");
        Console.WriteLine("  help              도움말 표시");
        return 0;
    }

    /// <summary>
    /// 알 수 없는 명령을 안내하고 실패 종료 코드를 반환합니다.
    /// </summary>
    private static int ShowUnknownCommand(string command)
    {
        Console.Error.WriteLine($"알 수 없는 명령입니다: {command}");
        ShowHelp();
        return 1;
    }
}
