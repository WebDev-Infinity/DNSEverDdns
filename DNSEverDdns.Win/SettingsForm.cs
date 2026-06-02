using DNSEverDdns.Core;

namespace DNSEverDdns.Win;

/// <summary>
/// DNSEver 접속 정보와 업데이트 대상을 편집하는 설정 화면입니다.
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly AppSettingsStore _settingsStore;
    private readonly TextBox _userIdTextBox = new();
    private readonly TextBox _authCodeTextBox = new();
    private readonly CheckedListBox _hostNamesCheckedListBox = new();
    private readonly TextBox _currentIpTextBox = new();
    private readonly TextBox _overrideIpTextBox = new();
    private readonly NumericUpDown _intervalNumericUpDown = new();
    private readonly CheckBox _darkThemeCheckBox = new();
    private readonly Label _statusLabel = new();

    /// <summary>
    /// 설정이 저장되었을 때 발생합니다.
    /// </summary>
    public event EventHandler<AppSettings>? SettingsSaved;

    /// <summary>
    /// 저장소와 현재 설정을 사용하여 화면을 초기화합니다.
    /// </summary>
    public SettingsForm(AppSettingsStore settingsStore, AppSettings settings)
    {
        _settingsStore = settingsStore;
        BuildLayout();
        LoadSettings(settings);
        ApplyTheme(settings.UseDarkTheme);
    }

    /// <summary>
    /// 설정 화면의 상태 메시지를 변경합니다.
    /// </summary>
    public void SetStatus(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(message));
            return;
        }

        _statusLabel.Text = message;
    }

    /// <summary>
    /// 설정 화면의 컨트롤 배치를 구성합니다.
    /// </summary>
    private void BuildLayout()
    {
        Text = "DNSEver DDNS 설정";
        Width = 670;
        Height = 610;
        MinimumSize = new Size(650, 590);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(CreateHeader(), 0, 0);
        root.Controls.Add(CreateSettingsPanel(), 0, 1);
        root.Controls.Add(CreateButtonPanel(), 0, 2);
        Controls.Add(root);
    }

    /// <summary>
    /// 화면 상단 헤더 영역을 생성합니다.
    /// </summary>
    private Control CreateHeader()
    {
        return new Label
        {
            Text = "DNSEver DDNS",
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 14)
        };
    }

    /// <summary>
    /// 설정 입력 영역을 생성합니다.
    /// </summary>
    private Control CreateSettingsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _authCodeTextBox.UseSystemPasswordChar = true;
        _hostNamesCheckedListBox.CheckOnClick = true;
        _hostNamesCheckedListBox.Height = 110;
        _currentIpTextBox.ReadOnly = true;
        _currentIpTextBox.Height = 28;
        _overrideIpTextBox.Height = 28;
        _userIdTextBox.Height = 28;
        _authCodeTextBox.Height = 28;
        _intervalNumericUpDown.Minimum = 1;
        _intervalNumericUpDown.Maximum = 1440;
        _intervalNumericUpDown.Height = 28;

        AddRow(panel, 0, "아이디", _userIdTextBox);
        AddRow(panel, 1, "인증 코드", _authCodeTextBox);
        AddRow(panel, 2, "호스트", _hostNamesCheckedListBox);
        AddRow(panel, 3, "현재 IP", CreateCurrentIpPanel(), 46);
        AddRow(panel, 4, "지정 IP", _overrideIpTextBox);
        AddRow(panel, 5, "주기(분)", _intervalNumericUpDown);

        _darkThemeCheckBox.Text = "어두운 테마 사용";
        _darkThemeCheckBox.AutoSize = true;
        _darkThemeCheckBox.Dock = DockStyle.Top;
        _darkThemeCheckBox.Margin = new Padding(0, 6, 0, 3);
        _darkThemeCheckBox.CheckedChanged += (_, _) => ApplyTheme(_darkThemeCheckBox.Checked);
        panel.Controls.Add(_darkThemeCheckBox, 1, 6);

        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(0, 12, 0, 0);
        panel.Controls.Add(_statusLabel, 1, 7);

        return panel;
    }

    /// <summary>
    /// 현재 나가는 공인 IP 조회 영역을 생성합니다.
    /// </summary>
    private Control CreateCurrentIpPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146));

        var lookupButton = new Button { Text = "IP 조회", Dock = DockStyle.Fill, Width = 112, Height = 38 };
        var applyButton = new Button { Text = "지정 IP 적용", Dock = DockStyle.Fill, Width = 146, Height = 38 };

        lookupButton.Click += async (_, _) => await LookupCurrentIpAsync(applyToOverride: false);
        applyButton.Click += async (_, _) => await LookupCurrentIpAsync(applyToOverride: true);

        _currentIpTextBox.Dock = DockStyle.Fill;
        panel.Controls.Add(_currentIpTextBox, 0, 0);
        panel.Controls.Add(lookupButton, 1, 0);
        panel.Controls.Add(applyButton, 2, 0);

        return panel;
    }

    /// <summary>
    /// 설정 행을 입력 패널에 추가합니다.
    /// </summary>
    private static void AddRow(TableLayoutPanel panel, int row, string labelText, Control inputControl, int rowHeight = 36)
    {
        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft
        };

        inputControl.Dock = row == 2 ? DockStyle.Fill : DockStyle.Top;
        inputControl.Margin = new Padding(0, 3, 0, 3);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 2 ? 120 : rowHeight));
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(inputControl, 1, row);
    }

    /// <summary>
    /// 하단 버튼 영역을 생성합니다.
    /// </summary>
    private Control CreateButtonPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 16, 0, 0)
        };

        var saveButton = new Button { Text = "저장", Width = 112, Height = 34 };
        var testButton = new Button { Text = "호스트 조회", Width = 146, Height = 34 };
        var closeButton = new Button { Text = "닫기", Width = 112, Height = 34 };

        saveButton.Click += (_, _) => SaveSettings();
        testButton.Click += async (_, _) => await TestHostsAsync();
        closeButton.Click += (_, _) => Close();

        panel.Controls.Add(saveButton);
        panel.Controls.Add(testButton);
        panel.Controls.Add(closeButton);

        return panel;
    }

    /// <summary>
    /// 저장된 설정 값을 화면에 반영합니다.
    /// </summary>
    private void LoadSettings(AppSettings settings)
    {
        _userIdTextBox.Text = settings.UserId;
        _authCodeTextBox.Text = SafeUnprotect(settings.ProtectedAuthCode);
        LoadHostChecklist(settings.AvailableHostNames, settings.SelectedHostNames);
        _currentIpTextBox.Text = string.Empty;
        _overrideIpTextBox.Text = settings.OverrideIpAddress;
        _intervalNumericUpDown.Value = Math.Clamp(settings.UpdateIntervalMinutes, 1, 1440);
        _darkThemeCheckBox.Checked = settings.UseDarkTheme;
    }

    /// <summary>
    /// 현재 입력 값을 설정 파일에 저장합니다.
    /// </summary>
    private void SaveSettings()
    {
        var settings = CreateSettingsFromInput();

        if (string.IsNullOrWhiteSpace(settings.UserId) || string.IsNullOrWhiteSpace(_authCodeTextBox.Text))
        {
            _statusLabel.Text = "아이디와 DDNS 인증 코드를 입력하세요.";
            return;
        }

        if (settings.HostNames.Count == 0)
        {
            _statusLabel.Text = "업데이트할 호스트를 하나 이상 선택하세요.";
            return;
        }

        _settingsStore.Save(settings);
        SettingsSaved?.Invoke(this, settings);
        _statusLabel.Text = "설정이 저장되었습니다.";
    }

    /// <summary>
    /// 현재 입력 값으로 DNSEver 호스트 목록 조회를 테스트합니다.
    /// </summary>
    private async Task TestHostsAsync()
    {
        try
        {
            _statusLabel.Text = "호스트를 조회하는 중입니다...";
            using var httpClient = new HttpClient();
            var client = new DnseverClient(httpClient);
            var credentials = new DnseverCredentials(_userIdTextBox.Text.Trim(), _authCodeTextBox.Text.Trim());
            var hosts = await client.GetHostsAsync(credentials);

            LoadHostChecklist(hosts.Select(host => host.Name), hosts.Select(host => host.Name));
            _statusLabel.Text = $"{hosts.Count}개 호스트를 조회했습니다.";
        }
        catch (DnseverApiException ex)
        {
            _statusLabel.Text = ex.Message;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    /// <summary>
    /// DNSEver 기준으로 현재 PC의 최종 외부 공인 IP를 조회합니다.
    /// </summary>
    private async Task LookupCurrentIpAsync(bool applyToOverride)
    {
        try
        {
            _statusLabel.Text = "현재 나가는 공인 IP를 조회하는 중입니다...";
            using var httpClient = new HttpClient();
            var client = new DnseverClient(httpClient);
            var currentIp = await client.GetCurrentIpAsync();

            _currentIpTextBox.Text = currentIp;

            if (applyToOverride)
            {
                _overrideIpTextBox.Text = currentIp;
                _statusLabel.Text = $"현재 IP {currentIp}를 지정 IP에 적용했습니다.";
                return;
            }

            _statusLabel.Text = $"현재 나가는 공인 IP는 {currentIp}입니다.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    /// <summary>
    /// 화면 입력 값으로 설정 객체를 생성합니다.
    /// </summary>
    private AppSettings CreateSettingsFromInput()
    {
        var availableHostNames = _hostNamesCheckedListBox.Items
            .Cast<string>()
            .Select(hostName => hostName.Trim())
            .Where(hostName => !string.IsNullOrWhiteSpace(hostName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var selectedHostNames = _hostNamesCheckedListBox.CheckedItems
            .Cast<string>()
            .Select(hostName => hostName.Trim())
            .Where(hostName => !string.IsNullOrWhiteSpace(hostName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AppSettings
        {
            UserId = _userIdTextBox.Text.Trim(),
            ProtectedAuthCode = _settingsStore.ProtectAuthCode(_authCodeTextBox.Text.Trim()),
            AvailableHostNames = availableHostNames,
            SelectedHostNames = selectedHostNames,
            OverrideIpAddress = _overrideIpTextBox.Text.Trim(),
            UpdateIntervalMinutes = (int)_intervalNumericUpDown.Value,
            UseDarkTheme = _darkThemeCheckBox.Checked
        };
    }

    /// <summary>
    /// 호스트 체크 목록에 전체 호스트와 선택 상태를 반영합니다.
    /// </summary>
    private void LoadHostChecklist(IEnumerable<string> availableHostNames, IEnumerable<string> selectedHostNames)
    {
        var selectedSet = selectedHostNames
            .Where(hostName => !string.IsNullOrWhiteSpace(hostName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hostNames = availableHostNames
            .Concat(selectedSet)
            .Select(hostName => hostName.Trim())
            .Where(hostName => !string.IsNullOrWhiteSpace(hostName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _hostNamesCheckedListBox.Items.Clear();

        foreach (var hostName in hostNames)
        {
            _hostNamesCheckedListBox.Items.Add(hostName, selectedSet.Count == 0 || selectedSet.Contains(hostName));
        }
    }

    /// <summary>
    /// 저장된 인증 코드를 안전하게 복호화합니다.
    /// </summary>
    private string SafeUnprotect(string protectedAuthCode)
    {
        try
        {
            return _settingsStore.UnprotectAuthCode(protectedAuthCode);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 설정 화면에 밝은 테마 또는 어두운 테마를 적용합니다.
    /// </summary>
    private void ApplyTheme(bool useDarkTheme)
    {
        var backColor = useDarkTheme ? Color.FromArgb(32, 35, 39) : Color.White;
        var foreColor = useDarkTheme ? Color.FromArgb(238, 241, 245) : Color.FromArgb(30, 30, 30);
        var inputBackColor = useDarkTheme ? Color.FromArgb(47, 51, 57) : Color.White;

        BackColor = backColor;
        ForeColor = foreColor;
        ApplyThemeToControls(Controls, backColor, foreColor, inputBackColor);
    }

    /// <summary>
    /// 하위 컨트롤에 테마 색상을 재귀적으로 적용합니다.
    /// </summary>
    private static void ApplyThemeToControls(Control.ControlCollection controls, Color backColor, Color foreColor, Color inputBackColor)
    {
        foreach (Control control in controls)
        {
            control.ForeColor = foreColor;
            control.BackColor = control is TextBox or NumericUpDown or CheckedListBox ? inputBackColor : backColor;

            if (control.HasChildren)
            {
                ApplyThemeToControls(control.Controls, backColor, foreColor, inputBackColor);
            }
        }
    }
}
