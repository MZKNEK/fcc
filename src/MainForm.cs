using System.Drawing;
using System.Windows.Forms;

namespace FCC;

internal sealed class MainForm : Form
{
    private readonly TextBox _path;
    private readonly Button _browse;
    private readonly CheckBox _hidden;
    private readonly CheckBox _recursive;
    private readonly CheckBox _dirNames;
    private readonly CheckBox _groupSize;
    private readonly CheckBox _avgSize;
    private readonly CheckBox _verbose;
    private readonly CheckBox _random;
    private readonly NumericUpDown _min;
    private readonly NumericUpDown _more;
    private readonly CheckBox _lessEnabled;
    private readonly NumericUpDown _less;
    private readonly Button _run;
    private readonly Button _save;
    private readonly ListView _list;
    private readonly ToolStripStatusLabel _status;

    private FolderReader.Output _lastOutput;

    internal MainForm()
    {
        Text = "FCC";
        ClientSize = new Size(750, 570);
        MinimumSize = new Size(640, 460);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        _lastOutput = new FolderReader.Output();

        // --- directory row -------------------------------------------------
        var dirLabel = new Label
        {
            Text = "Directory:",
            AutoSize = true,
            Location = new Point(12, 15)
        };

        _path = new TextBox
        {
            Location = new Point(80, 12),
            Width = 560,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = Directory.GetCurrentDirectory()
        };

        _browse = new Button
        {
            Text = "...",
            Location = new Point(648, 11),
            Width = 90,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _browse.Click += (_, _) => Browse();

        // --- options -------------------------------------------------------
        var options = new GroupBox
        {
            Text = "Options",
            Location = new Point(12, 48),
            Size = new Size(726, 120),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _hidden = new CheckBox { Text = "Include hidden (-a)", Location = new Point(12, 24), AutoSize = true };
        _recursive = new CheckBox { Text = "Recursive (-r)", Location = new Point(12, 50), AutoSize = true };
        _dirNames = new CheckBox { Text = "Subdir names (-d, needs -r)", Location = new Point(12, 76), AutoSize = true, Enabled = false };
        _groupSize = new CheckBox { Text = "Group size (-s)", Location = new Point(260, 24), AutoSize = true };
        _avgSize = new CheckBox { Text = "Average size (-g, needs -s)", Location = new Point(260, 50), AutoSize = true, Enabled = false };
        _verbose = new CheckBox { Text = "Verbose (-v)", Location = new Point(260, 76), AutoSize = true };
        _random = new CheckBox { Text = "Random entry (--rand)", Location = new Point(520, 24), AutoSize = true };

        _recursive.CheckedChanged += (_, _) =>
        {
            _dirNames.Enabled = _recursive.Checked;
            if (!_recursive.Checked)
                _dirNames.Checked = false;
        };
        _groupSize.CheckedChanged += (_, _) =>
        {
            _avgSize.Enabled = _groupSize.Checked;
            if (!_groupSize.Checked)
                _avgSize.Checked = false;
        };

        options.Controls.AddRange(new Control[]
        {
            _hidden, _recursive, _dirNames, _groupSize, _avgSize, _verbose, _random
        });

        // --- group filters -------------------------------------------------
        var filters = new GroupBox
        {
            Text = "Group filters",
            Location = new Point(12, 176),
            Size = new Size(726, 60),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var minLabel = new Label { Text = "Min name prefix (--min):", Location = new Point(12, 26), AutoSize = true };
        _min = new NumericUpDown { Location = new Point(160, 23), Width = 70, Minimum = 0, Maximum = 1000, Value = 20 };

        var moreLabel = new Label { Text = "Min count (--more):", Location = new Point(260, 26), AutoSize = true };
        _more = new NumericUpDown { Location = new Point(380, 23), Width = 70, Minimum = 0, Maximum = 1000000, Value = 0 };

        _lessEnabled = new CheckBox { Text = "Max count (--less):", Location = new Point(480, 25), AutoSize = true };
        _less = new NumericUpDown { Location = new Point(630, 23), Width = 70, Minimum = 0, Maximum = 1000000, Value = 0, Enabled = false };
        _lessEnabled.CheckedChanged += (_, _) => _less.Enabled = _lessEnabled.Checked;

        filters.Controls.AddRange(new Control[]
        {
            minLabel, _min, moreLabel, _more, _lessEnabled, _less
        });

        // --- actions -------------------------------------------------------
        _run = new Button { Text = "Run", Location = new Point(12, 246), Width = 100 };
        _run.Click += (_, _) => Run();

        _save = new Button { Text = "Save output...", Location = new Point(120, 246), Width = 130, Enabled = false };
        _save.Click += (_, _) => SaveOutput();

        // --- results -------------------------------------------------------
        _list = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = true,
            HideSelection = false,
            Location = new Point(12, 282),
            Size = new Size(726, 250),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _list.Columns.Add("Name", 520);
        _list.Columns.Add("Count", 90);
        _list.Columns.Add("Size", 110);

        _status = new ToolStripStatusLabel
        {
            Text = "Ready",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var status = new StatusStrip { Dock = DockStyle.Bottom };
        status.Items.Add(_status);

        AcceptButton = _run;

        Controls.AddRange(new Control[]
        {
            dirLabel, _path, _browse, options, filters, _run, _save, _list, status
        });
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = _path.Text };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _path.Text = dialog.SelectedPath;
    }

    private FolderReader.Configuration BuildFlags()
    {
        var flags = FolderReader.Configuration.None;

        if (_hidden.Checked) flags |= FolderReader.Configuration.Hidden;
        if (_recursive.Checked) flags |= FolderReader.Configuration.Recursive;
        if (_dirNames.Checked) flags |= FolderReader.Configuration.DirNames;
        if (_groupSize.Checked) flags |= FolderReader.Configuration.GroupSize;
        if (_avgSize.Checked) flags |= FolderReader.Configuration.AvgSize;
        if (_verbose.Checked) flags |= FolderReader.Configuration.Verbose;
        if (_random.Checked) flags |= FolderReader.Configuration.RandomEntry;

        return flags;
    }

    private void Run()
    {
        var dir = new DirectoryInfo(_path.Text);
        if (!dir.Exists)
        {
            MessageBox.Show(this, "Directory does not exist.", "FCC",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var max = _lessEnabled.Checked ? (uint)_less.Value : (uint?)null;
            var reader = new FolderReader(dir, BuildFlags(), (uint)_min.Value, max, (uint)_more.Value);
            var output = reader.Analyze();

            _lastOutput = output;
            ShowEntries(output);
            _save.Enabled = true;
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(this, "You do not have sufficient permissions to view all directories or files.", "FCC",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "FCC",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowEntries(FolderReader.Output output)
    {
        _list.BeginUpdate();
        _list.Items.Clear();

        foreach (var entry in output.Entries)
        {
            var name = entry.Directory is null ? entry.Name : $"{entry.Directory}/{entry.Name}";
            var item = new ListViewItem(name);
            item.SubItems.Add(entry.Count is null ? string.Empty : $"x{entry.Count}");
            item.SubItems.Add(entry.Size ?? string.Empty);
            _list.Items.Add(item);
        }

        _list.EndUpdate();
        _status.Text = $"TOTAL: {output.Stats.Summary(!_verbose.Checked)}";
    }

    private void SaveOutput()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "FCC output (*.fcc)|*.fcc|Text file (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"fcc-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.fcc"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, _lastOutput.Result.ToString());
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "FCC",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
