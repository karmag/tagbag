using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class ScanDuplicate : Panel
{
    private Data _Data;

    private DuplicationDetection? _DuplicationDetection;
    private bool _Running;

    private (DuplicationDetection.Activity, int, int) _ProgressCache;
    private long _LastReportUpdate;

    private Button _PopulateButton = new Button();
    private Button _FindSimilarButton = new Button();
    private TextBox _FindSimilarArgInput = new TextBox();
    private TextBox _FindSimilarArgDescription = new TextBox();
    private Button _DeleteDuplicatesButton = new Button();
    private Button _StopButton = new Button();

    private Label _ProgressActivity = new Label();
    private Label _ProgressCurrent = new Label();
    private Label _ProgressGoal = new Label();

    public ScanDuplicate(Data data)
    {
        _Data = data;

        GuiTool.Setup(this);
        GuiTool.Setup(_PopulateButton);
        GuiTool.Setup(_FindSimilarButton);
        GuiTool.Setup(_FindSimilarArgInput);
        GuiTool.Setup(_FindSimilarArgDescription);
        GuiTool.Setup(_DeleteDuplicatesButton);
        GuiTool.Setup(_StopButton);
        GuiTool.Setup(_ProgressCurrent);
        GuiTool.Setup(_ProgressGoal);

        LayoutControls();
        AdjustButtons();
        UpdateFindSimilar();

        _Data.EventHub.TagbagFileSet += (tbFileSet) => {
            if (tbFileSet.Tagbag != null)
            {
                _DuplicationDetection = new DuplicationDetection(tbFileSet.Tagbag);
                _DuplicationDetection.ProgressReport += ListenProgressReport;
            }
            else
                _DuplicationDetection = null;
            AdjustButtons();
        };
    }

    private void LayoutControls()
    {
        var font = new Font(FontFamily.GenericMonospace, 14);
        const int buttonWidth = 200;
        const int buttonHeight = 32;

        var basePanel = GuiTool.Setup(new Panel());
        basePanel.Width = 600;
        basePanel.Height = 270;

        // Progress

        var counterPanel = GuiTool.Setup(new Panel());
        counterPanel.Dock = DockStyle.Bottom;

        _ProgressActivity.Dock = DockStyle.Fill;
        _ProgressActivity.Font = font;
        _ProgressActivity.Text = "---";
        counterPanel.Controls.Add(_ProgressActivity);

        _ProgressCurrent.Dock = DockStyle.Right;
        _ProgressCurrent.Width = 50;
        _ProgressCurrent.Font = font;
        _ProgressCurrent.Text = "-";
        _ProgressCurrent.Width = 100;
        counterPanel.Controls.Add(_ProgressCurrent);

        var slash = new Label();
        slash.Dock = DockStyle.Right;
        slash.Width = 30;
        slash.Text = "/";
        slash.Font = font;
        counterPanel.Controls.Add(slash);

        _ProgressGoal.Dock = DockStyle.Right;
        _ProgressGoal.Width = 50;
        _ProgressGoal.Font = font;
        _ProgressGoal.Text = "-";
        _ProgressGoal.Width = 100;
        counterPanel.Controls.Add(_ProgressGoal);

        basePanel.Controls.Add(counterPanel);

        // Buttons

        var buttonPanel = new TableLayoutPanel();
        GuiTool.Setup(buttonPanel);
        buttonPanel.ColumnCount = 3;
        buttonPanel.RowCount = 4;
        buttonPanel.Height = 300;
        buttonPanel.Dock = DockStyle.Top;

        _PopulateButton.Text = "Populate Hashes";
        _PopulateButton.Font = font;
        _PopulateButton.Width = buttonWidth;
        _PopulateButton.Height = buttonHeight;
        _PopulateButton.Click += (_, _) => RunCommand(ClickPopulateHashes);
        buttonPanel.Controls.Add(_PopulateButton);

        buttonPanel.Controls.Add(new Label());
        buttonPanel.Controls.Add(new Label());

        _FindSimilarButton.Text = "Find Similar";
        _FindSimilarButton.Font = font;
        _FindSimilarButton.Width = buttonWidth;
        _FindSimilarButton.Height = buttonHeight;
        _FindSimilarButton.Click += (_, _) => RunCommand(ClickFindSimilar);
        buttonPanel.Controls.Add(_FindSimilarButton);

        _FindSimilarArgInput.Text = "10%";
        _FindSimilarArgInput.Font = font;
        _FindSimilarArgInput.Height = buttonHeight;
        _FindSimilarArgInput.WordWrap = false;
        _FindSimilarArgInput.KeyUp += (_, _) => UpdateFindSimilar();
        buttonPanel.Controls.Add(_FindSimilarArgInput);

        _FindSimilarArgDescription.Text = "-";
        _FindSimilarArgDescription.Font = font;
        _FindSimilarArgDescription.Height = buttonHeight;
        _FindSimilarArgDescription.Width = 250;
        buttonPanel.Controls.Add(_FindSimilarArgDescription);

        _DeleteDuplicatesButton.Text = "Delete Duplicates";
        _DeleteDuplicatesButton.Font = font;
        _DeleteDuplicatesButton.Width = buttonWidth;
        _DeleteDuplicatesButton.Height = buttonHeight;
        _DeleteDuplicatesButton.Click += (_, _) => RunCommand(ClickDeleteDuplicates);
        buttonPanel.Controls.Add(_DeleteDuplicatesButton);

        buttonPanel.Controls.Add(new Label());
        buttonPanel.Controls.Add(new Label());

        _StopButton.Text = "Stop";
        _StopButton.Font = font;
        _StopButton.Width = 100;
        _StopButton.Height = buttonHeight;
        _StopButton.Click += (_, _) => _DuplicationDetection?.Stop();
        buttonPanel.Controls.Add(_StopButton);

        buttonPanel.Controls.Add(new Label());
        buttonPanel.Controls.Add(new Label());

        basePanel.Controls.Add(buttonPanel);

        Controls.Add(basePanel);
    }

    private void AdjustButtons()
    {
        var active = _DuplicationDetection != null && !_Running;
        _PopulateButton.Enabled = active;
        _FindSimilarButton.Enabled = active;
        _FindSimilarArgInput.Enabled = active;
        _DeleteDuplicatesButton.Enabled = active;

        _StopButton.Enabled = _Running;
    }

    private void RunCommand(Func<Task?> cmd)
    {
        lock (this)
        {
            if (!_Running && _Data.Tagbag != null)
            {
                var task = cmd.Invoke();
                if (task != null)
                {
                    _Running = true;
                    AdjustButtons();
                    task.ContinueWith((_) => {
                        lock (this)
                        {
                            _Running = false;
                            AdjustButtons();
                            RenderProgress();
                        }
                    });
                }
            }
        }
    }

    private Task? ClickPopulateHashes()
    {
        return _DuplicationDetection?.PopulateHashes();
    }

    private Task? ClickFindSimilar()
    {
        var thresholdValues = GetThresholdValue();
        if (thresholdValues != null)
            return _DuplicationDetection?.FindSimilarHashes(thresholdValues.Item2);
        return null;
    }

    private Task? ClickDeleteDuplicates()
    {
        return _DuplicationDetection?.DeleteDuplicates();
    }

    // Returns number as string, threshold value, is-percent
    private Tuple<string, int, bool>? GetThresholdValue()
    {
        var text = "";
        var isPercent = false;
        var ok = true;

        foreach (var c in _FindSimilarArgInput.Text)
        {
            if (c >= '0' && c <= '9')
                text += c;
            else if (c == '%')
                isPercent = true;
            else
                ok = false;
        }

        var threshold = 0;
        if (ok && text.Length > 0)
        {
            System.Console.WriteLine(text);
            threshold = int.Parse(text);
            if (isPercent)
                threshold = (int)((float)DuplicationDetection.MaxThreshold * ((float)threshold / 100.0f));
        }

        if (threshold < 0 || DuplicationDetection.MaxThreshold < threshold)
            ok = false;

        if (ok)
            return new Tuple<string, int, bool>(text, threshold, isPercent);
        else
            return null;
    }

    private void UpdateFindSimilar()
    {
        var thresholdValues = GetThresholdValue();

        if (thresholdValues != null)
        {
            var (text, threshold, isPercent) = thresholdValues.ToValueTuple();

            _FindSimilarArgInput.BackColor = GuiTool.BackColor;

            var percentText = text;
            if (!isPercent)
                percentText = (threshold * 100 / DuplicationDetection.MaxThreshold).ToString();
            _FindSimilarArgDescription.Text =
                $"{threshold} / {DuplicationDetection.MaxThreshold} ({percentText}%)";
        }
        else
        {
            _FindSimilarArgInput.BackColor = GuiTool.BackColorAlt;
            _FindSimilarArgDescription.Text = "malformed";
        }
    }

    private void RenderProgress()
    {
        var (activity, current, goal) = _ProgressCache;

        var a = activity.ToString();
        var c = current.ToString();
        var g = goal.ToString();

        if (_ProgressActivity.Text != a)
            _ProgressActivity.Text = a;

        if (_ProgressCurrent.Text != c)
            _ProgressCurrent.Text = c;

        if (_ProgressGoal.Text != g)
            _ProgressGoal.Text = g;
    }

    private void ListenProgressReport(DuplicationDetection.Activity activity,
                                      int current,
                                      int goal)
    {
        _ProgressCache = (activity, current, goal);

        var now = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (now - _LastReportUpdate > 100)
        {
            _LastReportUpdate = now;
            RenderProgress();
        }
    }
}
