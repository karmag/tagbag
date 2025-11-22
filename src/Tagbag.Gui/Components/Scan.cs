using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class Scan : Control
{
    private Data _Data;

    private Button _SelectPathButton = new Button();
    private TextBox _Path = new TextBox();
    private Button _ScanButton = new Button();
    private MoreProgressBar _FilesProgress = new MoreProgressBar("File");
    private MoreProgressBar _ScannedProgress = new MoreProgressBar("Entry");
    private TextBox _Report = new TextBox();

    private Scanner? _Scanner;
    private bool _Running;

    public Scan(Data data)
    {
        _Data = data;
        LayoutControls();
    }

    private void LayoutControls()
    {
        var main = new Control();

        _Report.Dock = DockStyle.Fill;
        _Report.Multiline = true;
        _Report.ReadOnly = true;
        main.Controls.Add(_Report);

        _ScanButton.Text = "Scan";
        _ScanButton.Dock = DockStyle.Top;
        _ScanButton.Width = 80;
        _ScanButton.Click += (_, _) => Run();
        main.Controls.Add(_ScanButton);

        _ScannedProgress.Dock = DockStyle.Top;
        main.Controls.Add(_ScannedProgress);

        _FilesProgress.Dock = DockStyle.Top;
        main.Controls.Add(_FilesProgress);

        var path = new Control();

        _Path.Dock = DockStyle.Fill;
        _Path.Text = "Path";
        path.Controls.Add(_Path);

        _SelectPathButton.Text = "Select path";
        _SelectPathButton.Dock = DockStyle.Right;
        path.Controls.Add(_SelectPathButton);

        path.Dock = DockStyle.Top;
        path.Height = 30;
        main.Controls.Add(path);

        main.Width = 300;
        main.Height = 300;
        Controls.Add(main);
    }

    private void Run()
    {
        if (_Running)
            Stop();
        else
            Start();
    }

    private void Start()
    {
        lock (this)
        {
            if (!_Running && _Data.Tagbag is Tagbag.Core.Tagbag tb)
            {
                _Scanner = new Scanner(tb, null);
                _Scanner.ProgressReport += CounterUpdate;
                _Running = true;
                _ScanButton.Text = "Stop";
                _Scanner.Start();
            }
        }
    }

    private void Stop()
    {
        lock (this)
        {
            if (_Running)
            {
                _Scanner?.Stop();
                _Running = false;
                _ScanButton.Text = "Scan";

                _Data.SetTagbag(_Data.Tagbag);
            }
        }
    }

    private void CounterUpdate(Scanner.Counter counter)
    {
        var files = counter.DirectoriesQueued + counter.FilesQueued;
        var remainingFiles = counter.DirectoriesRemaining + counter.FilesRemaining;

        _FilesProgress.SetMax(files);
        _FilesProgress.SetCurrent(files - remainingFiles);
        _ScannedProgress.SetMax(counter.EntriesQueued);
        _ScannedProgress.SetCurrent(counter.EntriesQueued - counter.EntriesRemaining);

        if (counter.Completed)
            Stop();
    }
}

public class MoreProgressBar : Control
{
    private Label _Title = new Label();
    private ProgressBar _ProgressBar = new ProgressBar();
    private Label _Stats = new Label();

    private int _Max = 0;
    private int _Current = 0;

    public MoreProgressBar(string title)
    {
        _ProgressBar.Dock = DockStyle.Fill;
        Controls.Add(_ProgressBar);

        _Title.Text = title;
        _Title.Dock = DockStyle.Left;
        Controls.Add(_Title);

        _Stats.Dock = DockStyle.Right;
        Controls.Add(_Stats);

        Height = 30;

        UpdateValues();
    }

    public void SetMax(int max)
    {
        _Max = max;
        _ProgressBar.Maximum = max;
        UpdateValues();
    }

    public void SetCurrent(int current)
    {
        _Current = current;
        _ProgressBar.Value = current;
        UpdateValues();
    }

    private void UpdateValues()
    {
        _Stats.Text = $"{_Current} / {_Max}";
    }
}
