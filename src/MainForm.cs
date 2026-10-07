using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FCC;

internal sealed class MainForm : Form
{
    private static readonly (string Label, BiSize.Kind? Unit)[] SizeUnits =
    {
        ("Auto", null),
        ("Bytes", BiSize.Kind.Bytes),
        ("KiB", BiSize.Kind.KiB),
        ("MiB", BiSize.Kind.MiB),
        ("GiB", BiSize.Kind.GiB),
        ("TiB", BiSize.Kind.TiB),
    };

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

    private readonly List<string> _columnNames = new();

    private FolderReader.Output _lastOutput;

    private int _sortColumn = -1;
    private bool _sortAscending = true;
    private bool _hasDirColumn;
    private BiSize.Kind? _sizeUnit;

    internal MainForm()
    {
        Text = $"FCC {FCC.Version}";
        ClientSize = new Size(750, 570);
        MinimumSize = new Size(640, 460);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        _lastOutput = new FolderReader.Output();

        // --- menu ----------------------------------------------------------
        var menu = new MenuStrip();
        var viewMenu = new ToolStripMenuItem("View");
        var unitMenu = new ToolStripMenuItem("Size unit");

        foreach (var (label, unit) in SizeUnits)
        {
            var item = new ToolStripMenuItem(label) { Tag = unit, Checked = unit is null };
            item.Click += (s, _) =>
            {
                _sizeUnit = (BiSize.Kind?)((ToolStripMenuItem)s!).Tag;

                foreach (ToolStripMenuItem other in unitMenu.DropDownItems)
                    other.Checked = other.Tag is BiSize.Kind kind ? _sizeUnit == kind : _sizeUnit is null;

                RefreshEntries();
            };
            unitMenu.DropDownItems.Add(item);
        }

        viewMenu.DropDownItems.Add(unitMenu);
        menu.Items.Add(viewMenu);
        MainMenuStrip = menu;

        // --- directory row -------------------------------------------------
        var dirLabel = new Label
        {
            Text = "Directory:",
            AutoSize = true,
            Location = new Point(12, 43)
        };

        _path = new TextBox
        {
            Location = new Point(80, 40),
            Width = 560,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = Directory.GetCurrentDirectory()
        };

        var pathMenu = new ContextMenuStrip();
        pathMenu.Items.Add("Open in Explorer", null, (_, _) => OpenInExplorer());
        pathMenu.Items.Add("Copy path", null, (_, _) => CopyPath());
        _path.ContextMenuStrip = pathMenu;

        _browse = new Button
        {
            Text = "...",
            Location = new Point(648, 39),
            Width = 90,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _browse.Click += (_, _) => Browse();

        // --- options -------------------------------------------------------
        var options = new GroupBox
        {
            Text = "Options",
            Location = new Point(12, 76),
            Size = new Size(726, 120),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _hidden = new CheckBox { Text = "Include hidden (-a)", Location = new Point(12, 24), AutoSize = true };
        _recursive = new CheckBox { Text = "Recursive (-r)", Location = new Point(12, 50), AutoSize = true };
        _dirNames = new CheckBox { Text = "Subdir names (-d, needs -r)", Location = new Point(12, 76), AutoSize = true, Enabled = false };
        _groupSize = new CheckBox { Text = "Group size (-s)", Location = new Point(260, 24), AutoSize = true, Checked = true };
        _avgSize = new CheckBox { Text = "Average size (-g, needs -s)", Location = new Point(260, 50), AutoSize = true, Checked = true };
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
        _dirNames.CheckedChanged += (_, _) => RefreshEntries();

        options.Controls.AddRange(new Control[]
        {
            _hidden, _recursive, _dirNames, _groupSize, _avgSize, _verbose, _random
        });

        // --- group filters -------------------------------------------------
        var filters = new GroupBox
        {
            Text = "Group filters",
            Location = new Point(12, 204),
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
        _run = new Button { Text = "Run", Location = new Point(12, 274), Width = 100 };
        _run.Click += (_, _) => Run();

        _save = new Button { Text = "Save output...", Location = new Point(120, 274), Width = 130, Enabled = false };
        _save.Click += (_, _) => SaveOutput();

        // --- results -------------------------------------------------------
        _list = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = true,
            HideSelection = false,
            Location = new Point(12, 310),
            Size = new Size(726, 220),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _list.ColumnClick += (_, e) =>
        {
            if (e.Column == _sortColumn)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = e.Column;
                _sortAscending = true;
            }

            ApplySort();
        };

        var listMenu = new ContextMenuStrip();
        listMenu.Items.Add("Open in Explorer", null, (_, _) => OpenEntryInExplorer());
        listMenu.Items.Add("Open", null, (_, _) => OpenEntry());
        listMenu.Items.Add("Copy path", null, (_, _) => CopyEntryPath());
        listMenu.Opening += (_, _) => UpdateEntryMenu(listMenu);
        _list.ContextMenuStrip = listMenu;
        _list.DoubleClick += (_, _) => OpenEntryInExplorer();

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
            menu, dirLabel, _path, _browse, options, filters, _run, _save, _list, status
        });

        EnsureColumns();
    }

