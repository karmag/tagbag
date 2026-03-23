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
        TextBlock(
            @"Tagbag

    Tagbag connects keywords with images.

        - Tags are keywords with optional values.
        - Images are not manipulated.
        - Tags are stored in the nearest .tagbag file.

    New tagbag

        To setup a new tagbag file:

            1. Use ""New..."" to select a new tagbag root.
            2. Switch to ""Scan / Problems"" mode.
            3. Run ""Scan"" and then ""Fix"" to import images.
            4. Save and start tagging.

    Common controls

        Enter       - Goto / execute command line.
        Escape      - Pop last filter.
        Control + S - Save.

        Tab             - Switch Grid / Single image display.
        Control + Enter - Switch Command / Browse mode.
        Control + T     - Switch Tags / Tag-summary mode.

        Control + Q        - Clear marks.
        Control + A        - Mark all visible images.
        Space              - Mark image.
        Shift + Arrow keys - Mark image and move.

        Controls change depending on mode, see full listing below.

    Command line

        Tag examples:

            tag                 - Add tag.
            tag value           - Add tag with value.
            tag ""another value"" - Add value with whitespace.
            tag 15              - Add tag that is number.
            -tag                - Remove tag and any related values.

        Filter examples:

            :tag       - Find images that have tag.
            :tag value - Find images where tag = value.
            :tag < 15  - Find images where tag value is < 15.

        See full tagging and filtering syntax below.

    Status bar

        38% --- 279 / 349 --- 60 marked --- [level = 5]
        [a]        [b]           [c]            [d]

            a - Position within the currently visible images. Shows
            ""Top"", ""Bot"", or a percentage.

            b - Visible-images / all-images.

            c - Number of marked images.

            d - Current filters.
");

        Paragraph(0, "Specifics");

        Paragraph(1, ".tagbag");
        Paragraph(2, @"
The .tagbag file contains all the information about tags and images.
It's indicating a root position for the tagbag and all sub-directories
are considered when scanning for images to include.");
        Paragraph(2, @"
The .tagbag file can be created through the application or by placing
an empty .tagbag file in a directory.");

        Paragraph(1, "Browse / command");
        Paragraph(2, @"
Browse mode is focused on navigating entries. Command mode is focused
on manipulating tags. Both modes support most functions, the main
difference is in the amount of modifier keys or key strokes required
to perform an action.");
        Paragraph(2, @"
Swap between browse and command mode with [Control + Enter].");

        Paragraph(1, "Marks");
        Paragraph(2, @"
Marking allows you to apply tag commands to multiple entries at once.
When one or more entries are marked all tag commands are applied to
all those entries. Marks persist until removed and are not affected by
filters.");
        Paragraph(2, @"
Entries can be marked with [Space] and [Shift + Arrow Key]. See the
mark/* and mark-and-move/* actions for further options.");
        Paragraph(2, @"
Use [Control + Q] to clear all marks. The number of marks are
displayed in the status bar.");

        Paragraph(1, "Scan / Problems");
        Paragraph(2, @"
Scanning is used to add images and to find a number of problems. When
images are added they are populated with width, height, filesize, and
hash/sha256 tags. These tags are used to identify the image in problem
fixing and duplication detection.");
        Paragraph(3, @"
File moved - A file has been moved within the tagbag sub-directories.
The corresponding entry is updated to point at the new location.");
        Paragraph(3, @"
File missing - A file that is deleted or moved out of the tagbag
sub-directory. The corresponding entry is deleted.");
        Paragraph(3, @"
Duplicate files - Two, or more, files are binary equivalent. All but
one of them are removed and the tags from all entries are merged into
the remaining entry.");

        Paragraph(1, "Duplicate detection");
        Paragraph(2, @"
Finds images with similar color profile. This can find images that
have been resized, flipped, rotated, and cropped. Duplicates are found
and removed in a three step process.");
        Paragraph(3, @"
Populate hashes - This step adds a hash/color tag to each image for
use in comparisons. Only images that lacks the tag are populated.");
        Paragraph(3, @"
Find matches - Uses the color information to find similar images.
Images who's similarity is smaller than the given threshold are tagged
with the tag ""duplicate"" and a number. At this point you should verify
the findings and remove false positives manually.");
        TextBlock(@"
                Use   :duplicate
                and   !sort-int duplicate
                in the command line to display matches.
");
        Paragraph(3, @"
Delete duplicates - For each group of duplicates (entries with the
same value in their duplicate tag are a group) deletes all but one of
them. Deleted files are moved to the recycle bin. Tags in deleted
entries are merged into the remaining entry. The surviving file is
chosen based on image dimensions, filesize, directory depth, and path
length - in that order.");
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
