using System.IO.Pipes;

namespace DNSEverDdns.Win;

/// <summary>
/// Windows Forms 애플리케이션의 진입점을 제공합니다.
/// </summary>
static class Program
{
    private const string SingleInstanceMutexName = @"Local\DNSEverDdns.Win.SingleInstance";
    private const string ActivationPipeName = "DNSEverDdns.Win.ActivationPipe";

    /// <summary>
    /// 애플리케이션을 초기화하고 트레이 기반 실행 컨텍스트를 시작합니다.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        using var singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var isFirstInstance);

        ApplicationConfiguration.Initialize();

        if (!isFirstInstance)
        {
            if (!TryActivateRunningInstance())
            {
                MessageBox.Show(
                    "DNSEver DDNS가 이미 실행 중입니다.",
                    "DNSEver DDNS",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return;
        }

        Application.Run(new DdnsApplicationContext(IsTrayStart(args)));
    }    

    /// <summary>
    /// 이미 실행 중인 인스턴스에 설정 창 표시 명령을 전송합니다.
    /// </summary>
    private static bool TryActivateRunningInstance()
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", ActivationPipeName, PipeDirection.Out);
            pipeClient.Connect(800);

            using var writer = new StreamWriter(pipeClient) { AutoFlush = true };
            writer.WriteLine("show");
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 트레이 전용 시작 옵션이 지정되었는지 확인합니다.
    /// </summary>
    private static bool IsTrayStart(string[] args)
    {
        return args.Any(arg => string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase));
    }
}
