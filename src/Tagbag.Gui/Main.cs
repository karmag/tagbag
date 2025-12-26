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
            KeyMap km = new KeyMap();
            Root.SetupActionDefinitions(km);
            Root.SetupKeyMap(km);

            List<KeyData> keys = new List<KeyData>(km.GetKeyData());
            keys.Sort((KeyData a, KeyData b) => {
                if ((a.Mode == null && b.Mode == null) || a.Mode == b.Mode)
                    return String.Compare(a.ActionId, b.ActionId);

                if (a.Mode == null)
                    return -1;

                if (b.Mode == null)
                    return 1;

                return String.Compare(a.Mode.ToString(), b.Mode.ToString());
            });

            bool first = true;
            Mode? mode = null;

            var sb = new System.Text.StringBuilder();
            foreach (var keyData in keys)
            {
                if (first || mode != keyData.Mode)
                {
                    first = false;
                    mode = keyData.Mode;

                    if (mode == null)
                        sb.Append("All modes:\n");
                    else
                        sb.Append($"Mode: {mode?.ToString()}\n");
                }

                var modifiers = keyData.Key & Keys.Modifiers;
                var key = keyData.Key & Keys.KeyCode;

                var keyText = modifiers.ToString().Replace(",", " +");
                if (modifiers == 0)
                    keyText = "";
                else
                    keyText += $" + ";
                keyText += key.ToString();

                sb.Append($"    {keyText,-20} -> {keyData.ActionId}\n");
            }

            System.IO.File.WriteAllText("readme.txt", sb.ToString());
        }
        else
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Root());
        }
    }
}
