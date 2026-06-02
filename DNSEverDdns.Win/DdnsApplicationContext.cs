using DNSEverDdns.Core;
using System.IO.Pipes;

namespace DNSEverDdns.Win;

/// <summary>
/// 트레이 아이콘과 주기 업데이트 루프를 관리하는 애플리케이션 컨텍스트입니다.
/// </summary>
public sealed class DdnsApplicationContext : ApplicationContext
{
    private const string ActivationPipeName = "DNSEverDdns.Win.ActivationPipe";

    private readonly AppLogger _logger = new();
    private readonly AppSettingsStore _settingsStore = new();
    private readonly HttpClient _httpClient = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly CancellationTokenSource _activationCancellationTokenSource = new();
    private readonly SynchronizationContext _uiContext;
    private AppSettings _settings;
    private SettingsForm? _settingsForm;
    private bool _isUpdating;

    /// <summary>
    /// 트레이 메뉴와 업데이트 타이머를 초기화합니다.
    /// </summary>
    public DdnsApplicationContext(bool startMinimizedToTray)
    {
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _settings = _settingsStore.Load();
        _notifyIcon = CreateNotifyIcon();
        _timer = CreateTimer();
        ApplyTimerInterval();
        _timer.Start();
        StartActivationPipeServer();

        if (!startMinimizedToTray || NeedsInitialSettings())
        {
            ShowSettings();
        }
    }

    /// <summary>
    /// 컨텍스트가 사용하는 리소스를 정리합니다.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activationCancellationTokenSource.Cancel();
            _activationCancellationTokenSource.Dispose();
            _timer.Dispose();
            _notifyIcon.Dispose();
            _httpClient.Dispose();
            _settingsForm?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// 트레이 아이콘과 컨텍스트 메뉴를 생성합니다.
    /// </summary>
    private NotifyIcon CreateNotifyIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("설정 열기", null, (_, _) => ShowSettings());
        menu.Items.Add("지금 업데이트", null, async (_, _) => await RunUpdateAsync(showBalloon: true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) => ExitApplication());

        var notifyIcon = new NotifyIcon
        {
            Icon = AppIcon.Load(),
            Text = "DNSEver DDNS",
            Visible = true,
            ContextMenuStrip = menu
        };

        notifyIcon.DoubleClick += (_, _) => ShowSettings();

        return notifyIcon;
    }

    /// <summary>
    /// 중복 실행된 프로세스의 창 표시 요청을 받을 named pipe 서버를 시작합니다.
    /// </summary>
    private void StartActivationPipeServer()
    {
        _ = Task.Run(() => RunActivationPipeServerAsync(_activationCancellationTokenSource.Token));
    }

    /// <summary>
    /// named pipe 요청을 반복해서 수신하고 설정 창 표시 명령을 처리합니다.
    /// </summary>
    private async Task RunActivationPipeServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var pipeServer = new NamedPipeServerStream(
                    ActivationPipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipeServer.WaitForConnectionAsync(cancellationToken);

                using var reader = new StreamReader(pipeServer);
                var command = await reader.ReadLineAsync(cancellationToken);

                if (string.Equals(command, "show", StringComparison.OrdinalIgnoreCase))
                {
                    _uiContext.Post(_ => ShowSettings(), null);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
        }
    }

    /// <summary>
    /// 주기 업데이트 타이머를 생성합니다.
    /// </summary>
    private System.Windows.Forms.Timer CreateTimer()
    {
        var timer = new System.Windows.Forms.Timer();
        timer.Tick += async (_, _) => await RunUpdateAsync(showBalloon: false);
        return timer;
    }

    /// <summary>
    /// 설정된 업데이트 주기를 타이머에 반영합니다.
    /// </summary>
    private void ApplyTimerInterval()
    {
        var minutes = Math.Max(1, _settings.UpdateIntervalMinutes);
        _timer.Interval = minutes * 60 * 1000;
    }

    /// <summary>
    /// 초기 설정 화면 표시가 필요한지 판단합니다.
    /// </summary>
    private bool NeedsInitialSettings()
    {
        return string.IsNullOrWhiteSpace(_settings.UserId)
            || string.IsNullOrWhiteSpace(_settings.ProtectedAuthCode)
            || _settings.SelectedHostNames.Count == 0;
    }

    /// <summary>
    /// 설정 화면을 표시하거나 기존 설정 화면을 앞으로 가져옵니다.
    /// </summary>
    private void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            if (_settingsForm.WindowState == FormWindowState.Minimized)
            {
                _settingsForm.WindowState = FormWindowState.Normal;
            }

            _settingsForm.Show();
            _settingsForm.Activate();
            _settingsForm.BringToFront();
            return;
        }

        _settingsForm = new SettingsForm(_settingsStore, _settings);
        _settingsForm.SettingsSaved += async (_, settings) =>
        {
            _settings = settings;
            ApplyTimerInterval();
            _logger.Info("설정이 저장되었습니다.");
            _settingsForm.SetStatus("설정을 저장했고 DDNS에 반영하는 중입니다...");
            var updated = await RunUpdateAsync(showBalloon: true);
            _settingsForm?.SetStatus(updated
                ? "설정 저장과 DDNS 반영을 완료했습니다."
                : "설정은 저장했지만 DDNS 반영은 실패했습니다. 로그와 알림 메시지를 확인하세요.");
        };
        _settingsForm.Show();
    }

    /// <summary>
    /// 현재 설정으로 DNSEver 업데이트를 실행합니다.
    /// </summary>
    private async Task<bool> RunUpdateAsync(bool showBalloon)
    {
        if (_isUpdating || NeedsInitialSettings())
        {
            return false;
        }

        _isUpdating = true;

        try
        {
            var authCode = _settingsStore.UnprotectAuthCode(_settings.ProtectedAuthCode);
            var credentials = new DnseverCredentials(_settings.UserId, authCode);
            var client = new DnseverClient(_httpClient);
            var overrideIp = string.IsNullOrWhiteSpace(_settings.OverrideIpAddress) ? null : _settings.OverrideIpAddress;
            var result = await client.UpdateAsync(credentials, _settings.SelectedHostNames, overrideIp);
            var hostSummary = string.Join(", ", result.Hosts.Select(host => $"{host.Name}:{host.Code}"));

            _logger.Info($"업데이트 결과: {result.Code} {result.Message} {hostSummary}");

            if (showBalloon)
            {
                ShowBalloon("DNSEver DDNS", $"{result.Code} {result.Message}");
            }

            return result.IsSuccess;
        }
        catch (DnseverApiException ex)
        {
            _logger.Error($"{(int)ex.StatusCode} {ex.StatusCode}: {ex.Message}");

            if (showBalloon)
            {
                ShowBalloon("DNSEver DDNS 인증 오류", ex.Message);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());

            if (showBalloon)
            {
                ShowBalloon("DNSEver DDNS 오류", ex.Message);
            }

            return false;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// 트레이 알림 풍선을 표시합니다.
    /// </summary>
    private void ShowBalloon(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(3000);
    }

    /// <summary>
    /// 애플리케이션을 종료합니다.
    /// </summary>
    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        ExitThread();
    }
}
