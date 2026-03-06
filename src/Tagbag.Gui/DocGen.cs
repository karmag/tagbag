using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;

namespace Tagbag.Gui;

public class DocGen
{
    private KeyMap km;
    private StringBuilder sb;

    public DocGen(KeyMap keyMap)
    {
        km = keyMap;
        sb = new StringBuilder();

        Intro();
        ChapterBreak();

        TagHelp();
        ChapterBreak();

        FilterHelp();
        ChapterBreak();

        KeyTables();
        ChapterBreak();

        BuildCommands();
    }

    public string Get()
    {
        return sb.ToString();
    }

    private void NewLine(string text = "")
    {
        sb.Append(text);
        sb.Append("\n");
    }

    private void EnsureEmptyLine()
    {
        var count = 0;
        for (int i = sb.Length - 1; i >= 0; i--)
            if (sb[i] == '\n')
                count++;
            else
                break;

        count = 2 - int.Min(2, count);
        for (int i = 0; i < count; i++)
            NewLine();
    }

    private void TextBlock(string text)
    {
        sb.Append(text.Replace("\r", ""));
    }

    private void Paragraph(int indentLevels, string text)
    {
        EnsureEmptyLine();

        var allWords = new List<String>(
            text.Split(new string[]{" ", "\n", "\r"},
                       StringSplitOptions.RemoveEmptyEntries |
                       StringSplitOptions.TrimEntries));

        var indentString = "    ";
        const int max = 60;
        var count = 0;
        var firstLine = true;

        foreach (var word in allWords)
        {
            if (count + word.Length > max || firstLine)
            {
                if (firstLine)
                    firstLine = false;
                else
                    NewLine();

                count = 0;
                for (int i = 0; i < indentLevels; i++)
                {
                    sb.Append(indentString);
                    count += indentString.Length;
                }
            }

            if (sb[sb.Length - 1] != ' ' && sb[sb.Length - 2] != '\n')
            {
                sb.Append(" ");
                count++;
            }

            sb.Append(word);
            count += word.Length;
        }

        NewLine();
    }

    private void ChapterBreak()
    {
        NewLine();
        NewLine("----------------------------------------------------------------------");
        NewLine();
    }

