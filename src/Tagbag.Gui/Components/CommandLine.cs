using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Tagbag.Core;
using Tagbag.Core.Input;

namespace Tagbag.Gui.Components;

public class CommandLine : Panel
{
    private EventHub _EventHub;
    private bool _Enabled;

    private Label _ModeLabel;
    private TextBox _TextBox;
    private RichLabel _StatusLabel;

    private History _History;
    private int _LastTagMode;

    public CommandLine(EventHub eventHub)
    {
        _EventHub = eventHub;
        _StatusLabel = new RichLabel();
        _TextBox = new TextBox();
        _ModeLabel = new Label();

        GuiTool.Setup(this);
        GuiTool.Setup(_StatusLabel);
        GuiTool.Setup(_TextBox);
        GuiTool.Setup(_ModeLabel);
        LayoutComponents();

        _TextBox.KeyDown += HandleKeyDown;
        _TextBox.KeyUp += HandleKeyUp;

        _History = new History();

        SetEnabled(true);
        UpdateMode(force: true);

        eventHub.Log += ListenLog;
        eventHub.ShowFilter += ListenShowFilter;
    }

    private void LayoutComponents()
    {
        var pad = 5;

        _StatusLabel.Name = "CommandLine:StatusLabel";
        _StatusLabel.Dock = DockStyle.Fill;
        _StatusLabel.Left = pad * 10;
        _StatusLabel.Font = new Font("Verdana", 14);
        Controls.Add(_StatusLabel);

        _TextBox.Name = "CommandLine:TextBox";
        _TextBox.Dock = DockStyle.Left;
        _TextBox.Width = 300;
        _TextBox.Font = new Font("Arial", 16);
        _TextBox.Multiline = true;
        _TextBox.AcceptsTab = true;
        _TextBox.Height = _TextBox.Font.Height + 4;
        _TextBox.ForeColor = Color.Black;
        _TextBox.BackColor = Color.White;
        Controls.Add(_TextBox);

        _ModeLabel.Name = "CommandLine:ModeLabel";
        _ModeLabel.Dock = DockStyle.Left;
        _ModeLabel.Top = pad;
        _ModeLabel.Left = pad;
        _ModeLabel.Font = new Font("Arial", 14);
        _ModeLabel.TextAlign = ContentAlignment.MiddleCenter;
        Controls.Add(_ModeLabel);

        ClientSizeChanged += (_, _) =>
        {
            _TextBox.Width = Math.Max(300, Width / 4);
            Height = _TextBox.Font.Height + 4 + 2 * pad;
        };
    }

    public void SetEnabled(bool enabled)
    {
        _Enabled = enabled;
        _TextBox.Enabled = enabled;
        UpdateMode(force: true);
    }

    public bool IsEnabled()
    {
        return _Enabled;
    }

    new public void Focus()
    {
        _TextBox.Focus();
    }

    private void ListenLog(Log log)
    {
        _StatusLabel.Clear();
        _StatusLabel.AddText(" ");
        _StatusLabel.AddText(System.DateTime.Now.ToShortTimeString(), GuiTool.ForeColorDisabled);

        switch (log.Type)
        {
            case LogType.Info:
                //_StatusLabel.AddText("Info", Color.Green);
                break;

            case LogType.Error:
                _StatusLabel.AddText("ERROR", Color.Red);
                break;
        }

        _StatusLabel.AddText(" ");
        _StatusLabel.AddText(log.Message, GuiTool.ForeColor);
    }

    private void ListenShowFilter(ShowFilter sf)
    {
        _TextBox.Text = CommandBuilder.FilterIndicator + sf.Filter.ToString();
    }

    public void PerformCommand()
    {
        var txt = _TextBox.Text;
        if (txt.Trim().Length == 0)
        {
            _TextBox.Text = "";
        }
        else if (txt.Length > 0)
        {
            _History.Add(txt);
            try
            {
                var selection = CommandBuilder.Build(txt);

                if (selection.Item1 is IDataAction action)
                    _EventHub.Send(new DataActionCommand(action));
                else if (selection.Item2 is ITagOperation tagOperation)
                    _EventHub.Send(new TagCommand(tagOperation));
                else if (selection.Item3 is IFilter filter)
                    _EventHub.Send(new FilterCommand(filter));

                _TextBox.Text = "";
            }
            catch (BuildException e)
            {
                _EventHub.Send(new Log(LogType.Error, e.FullMessage()));
            }
            catch (ArgumentException e)
            {
                _EventHub.Send(new Log(LogType.Error, e.Message));
            }
        }
    }

