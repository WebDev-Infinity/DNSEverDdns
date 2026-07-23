using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace DNSEverDdns.Linux;

/// <summary>
/// 현재 Linux 사용자의 systemd 서비스 등록과 해제를 관리합니다.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class SystemdServiceManager
{
    private const string ServiceFileName = "dnsever-ddns.service";

    /// <summary>
    /// 현재 실행 파일을 systemd 사용자 서비스로 등록하고 즉시 시작합니다.
    /// </summary>
    public async Task InstallAsync(CancellationToken cancellationToken = default)
    {
        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("현재 실행 파일 경로를 확인할 수 없습니다.");
        var serviceDirectoryPath = ResolveServiceDirectoryPath();
        var serviceFilePath = Path.Combine(serviceDirectoryPath, ServiceFileName);

        Directory.CreateDirectory(serviceDirectoryPath);
        File.SetUnixFileMode(
            serviceDirectoryPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var serviceText = BuildServiceFile(executablePath);
        File.WriteAllText(serviceFilePath, serviceText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.SetUnixFileMode(serviceFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        await RunSystemctlAsync(new[] { "--user", "daemon-reload" }, requireSuccess: true, cancellationToken);
        await RunSystemctlAsync(
            new[] { "--user", "enable", "--now", ServiceFileName },
            requireSuccess: true,
            cancellationToken);

        Console.WriteLine($"systemd 사용자 서비스를 등록했습니다: {serviceFilePath}");
    }

    /// <summary>
    /// systemd 사용자 서비스를 중지하고 등록 파일을 제거합니다.
    /// </summary>
    public async Task UninstallAsync(CancellationToken cancellationToken = default)
    {
        var serviceFilePath = Path.Combine(ResolveServiceDirectoryPath(), ServiceFileName);

        await RunSystemctlAsync(
            new[] { "--user", "disable", "--now", ServiceFileName },
            requireSuccess: false,
            cancellationToken);

        if (File.Exists(serviceFilePath))
        {
            File.Delete(serviceFilePath);
        }

        await RunSystemctlAsync(new[] { "--user", "daemon-reload" }, requireSuccess: true, cancellationToken);
        Console.WriteLine("systemd 사용자 서비스를 해제했습니다.");
    }

    /// <summary>
    /// systemd 사용자 서비스의 현재 상태를 출력합니다.
    /// </summary>
    public async Task ShowStatusAsync(CancellationToken cancellationToken = default)
    {
        await RunSystemctlAsync(
            new[] { "--user", "status", ServiceFileName, "--no-pager" },
            requireSuccess: false,
            cancellationToken);
    }

    /// <summary>
    /// 현재 실행 파일 경로를 포함하는 systemd 서비스 파일 내용을 생성합니다.
    /// </summary>
    private static string BuildServiceFile(string executablePath)
    {
        var escapedPath = executablePath
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

        return
            "[Unit]\n" +
            "Description=DNSEver DDNS Linux Client\n" +
            "After=network-online.target\n" +
            "Wants=network-online.target\n\n" +
            "[Service]\n" +
            "Type=simple\n" +
            $"ExecStart=\"{escapedPath}\" run\n" +
            "Restart=on-failure\n" +
            "RestartSec=30\n\n" +
            "[Install]\n" +
            "WantedBy=default.target\n";
    }

    /// <summary>
    /// XDG_CONFIG_HOME 또는 사용자 홈을 기준으로 systemd 사용자 서비스 경로를 결정합니다.
    /// </summary>
    private static string ResolveServiceDirectoryPath()
    {
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

        if (!string.IsNullOrWhiteSpace(xdgConfigHome))
        {
            return Path.Combine(xdgConfigHome, "systemd", "user");
        }

        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(homeDirectory))
        {
            throw new InvalidOperationException("Linux 사용자 홈 디렉터리를 확인할 수 없습니다.");
        }

        return Path.Combine(homeDirectory, ".config", "systemd", "user");
    }

    /// <summary>
    /// systemctl 명령을 실행하고 출력을 현재 콘솔에 전달합니다.
    /// </summary>
    private static async Task<int> RunSystemctlAsync(
        IEnumerable<string> arguments,
        bool requireSuccess,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "systemctl",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("systemctl 프로세스를 시작할 수 없습니다.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            Console.Write(standardOutput);
        }

        if (!string.IsNullOrWhiteSpace(standardError))
        {
            Console.Error.Write(standardError);
        }

        if (requireSuccess && process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"systemctl 명령이 종료 코드 {process.ExitCode}로 실패했습니다.");
        }

        return process.ExitCode;
    }
}