    private void Intro()
    {
        NewLine("Tagbag");
        NewLine();

        Paragraph(1, "Tagbag is an application for tagging images.");
        NewLine();

        TextBlock(
            @"        - Tagging is manual.
        - The images are not manipulated.
        - Tags are stored in the nearest .tagbag file.
        - Primarily keyboard oriented.
");

        Paragraph(0, "Tags");
        Paragraph(1, @"
To manipulate tags enter commands in the input field. Tags consist of
a key and any number of values, including none at all.");
        Paragraph(1, @"
Use [Enter] to access the input field and to execute commands. To see
a summary of tags press [Ctrl + T].");

        Paragraph(0, "Filters");
        Paragraph(1, @"
Filters limit the amount of entries shown. To add a filter prefix it
in the input field with a colon. Filters goes on a stack and can be
popped one at a time. Current filters are shown in the status bar.");
        Paragraph(1, @"
Use [Escape] to pop the current filter. The number of visible entries
are shown in the status bar.");

        Paragraph(0, "Marks");
        Paragraph(1, @"
Marking allows you to apply tag commands to multiple entries at once.
When one or more entries are marked all tag commands are applied to
all those entries. Marks persist until removed and are not affected by
filters.");
        Paragraph(1, @"
Entries can be marked with [Space] and [Shift + Arrow Key]. See the
mark/* and mark-and-move/* actions for further options.");
        Paragraph(1, @"
Use [Ctrl + Q] to clear all marks. The number of marks are displayed
in the status bar.");

        Paragraph(0, "Browse / command");
        Paragraph(1, @"
Browse mode is focused on navigating entries. Command mode is focused
on manipulating tags. Both modes support most functions, the main
difference is in the amount of modifier keys or key strokes required
to perform an action.");
        Paragraph(1, @"
Swap between browse and command mode with [Ctrl + Enter].");

        Paragraph(0, "Grid / single");
        Paragraph(1, @"
Grid mode show multiple entries. Single mode shows one entry.");
        Paragraph(1, @"
Swap between grid and single with [Tab].");

        Paragraph(0, "Scan");
        Paragraph(1, @"
Scanning is used to add images to the tagbag as well as fix problems.
Access the Scan page with [F2] or the menu. Use [F1] to return to grid
view.");

        Paragraph(0, "Save");
        Paragraph(1, @"
Modifications to entries needs to be saved manually. Use [Ctrl + S] or
the menu to save.");
    }

    private void BuildCommands()
    {
        TextBlock(
            @"Build commands
    Run              : dotnet run <optional-initial-path>
    Build executable : dotnet publish
    Test             : dotnet test
    Generate readme  : dotnet run --gen-doc

Standalone executable is located at
    bin\Release\net9.0-windows\win-x64\publish\tagbag.exe");
        NewLine();
    }

    private void TagHelp()
    {
        TextBlock(
            @"Tagging

    Tag grammar
        tag-command = unary | binary | ternary
        unary       = ['+' | '-'] tag
        binary      = tag value
        ternary     = tag op value
        tag         = symbol
        op          = '+' | '-' | '='
        value       = int | string | symbol

        Operators
            + : Add the value to tag.
            - : Remove the value from tag.
            = : Set the tag to have value, removing other values.

    Examples
        river
            Add the tag 'river' to the entry.
        score 10
            Add the value 10 to the tag 'score'.
        author = 'Quill Penhammer'
            Set the 'author' tag to be 'Quill Penhammer' overwriting
            any old values.
        -normal
            Remove the tag normal.");
        NewLine();
    }

    private void FilterHelp()
    {
        TextBlock(
            @"Filtering

    Filter grammar
        filter-command = unary | binary | ternary | negated
        unary          = tag
        binary         = tag value
        ternary        = tag op value
        negated        = 'not' (unary | binary | ternary)
        tag            = symbol
        op             = '=' | '~=' | '<' | '<=' | '>' | '>='
        value          = int | string | symbol

        A string is characters enclosed in doublequotes. A symbol is a
        letter followed by other non-whitespace characters. A symbol
        will be interpreted as a string in relevant contexts.

        Operators
            =            : equality
            ~=           : regular expression, ignores case
            <, <=, >, >= : general math operators

    Examples
        cloud
            Find any entry with the tag 'cloud' regardless of values.
        score 4
            Find any entry where the tag 'score' is equal to 4.
        year > 2000
            Find any entry where the tag 'year' is greater than 2000.
        not good
            Find any entry that doesn't have the tag 'good'.");
        NewLine();
    }

    private void KeyTables()
    {
        List<ActionDef> actions = new List<ActionDef>(km.GetActionDefs());
        actions.Sort((a, b) => String.Compare(a.Id, b.Id));

        List<KeyData> keyData = new List<KeyData>(km.GetKeyData());
        keyData.Sort((a, b) => String.Compare(a.Key.ToString(), b.Key.ToString()));

        var doc = (string[] headers, Func<KeyData, int> columnIndex) => {
            var table = new Table();
            var row = 1;

            for (int i = 0; i < headers.Length; i++)
                table.Set(0, i, headers[i]);
            table.BreakAfter(0);

            foreach (var ad in actions)
            {
                var hasValue = false;
                foreach (var kd in keyData)
                {
                    if (ad.Id == kd.ActionId)
                    {
                        var col = columnIndex(kd);
                        if (col != -1)
                        {
                            table.Add(row, col, kd.Key);
                            hasValue = true;
                        }
                    }
                }

                if (hasValue)
                {
                    table.Set(row, 0, ad.Id);
                    row++;
                }
            }

            table.WriteTo(this);
        };

        NewLine("Common keys");
        doc(new string[]{"Action", "Key"},
            keyData => {
                if (keyData.Mode == null) return 1;
                else                      return -1;
            });

        NewLine("\nGrid keys");
        doc(new string[] { "Action",
                          Mode.InputMode.Browse.ToString() ?? "",
                          Mode.InputMode.Command.ToString() ?? "" },
            keyData => {
                if (keyData.Mode != null &&
                    keyData.Mode.Application == Mode.ApplicationMode.Grid)
                {
                    switch (keyData.Mode.Input) {
                        case Mode.InputMode.Browse:
                            return 1;
                        case Mode.InputMode.Command:
                            return 2;
                    }
                }
                return -1;
            });

        NewLine("\nSingle keys");
        doc(new string[] { "Action",
                          Mode.InputMode.Browse.ToString() ?? "",
                          Mode.InputMode.Command.ToString() ?? "" },
            keyData => {
                if (keyData.Mode != null &&
                    keyData.Mode.Application == Mode.ApplicationMode.Single)
                {
                    switch (keyData.Mode.Input) {
                        case Mode.InputMode.Browse:
                            return 1;
                        case Mode.InputMode.Command:
                            return 2;
                    }
                }
                return -1;
            });
    }

    private class Table
    {
        private string?[,] _Cells;
        private const int MaxColumns = 3;
        private const int MaxRows = 1000;
        private bool[] _LineBreaks;

        public Table()
        {
            _Cells = new string?[MaxRows, MaxColumns];
            _LineBreaks = new bool[MaxRows];
        }

        public void Set(int row, int col, string value)
        {
            _Cells[row, col] = value;
        }

        public void Add(int row, int col, string value)
        {
            string? old = _Cells[row, col];
            if (old == null)
                _Cells[row, col] = value;
            else
                _Cells[row, col] = $"{old}\n{value}";
        }

        public void Add(int row, int col, Keys keys)
        {
            var modifiers = keys & Keys.Modifiers;
            var key = keys & Keys.KeyCode;

            var keyText = modifiers.ToString().Replace(",", " +");
            if (modifiers == 0)
                keyText = "";
            else
                keyText += $" + ";
            keyText += key.ToString();

            Add(row, col, keyText);
        }

        public void BreakAfter(int row)
        {
            _LineBreaks[row] = true;
        }

        public void WriteTo(DocGen doc)
        {
            var rowCount = -1;
            var colCount = -1;
            var lineCount = new int[MaxRows];
            var maxWidth = new int[MaxColumns];

            var row = 0;
            for (row = 0; row < MaxRows; row++)
            {
                for (int col = 0; col < MaxColumns; col++)
                {
                    if (_Cells[row, col] != null)
                    {
                        rowCount = row + 1;
                        colCount = Math.Max(colCount, col + 1);

                        var lines = GetLines(row, col);
                        lineCount[row] = Math.Max(lineCount[row], lines.Length);
                        foreach (var line in lines)
                            maxWidth[col] = Math.Max(maxWidth[col], line.Length);
                    }
                }
            }

            var lineBreak = " +";
            for (int i = 0; i < colCount; i++)
            {
                for (int j = 0; j < maxWidth[i] + 2; j++)
                    lineBreak += "-";
                lineBreak += "+";
            }

            doc.NewLine(lineBreak);
            for (row = 0; row < rowCount; row++)
            {
                for (int lineIndex = 0; lineIndex < lineCount[row]; lineIndex++)
                {
                    for (int col = 0; col < colCount; col++)
                    {
                        var lines = GetLines(row, col);
                        doc.sb.Append(" | ");
                        if (lineIndex < lines.Length)
                            doc.sb.Append(SetWidth(lines[lineIndex], maxWidth[col]));
                        else
                            doc.sb.Append(SetWidth("", maxWidth[col]));
                    }
                    doc.NewLine(" |");
                }

                if (_LineBreaks[row])
                    doc.NewLine(lineBreak);
            }
            if (!_LineBreaks[row])
                doc.NewLine(lineBreak);
        }

        private string[] GetLines(int row, int col)
        {
            if (_Cells[row, col] is string str)
            {
                return str.Split("\n");
            }
            return new string[0];
        }

        private string SetWidth(string input, int width)
        {
            while (input.Length < width)
                input = input + " ";
            return input;
        }
    }
}
