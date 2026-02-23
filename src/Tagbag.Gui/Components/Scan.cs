using System.Drawing;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class Scan : Control
{
    private Data _Data;

    private ListBox _ProblemListing = new ListBox();
    private Button _ScanButton = new Button();
    private Button _FixAllButton = new Button();
    private Button _StopButton = new Button();

    private Label _ProgressLabel = new Label();
    private Label _ProblemsFoundLabel = new Label();
    private Label _ProblemsFixedLabel = new Label();
    private long _LastReportUpdate;

    private Check? _Check;

    public Scan(Data data)
    {
        _Data = data;

        GuiTool.Setup(this);
        GuiTool.Setup(_ProblemListing);
        GuiTool.Setup(_ScanButton);
        GuiTool.Setup(_FixAllButton);
        GuiTool.Setup(_StopButton);

        GuiTool.Setup(_ProgressLabel);
        GuiTool.Setup(_ProblemsFoundLabel);
        GuiTool.Setup(_ProblemsFixedLabel);

        LayoutControls();

        _ScanButton.Click += (_, _) => ClickScan();
        _FixAllButton.Click += (_, _) => ClickFixAll();
        _StopButton.Click += (_, _) => ClickStop();

        _Data.EventHub.TagbagFileSet += (_) => AdjustButtons();

        AdjustButtons();
        ReportChanged(force: true);
    }

    private void LayoutControls()
    {
        var font = new Font(FontFamily.GenericMonospace, 14);

        var basePlate = GuiTool.Setup(new Panel());
        basePlate.Width = 500;
        basePlate.Height = 500;

        _ProblemListing.Dock = DockStyle.Fill;
        basePlate.Controls.Add(_ProblemListing);

        var statusBox = new TableLayoutPanel();
        statusBox.Dock = DockStyle.Top;
        statusBox.ColumnCount = 2;
        statusBox.RowCount = 3;

        var label = GuiTool.Setup(new Label());
        label.Text = "Status";
        label.Font = font;
        label.Width = 300;
        statusBox.Controls.Add(label);
        _ProgressLabel.Font = font;
        statusBox.Controls.Add(_ProgressLabel);

        label = GuiTool.Setup(new Label());
        label.Text = "Problems found";
        label.Font = font;
        label.Width = 300;
        statusBox.Controls.Add(label);
        _ProblemsFoundLabel.Font = font;
        statusBox.Controls.Add(_ProblemsFoundLabel);

        label = GuiTool.Setup(new Label());
        label.Text = "Problems fixed";
        label.Font = font;
        label.Width = 300;
        statusBox.Controls.Add(label);
        _ProblemsFixedLabel.Font = font;
        statusBox.Controls.Add(_ProblemsFixedLabel);

        basePlate.Controls.Add(statusBox);

        var buttonRow = GuiTool.Setup(new Panel());
        buttonRow.Dock = DockStyle.Top;
        buttonRow.Height = 40;

        _FixAllButton.Dock = DockStyle.Left;
        _FixAllButton.Font = font;
        _FixAllButton.Text = "Fix";
        buttonRow.Controls.Add(_FixAllButton);

        _ScanButton.Dock = DockStyle.Left;
        _ScanButton.Font = font;
        _ScanButton.Text = "Check";
        buttonRow.Controls.Add(_ScanButton);

        _StopButton.Dock = DockStyle.Right;
        _StopButton.Font = font;
        _StopButton.Text = "Stop";
        buttonRow.Controls.Add(_StopButton);

        basePlate.Controls.Add(buttonRow);

        Controls.Add(basePlate);
    }

    private void AdjustButtons()
    {
        var stopped = _Check == null || _Check.GetState() == Check.State.None;

        _ScanButton.Enabled = stopped;
        _FixAllButton.Enabled = stopped && _Check?.GetProblems().Count > 0;
        _StopButton.Enabled = !stopped;
    }

    private void ReportChanged(bool force = false)
    {
        if (!force)
        {
            var now = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (now - _LastReportUpdate > 100)
                _LastReportUpdate = now;
            else
                return;
        }

        if (_Check == null)
        {
            _ProgressLabel.Text = "Ready";
            _ProblemsFoundLabel.Text = "-";
            _ProblemsFixedLabel.Text = "-";
        }
        else
        {
            var report = _Check.Report;
            switch (_Check.GetState())
            {
                case Check.State.None:
                    _ProgressLabel.Text = "Ready";
                    break;
                case Check.State.Running:
                    _ProgressLabel.Text = "Running";
                    break;
                case Check.State.Stopping:
                    _ProgressLabel.Text = "Stopping";
                    break;
            }
            _ProblemsFoundLabel.Text = "" + report.ProblemsFound;
            _ProblemsFixedLabel.Text = "" + report.ProblemsFixed;
        }
    }

    private void ClickScan()
    {
        if (_Data.Tagbag is Tagbag.Core.Tagbag tb)
        {
            _Check = new Check(tb);

            _Check.StateChange += (_, _) =>
            {
                AdjustButtons();
                ReportChanged(force: true);
                RefreshProblemListing();
            };
            _Check.ReportChanged += () => ReportChanged();

            _Check.Scan();
            AdjustButtons();
        }
    }

    private void ClickFixAll()
    {
        _Check?.Fix();
        AdjustButtons();
    }

    private void ClickStop()
    {
        _Check?.Stop();
        AdjustButtons();
    }

    private void RefreshProblemListing()
    {
        _ProblemListing.BeginUpdate();
        _ProblemListing.Items.Clear();

        try
        {
            foreach (var problem in _Check?.GetProblems() ?? [])
                _ProblemListing.Items.Add(problem);
        }
        finally
        {
            _ProblemListing.EndUpdate();
        }
    }
}