    private void HandleKeyDown(Object? o, KeyEventArgs e)
    {
        switch (e.KeyData)
        {
            case Keys.Enter:
                e.SuppressKeyPress = true;
                PerformCommand();
                break;

            case Keys.Up:
                e.SuppressKeyPress = true;
                if (_History.Next() is string s)
                    _TextBox.Text = s;
                break;

            case Keys.Down:
                e.SuppressKeyPress = true;
                _TextBox.Text = _History.Prev();
                break;
        }
    }

    private void HandleKeyUp(Object? o, KeyEventArgs e)
    {
        UpdateMode();
    }

    private void UpdateMode(bool force = false)
    {
        int tagMode;
        if (CommandBuilder.IsFilter(_TextBox.Text))
            tagMode = 1;
        else if (CommandBuilder.IsCommand(_TextBox.Text))
            tagMode = 2;
        else
            tagMode = 3;

        if (tagMode == _LastTagMode && !force)
            return;

        _LastTagMode = tagMode;
        if (_Enabled)
        {
            switch (_LastTagMode)
            {
                case 1:
                    _ModeLabel.Text = "FILTER";
                    _ModeLabel.BackColor = GuiTool.BackColorFilter;
                    break;
                case 2:
                    _ModeLabel.Text = "COMMAND";
                    _ModeLabel.BackColor = GuiTool.BackColorCommand;
                    break;
                case 3:
                    _ModeLabel.Text = "TAG";
                    _ModeLabel.BackColor = GuiTool.BackColorTag;
                    break;
            }
        }
        else
        {
            _ModeLabel.BackColor = GuiTool.BackColorDisabled;
            _ModeLabel.Text = "";
        }
    }

    private class History
    {
        private LinkedList<String> _History;
        private LinkedListNode<String>? _ScrollBackPosition;

        public History()
        {
            _History = new LinkedList<String>();
            _ScrollBackPosition = null;
        }

        public void Reset()
        {
            _ScrollBackPosition = null;
        }

        public void Add(string cmd)
        {
            cmd = cmd.Trim();
            _History.Remove(cmd);
            _History.AddFirst(cmd);
            _ScrollBackPosition = null;
        }

        public string? Next()
        {
            if (_ScrollBackPosition == null)
                _ScrollBackPosition = _History.First;
            else
                _ScrollBackPosition = _ScrollBackPosition.Next ?? _ScrollBackPosition;

            return _ScrollBackPosition?.Value;
        }

        public string? Prev()
        {
            if (_ScrollBackPosition == null)
                return null;
            else
                _ScrollBackPosition = _ScrollBackPosition.Previous;

            return _ScrollBackPosition?.Value;
        }
    }
}

public static class CommandBuilder
{
    public static string FilterIndicator = ":";
    private static string CommandIndicator = "!";

    public static Tuple<IDataAction?, ITagOperation?, IFilter?> Build(string input)
    {
        if (IsFilter(input))
        {
            var parts = input.Split(FilterIndicator, 2);
            var cleanInput = parts[0] + " " + parts[1];
            return new Tuple<IDataAction?, ITagOperation?, IFilter?>(
                null, null, FilterBuilder.Build(cleanInput));
        }

        if (IsCommand(input))
        {
            var parts = input.Split(CommandIndicator, 2);
            var cleanInput = parts[0] + " " + parts[1];
            return new Tuple<IDataAction?, ITagOperation?, IFilter?>(
                Build(Tokenizer.GetTokens(cleanInput)), null, null);
        }

        if (input.Trim().Length > 0)
            return new Tuple<IDataAction?, ITagOperation?, IFilter?>(
                null, TagBuilder.Build(input), null);

        return new Tuple<IDataAction?, ITagOperation?, IFilter?>(null, null, null);
    }

