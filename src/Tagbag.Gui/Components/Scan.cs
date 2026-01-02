using System.Drawing;
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
        foreach (var problem in _Check?.GetProblems() ?? [])
            _ProblemListing.Items.Add(problem);
        _ProblemListing.EndUpdate();
    }
}

public class MoreProgressBar : ProgressBar
{
    private int _Value;
    private int _Maximum;

    public MoreProgressBar()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        Height = 30;
    }

    public void SetValues(string text, int value, int max)
    {
        Text = text;
        _Value = value;
        _Maximum = max;

        Maximum = value + 1;
        Value = value;
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
