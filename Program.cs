using System.Diagnostics;
using System.Security.Cryptography;

namespace CheckHash;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private const string RepositoryUrl = "https://github.com/1847bell/HashChecker";

    private readonly FlowLayoutPanel algorithmPanel = new();
    private readonly Panel dropPanel = new();
    private readonly Label dropTitle = new();
    private readonly Label dropHint = new();
    private readonly ListBox resultList = new();
    private readonly Button chooseButton = new();
    private readonly Button clearButton = new();
    private readonly Button copyButton = new();
    private readonly Label statusLabel = new();
    private readonly RadioButton md5Radio = new();
    private readonly RadioButton sha1Radio = new();
    private readonly RadioButton sha256Radio = new();
    private readonly RadioButton sha384Radio = new();
    private readonly RadioButton sha512Radio = new();

    private bool isProcessing;

    public MainForm()
    {
        Text = "CheckHash - 文件校验工具";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 560);
        MinimumSize = new Size(600, 420);
        TopMost = true;
        Font = new Font("Microsoft YaHei UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);

        BuildLayout();
        WireEvents();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        var heading = new Label
        {
            Text = "文件哈希校验",
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 41, 55),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(heading, 0, 0);

        var algorithmLabel = new Label
        {
            Text = "校验算法",
            AutoSize = false,
            Size = new Size(84, 36),
            Margin = new Padding(0, 0, 8, 0),
            ForeColor = Color.FromArgb(75, 85, 99),
            TextAlign = ContentAlignment.MiddleLeft
        };

        algorithmPanel.Dock = DockStyle.Fill;
        algorithmPanel.WrapContents = false;
        algorithmPanel.FlowDirection = FlowDirection.LeftToRight;
        algorithmPanel.Padding = new Padding(0, 8, 0, 0);
        algorithmPanel.BackColor = Color.Transparent;
        algorithmPanel.Controls.Add(algorithmLabel);
        algorithmPanel.Controls.AddRange([md5Radio, sha1Radio, sha256Radio, sha384Radio, sha512Radio]);
        ConfigureRadio(md5Radio, "MD5", true);
        ConfigureRadio(sha1Radio, "SHA1");
        ConfigureRadio(sha256Radio, "SHA256");
        ConfigureRadio(sha384Radio, "SHA384");
        ConfigureRadio(sha512Radio, "SHA512");
        root.Controls.Add(algorithmPanel, 0, 1);

        dropPanel.Dock = DockStyle.Fill;
        dropPanel.AllowDrop = true;
        dropPanel.BackColor = Color.White;
        dropPanel.BorderStyle = BorderStyle.FixedSingle;
        dropPanel.Cursor = Cursors.Hand;
        root.Controls.Add(dropPanel, 0, 2);

        dropTitle.Text = "将文件拖到这里";
        dropTitle.Dock = DockStyle.Top;
        dropTitle.Height = 49;
        dropTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
        dropTitle.ForeColor = Color.FromArgb(37, 99, 235);
        dropTitle.TextAlign = ContentAlignment.BottomCenter;
        dropTitle.Padding = new Padding(0, 0, 0, 4);
        dropTitle.AutoSize = false;
        dropTitle.AllowDrop = true;
        dropTitle.Click += (_, _) => ChooseFiles();
        dropPanel.Controls.Add(dropTitle);

        dropHint.Text = "支持一次拖入多个文件，自动计算并添加结果";
        dropHint.Dock = DockStyle.Fill;
        dropHint.Font = new Font("Microsoft YaHei UI", 9.5F);
        dropHint.ForeColor = Color.FromArgb(107, 114, 128);
        dropHint.TextAlign = ContentAlignment.TopCenter;
        dropHint.Padding = new Padding(0, 7, 0, 0);
        dropHint.AutoSize = false;
        dropHint.AllowDrop = true;
        dropHint.Click += (_, _) => ChooseFiles();
        dropPanel.Controls.Add(dropHint);

        var resultsLabel = new Label
        {
            Text = "计算结果",
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = Color.FromArgb(75, 85, 99),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var resultsArea = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        resultsArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        resultsArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        resultsArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.Controls.Add(resultsArea, 0, 3);
        resultsArea.Controls.Add(resultsLabel, 0, 0);

        resultList.Dock = DockStyle.Fill;
        resultList.Font = new Font("Consolas", 10F);
        resultList.IntegralHeight = false;
        resultList.HorizontalScrollbar = true;
        resultList.SelectionMode = SelectionMode.MultiExtended;
        resultList.BorderStyle = BorderStyle.FixedSingle;
        resultList.BackColor = Color.White;
        resultList.ForeColor = Color.FromArgb(31, 41, 55);
        resultsArea.Controls.Add(resultList, 0, 1);

        var actionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        resultsArea.Controls.Add(actionPanel, 0, 2);

        ConfigureButton(chooseButton, "选择文件", Color.FromArgb(37, 99, 235));
        ConfigureButton(clearButton, "清空结果", Color.FromArgb(107, 114, 128));
        ConfigureButton(copyButton, "复制选中", Color.FromArgb(16, 185, 129));
        actionPanel.Controls.Add(chooseButton, 0, 0);
        actionPanel.Controls.Add(clearButton, 1, 0);
        actionPanel.Controls.Add(copyButton, 2, 0);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.Controls.Add(footer, 0, 4);

        var attribution = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 9, 0, 0),
            BackColor = Color.Transparent
        };
        var copyrightLabel = new Label
        {
            Text = "© 1847bell",
            AutoSize = true,
            ForeColor = Color.FromArgb(166, 175, 188),
            Margin = new Padding(0, 3, 10, 0)
        };
        var githubLink = new LinkLabel
        {
            Text = "GitHub",
            AutoSize = true,
            LinkColor = Color.FromArgb(145, 156, 172),
            ActiveLinkColor = Color.FromArgb(37, 99, 235),
            VisitedLinkColor = Color.FromArgb(145, 156, 172),
            LinkBehavior = LinkBehavior.HoverUnderline,
            Margin = new Padding(0, 3, 0, 0),
            Cursor = Cursors.Hand
        };
        githubLink.LinkClicked += (_, _) => OpenRepository();
        attribution.Controls.Add(copyrightLabel);
        attribution.Controls.Add(githubLink);
        footer.Controls.Add(attribution, 0, 0);

        statusLabel.Text = "就绪 · 窗口始终置顶";
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.ForeColor = Color.FromArgb(107, 114, 128);
        statusLabel.TextAlign = ContentAlignment.MiddleRight;
        footer.Controls.Add(statusLabel, 1, 0);
    }

    private void WireEvents()
    {
        foreach (var target in new Control[] { dropPanel, dropTitle, dropHint })
        {
            target.DragEnter += DropPanel_DragEnter;
            target.DragOver += DropPanel_DragEnter;
            target.DragLeave += (_, _) => SetDropVisual(false);
            target.DragDrop += DropPanel_DragDrop;
        }
        chooseButton.Click += (_, _) => ChooseFiles();
        clearButton.Click += (_, _) => resultList.Items.Clear();
        copyButton.Click += (_, _) => CopySelectedResults();
    }

    private static void ConfigureRadio(RadioButton radio, string text, bool selected = false)
    {
        radio.Text = text;
        radio.AutoSize = true;
        radio.Margin = new Padding(0, 6, 14, 0);
        radio.ForeColor = Color.FromArgb(55, 65, 81);
        radio.Checked = selected;
    }

    private static void ConfigureButton(Button button, string text, Color color)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Height = 30;
        button.Margin = new Padding(0, 2, 8, 3);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.Cursor = Cursors.Hand;
    }

    private void DropPanel_DragEnter(object? sender, DragEventArgs e)
    {
        if (isProcessing || !e.Data!.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        e.Effect = DragDropEffects.Copy;
        SetDropVisual(true);
    }

    private async void DropPanel_DragDrop(object? sender, DragEventArgs e)
    {
        SetDropVisual(false);
        if (isProcessing || !e.Data!.GetDataPresent(DataFormats.FileDrop))
            return;

        var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        await ProcessFilesAsync(paths);
    }

    private void SetDropVisual(bool active)
    {
        dropPanel.BackColor = active ? Color.FromArgb(239, 246, 255) : Color.White;
        dropTitle.ForeColor = active ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235);
    }

    private async void ChooseFiles()
    {
        if (isProcessing)
            return;

        using var dialog = new OpenFileDialog
        {
            Title = "选择要校验的文件",
            Multiselect = true,
            CheckFileExists = true,
            RestoreDirectory = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            await ProcessFilesAsync(dialog.FileNames);
    }

    private async Task ProcessFilesAsync(IEnumerable<string> paths)
    {
        var files = paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (files.Length == 0)
            return;

        isProcessing = true;
        SetControlsEnabled(false);
        var algorithmName = GetSelectedAlgorithmName();
        var started = resultList.Items.Count;
        statusLabel.Text = $"正在使用 {algorithmName} 计算 {files.Length} 个文件…";

        try
        {
            foreach (var (path, index) in files.Select((value, index) => (value, index)))
            {
                statusLabel.Text = $"正在计算 {index + 1}/{files.Length} · {Path.GetFileName(path)}";
                try
                {
                    var hash = await ComputeHashAsync(path, algorithmName);
                    resultList.Items.Add($"{Path.GetFileName(path)} => {Convert.ToHexString(hash).ToLowerInvariant()}");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
                {
                    resultList.Items.Add($"{Path.GetFileName(path)} => [失败: {ex.Message}]");
                }
            }

            if (resultList.Items.Count > started)
                resultList.TopIndex = resultList.Items.Count - 1;
            statusLabel.Text = $"完成 · 已处理 {files.Length} 个文件 · {algorithmName}";
        }
        finally
        {
            isProcessing = false;
            SetControlsEnabled(true);
        }
    }

    private static async Task<byte[]> ComputeHashAsync(string path, string algorithmName)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var algorithm = CreateAlgorithm(algorithmName);
        return await algorithm.ComputeHashAsync(stream);
    }

    private static HashAlgorithm CreateAlgorithm(string algorithmName) => algorithmName switch
    {
        "MD5" => MD5.Create(),
        "SHA1" => SHA1.Create(),
        "SHA256" => SHA256.Create(),
        "SHA384" => SHA384.Create(),
        "SHA512" => SHA512.Create(),
        _ => SHA256.Create()
    };

    private string GetSelectedAlgorithmName() =>
        md5Radio.Checked ? "MD5" :
        sha1Radio.Checked ? "SHA1" :
        sha384Radio.Checked ? "SHA384" :
        sha512Radio.Checked ? "SHA512" : "SHA256";

    private void CopySelectedResults()
    {
        if (resultList.SelectedItems.Count == 0)
            return;

        var lines = resultList.SelectedItems.Cast<string>().ToArray();
        Clipboard.SetText(string.Join(Environment.NewLine, lines));
        statusLabel.Text = $"已复制 {lines.Length} 行结果";
    }

    private static void OpenRepository()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = RepositoryUrl,
            UseShellExecute = true
        });
    }

    private void SetControlsEnabled(bool enabled)
    {
        algorithmPanel.Enabled = enabled;
        chooseButton.Enabled = enabled;
        clearButton.Enabled = enabled;
        copyButton.Enabled = enabled;
    }
}