    public static bool IsFilter(string input)
    {
        return input.TrimStart().StartsWith(FilterIndicator);
    }

    public static bool IsCommand(string input)
    {
        return input.TrimStart().StartsWith(CommandIndicator);
    }

    public static IDataAction Build(LinkedList<Token> tokens)
    {
        if (tokens.First?.Value is Token command)
        {
            if (command.Type == TokenType.Symbol)
            {
                switch (command.Text)
                {
                    case "sort":
                        return BuildSort(tokens);
                    default:
                        throw new BuildException("Unknown command").With(command);
                }
            }

            throw new BuildException("Command must be a symbol").With(command);
        }

        throw new ArgumentException("No command given");
    }

    private static IDataAction BuildSort(LinkedList<Token> tokens)
    {
        var node = tokens.First;

        string? command = node?.Value.Text;
        string? tag;
        string order = "asc";
        string? lookup = null;
        var extra = new List<string>();

        node = node?.Next;

        tag = node?.Value.Text;

        node = node?.Next;

        for (; node != null; node = node?.Next)
        {
            switch (node.Value.Text)
            {
                case "asc":
                case "desc":
                    order = node.Value.Text;
                    break;
                case "int":
                case "str":
                    lookup = node.Value.Text;
                    break;
                default:
                    extra.Add(node.Value.Text);
                    break;
            }
        }

        if (command != "sort")
            throw new ArgumentException("Not a sort command");

        if (extra.Count > 0)
            throw new ArgumentException(
                $"Unknown sort arguments: {String.Join(" ", extra)}");

        if (tag == null)
            tag = "path";

        Comparison<Entry> comp;
        switch (lookup)
        {
            case "int":
                comp = (a, b) => CompareInt(a, b, tag, order == "asc");
                break;

                case "str":
                    comp = (a, b) => CompareString(a, b, tag, order == "asc");
                    break;

                default:
                    comp = (a, b) =>
                    {
                        var diff = CompareInt(a, b, tag, order == "asc");
                        if (diff == 0)
                            diff = CompareString(a, b, tag, order == "asc");
                        return diff;
                    };
                    break;
            }

        return new GenericDataAction(
            $"Sort by \"{tag}\" {order}",
            (Data data) => data.EntryCollection.SetSorting(comp));
    }

    private static int CompareInt(Entry a, Entry b, string tag, bool asc)
    {
        var defaultInt = asc ? int.MaxValue : int.MinValue;
        var aInt = a.GetIntBy(tag, int.Min, otherwise: defaultInt);
        var bInt = b.GetIntBy(tag, int.Min, otherwise: defaultInt);

        if (a.GetMeta(tag) is Value aVal)
            foreach (var i in aVal.GetInts() ?? [])
                aInt = int.Min(aInt, i);

        if (b.GetMeta(tag) is Value bVal)
            foreach (var i in bVal.GetInts() ?? [])
                bInt = int.Min(bInt, i);

        if (aInt < bInt)
            return asc ? -1 : 1;
        if (aInt > bInt)
            return asc ? 1 : -1;
        return 0;
    }

    private static int CompareString(Entry a, Entry b, string tag, bool asc)
    {
        var aStrs = new List<string>(a.GetStrings(tag) ?? []);
        var bStrs = new List<string>(b.GetStrings(tag) ?? []);

        if (Const.MetaTags.Contains(tag))
        {
            aStrs.AddRange(a.GetMeta(tag)?.GetStrings() ?? []);
            bStrs.AddRange(b.GetMeta(tag)?.GetStrings() ?? []);
        }

        aStrs.Sort();
        bStrs.Sort();

        var aVal = aStrs.Count > 0 ? aStrs[0] : null;
        var bVal = bStrs.Count > 0 ? bStrs[0] : null;
        return String.Compare(aVal, bVal, ignoreCase: true) * (asc ? 1 : -1);
    }
}

public interface IDataAction
{
    public Action<Data> GetAction();
}

public class GenericDataAction : IDataAction
{
    private string _Text;
    private Action<Data> _Action;

    public GenericDataAction(string text, Action<Data> action)
    {
        _Text = text;
        _Action = action;
    }

    public Action<Data> GetAction() { return _Action; }
    override public string ToString() { return _Text; }
}
