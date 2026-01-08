using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class Scan : Control
{
    private Data _Data;

    private ListBox _ProblemListing = new ListBox();
    private MoreProgressBar _ProgressBar = new MoreProgressBar();
    private Button _ScanButton = new Button();
    private Button _FixAllButton = new Button();
    private Button _StopButton = new Button();

    private Check? _Check;

    public Scan(Data data)
    {
        _Data = data;
        LayoutControls();

        GuiTool.Setup(this);
        GuiTool.Setup(_ProblemListing);
        GuiTool.Setup(_ProgressBar);
        GuiTool.Setup(_ScanButton);
        GuiTool.Setup(_FixAllButton);
        GuiTool.Setup(_StopButton);

        _ScanButton.Click += (_, _) => ClickScan();
        _FixAllButton.Click += (_, _) => ClickFixAll();
        _StopButton.Click += (_, _) => ClickStop();

        _Data.EventHub.TagbagFileSet += (_) => AdjustButtons();

        AdjustButtons();
    }

    private void LayoutControls()
    {
        var font = new Font(FontFamily.GenericSansSerif, 16);

        _ProblemListing.Dock = DockStyle.Fill;
        Controls.Add(_ProblemListing);

        _ProgressBar.Dock = DockStyle.Bottom;
        Controls.Add(_ProgressBar);

        var buttonRow = new Control();
        buttonRow.Dock = DockStyle.Bottom;
        buttonRow.Height = 40;

        _FixAllButton.Dock = DockStyle.Left;
        _FixAllButton.Text = "Fix all";
        _FixAllButton.Font = font;
        _FixAllButton.Width = _FixAllButton.Width + 20;
        buttonRow.Controls.Add(_FixAllButton);

        _ScanButton.Dock = DockStyle.Left;
        _ScanButton.Text = "Scan";
        _ScanButton.Font = font;
        buttonRow.Controls.Add(_ScanButton);

        _StopButton.Dock = DockStyle.Right;
        _StopButton.Text = "Stop";
        _StopButton.Font = font;
        buttonRow.Controls.Add(_StopButton);

        Controls.Add(buttonRow);
    }

    private void AdjustButtons()
    {
        _ScanButton.Enabled = _Check == null || _Check.GetState() == Check.State.None;
        _FixAllButton.Enabled = _Check?.GetProblems().Count > 0;
        _StopButton.Enabled = _Check?.GetState() != Check.State.None;
    }

    private void ClickScan()
    {
        if (_Data.Tagbag is Tagbag.Core.Tagbag tb)
        {
            _Check = new Check(tb);

            _Check.StateChange += (_, _) => AdjustButtons();
            _Check.StateChange += (_, _) => RefreshProblemListing();
            _Check.Progress += _ProgressBar.SetValues;

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
            var node = (_Check?.GetProblems() ?? []).First;
            while (node != null)
            {
                if (node?.Value is Problem problem)
                    _ProblemListing.Items.Add(problem);
                node = node?.Next;
            }
        }
        finally
        {
            _ProblemListing.EndUpdate();
        }
    }
}

public class MoreProgressBar : Control
{
    private int _Value;
    private int _Maximum;

    private System.Windows.Forms.Timer _RefreshTimer;
    private int _RefreshCounter;

    public MoreProgressBar()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        Height = 30;

        _RefreshTimer = new System.Windows.Forms.Timer();
        _RefreshTimer.Interval = 250;
        _RefreshTimer.Tick += (_, _) => RefreshFunction();
        _RefreshTimer.Start();
    }

    public void SetValues(string text, int value, int max)
    {
        Text = text;
        _Value = value;
        _Maximum = max;

        Interlocked.Increment(ref _RefreshCounter);
    }

    private void RefreshFunction()
    {
        if (0 != Interlocked.Exchange(ref _RefreshCounter, 0))
            Refresh();
    }

    override protected void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.FillRectangle(Brushes.DarkRed, ClientRectangle);

        float progressWidth = 0;
        if (_Value >= 0 && _Maximum > 0)
            progressWidth = _Value * Width / _Maximum;
        else if (_Maximum == 0 && _Value == 0)
            progressWidth = Width;

        g.FillRectangle(Brushes.DarkGreen,
                        new RectangleF(0, 0, progressWidth, Height));

        using (Font f = new Font(FontFamily.GenericSerif, 16))
        {
            string text;
            if (_Maximum > 0 || (_Maximum == 0 && _Value == 0))
                text = $"{Text} {_Value} / {_Maximum}";
            else
                text = $"{Text} {_Value} / ?";

            var len = g.MeasureString(text, f);
            var location = new Point(System.Convert.ToInt32((Width / 2) - len.Width / 2),
                                     System.Convert.ToInt32((Height / 2) - len.Height / 2));
            g.DrawString(text, f, Brushes.White, location);
        }
    }
}