    private void EnsureColumns()
    {
        var wantDir = _dirNames.Checked;

        if (_columnNames.Count > 0 && wantDir == _hasDirColumn)
            return;

        _hasDirColumn = wantDir;
        _sortColumn = -1;
        _columnNames.Clear();
        _list.Columns.Clear();

        if (wantDir)
        {
            _columnNames.Add("Dir");
            _list.Columns.Add("Dir", 170);
        }

        _columnNames.Add("Name");
        _columnNames.Add("Count");
        _columnNames.Add("Size");
        _columnNames.Add("Unit");

        _list.Columns.Add("Name", wantDir ? 300 : 440);
        _list.Columns.Add("Count", 80);
        _list.Columns.Add("Size", 110);
        _list.Columns.Add("Unit", 80);
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

            _lastOutput = reader.Analyze();
            RefreshEntries();
            _save.Enabled = true;
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(this, "You do not have sufficient permissions to view all directories or files.", "FCC",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RefreshEntries()
    {
        EnsureColumns();

        _list.BeginUpdate();
        _list.Items.Clear();

        foreach (var entry in _lastOutput.Entries)
        {
            var values = new List<string>();

            if (_hasDirColumn)
                values.Add(entry.Directory ?? string.Empty);

            values.Add(entry.Name);
            values.Add(entry.Count?.ToString() ?? string.Empty);

            if (entry.Size is null)
            {
                values.Add(string.Empty);
                values.Add(string.Empty);
            }
            else
            {
                var (value, unit) = BiSize.FormatBytes(entry.SizeBytes, _sizeUnit);
                values.Add(value);
                values.Add(unit.ToString());
            }

            var item = new ListViewItem(values[0]) { Tag = entry };
            for (var i = 1; i < values.Count; i++)
                item.SubItems.Add(values[i]);

            _list.Items.Add(item);
        }

        _list.EndUpdate();
        _status.Text = $"TOTAL: {_lastOutput.Stats.Summary(!_verbose.Checked)}";

        if (_sortColumn >= 0)
            ApplySort();
    }

    private void ApplySort()
    {
        _list.ListViewItemSorter = new EntryComparer(_sortColumn, _sortAscending, _hasDirColumn);

        for (var i = 0; i < _list.Columns.Count && i < _columnNames.Count; i++)
        {
            var text = _columnNames[i];
            if (i == _sortColumn)
                text += _sortAscending ? " \u25B2" : " \u25BC";
            _list.Columns[i].Text = text;
        }

        _list.Sort();
    }

    private void OpenInExplorer()
    {
        var path = _path.Text;
        if (!Directory.Exists(path))
        {
            MessageBox.Show(this, "Directory does not exist.", "FCC",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void CopyPath()
    {
        if (_path.TextLength > 0)
            Clipboard.SetText(_path.Text);
    }

    private bool TryGetSelectedEntry(out FolderReader.Entry entry)
    {
        if (_list.SelectedItems.Count > 0 && _list.SelectedItems[0].Tag is FolderReader.Entry selected)
        {
            entry = selected;
            return true;
        }

        entry = default;
        return false;
    }

    private void UpdateEntryMenu(ContextMenuStrip menu)
    {
        var hasEntry = _list.SelectedItems.Count > 0 && _list.SelectedItems[0].Tag is FolderReader.Entry;

        foreach (ToolStripItem item in menu.Items)
            item.Enabled = hasEntry;
    }

    private void OpenEntryInExplorer()
    {
        if (!TryGetSelectedEntry(out var entry))
            return;

        try
        {
            if (entry.OpenPath is { } path && File.Exists(path))
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            else if (entry.FullDirectory is not null)
                Process.Start(new ProcessStartInfo { FileName = entry.FullDirectory, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void OpenEntry()
    {
        if (!TryGetSelectedEntry(out var entry))
            return;

        // A group entry opens its first file in the default program.
        if (entry.OpenPath is { } path && File.Exists(path))
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
        else
        {
            OpenEntryInExplorer();
        }
    }

    private void CopyEntryPath()
    {
        if (!TryGetSelectedEntry(out var entry))
            return;

        var path = entry.OpenPath ?? entry.FullDirectory;
        if (path is not null)
            Clipboard.SetText(path);
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
            ShowError(ex);
        }
    }

    private void ShowError(Exception ex)
        => MessageBox.Show(this, ex.Message, "FCC", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private sealed class EntryComparer : System.Collections.IComparer
    {
        private readonly int _column;
        private readonly bool _ascending;
        private readonly bool _hasDir;

        internal EntryComparer(int column, bool ascending, bool hasDir)
        {
            _column = column;
            _ascending = ascending;
            _hasDir = hasDir;
        }

        public int Compare(object? x, object? y)
        {
            if (x is not ListViewItem a || y is not ListViewItem b ||
                a.Tag is not FolderReader.Entry ea || b.Tag is not FolderReader.Entry eb)
                return 0;

            var dirColumn = _hasDir ? 0 : -1;
            var nameColumn = _hasDir ? 1 : 0;
            var countColumn = _hasDir ? 2 : 1;
            var sizeColumn = _hasDir ? 3 : 2;
            var unitColumn = _hasDir ? 4 : 3;

            int result;

            if (_column == dirColumn)
                result = string.Compare(ea.Directory, eb.Directory, StringComparison.OrdinalIgnoreCase);
            else if (_column == nameColumn)
                result = string.Compare(ea.Name, eb.Name, StringComparison.OrdinalIgnoreCase);
            else if (_column == countColumn)
                result = Nullable.Compare(ea.Count, eb.Count);
            else if (_column == sizeColumn)
                result = ea.SizeBytes.CompareTo(eb.SizeBytes);
            else if (_column == unitColumn)
                result = string.Compare(UnitName(ea), UnitName(eb), StringComparison.Ordinal);
            else
                result = 0;

            return _ascending ? result : -result;
        }

        private static string UnitName(FolderReader.Entry entry)
            => entry.Size is null ? string.Empty : BiSize.FormatBytes(entry.SizeBytes, null).Unit.ToString();
    }
}
