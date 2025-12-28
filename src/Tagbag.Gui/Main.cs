using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui;

static class Program
{
    [STAThread]
    static void Main(string []args)
    {
        // To customize application configuration such as set high DPI
        // settings or default font, see
        // https://aka.ms/applicationconfiguration.

        // Tagbag.Core.Tagbag tb = Tagbag.Core.Tagbag.Open("test-data");
        // var scan = new Scanner(tb).PopulateAllTags().Recursive();
        // scan.Scan(".");
        // tb.Save();
        // var y = 0;
        // var x = 1 / y;

        // Json.PrettyPrint(tb);

        if (args.Length == 1 && args[0] == "--gen-doc")
        {
            //GenDoc();
            GenDoc2();
        }
        else
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Root());
        }
    }

    static void GenDoc2()
    {
        KeyMap km = new KeyMap();
        Root.SetupActionDefinitions(km);
        Root.SetupKeyMap(km);

        List<ActionDef> actions = new List<ActionDef>(km.GetActionDefs());
        actions.Sort((a, b) => String.Compare(a.Id, b.Id));

        List<KeyData> keyData = new List<KeyData>(km.GetKeyData());
        keyData.Sort((a, b) => String.Compare(a.Key.ToString(), b.Key.ToString()));

        var sb = new System.Text.StringBuilder();

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

            table.WriteTo(sb);
        };

        sb.AppendLine("Common keys");
        doc(new string[]{"Action", "Key"},
            keyData => {
                if (keyData.Mode == null) return 1;
                else                      return -1;
            });

        sb.AppendLine("\nGrid keys");
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

        sb.AppendLine("\nSingle keys");
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

        System.IO.File.WriteAllText("readme.txt", sb.ToString());
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

        public void WriteTo(System.Text.StringBuilder sb)
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

            sb.AppendLine(lineBreak);
            for (row = 0; row < rowCount; row++)
            {
                for (int lineIndex = 0; lineIndex < lineCount[row]; lineIndex++)
                {
                    for (int col = 0; col < colCount; col++)
                    {
                        var lines = GetLines(row, col);
                        sb.Append(" | ");
                        if (lineIndex < lines.Length)
                            sb.Append(SetWidth(lines[lineIndex], maxWidth[col]));
                        else
                            sb.Append(SetWidth("", maxWidth[col]));
                    }
                    sb.AppendLine(" |");
                }

                if (_LineBreaks[row])
                    sb.AppendLine(lineBreak);
            }
            if (!_LineBreaks[row])
                sb.AppendLine(lineBreak);
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
