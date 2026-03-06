using System;
using System.Windows.Forms;

namespace Tagbag.Gui;

static class Program
{
    [STAThread]
    static void Main(string []args)
    {
        if (args.Length == 1 && args[0] == "--gen-doc")
        {
            KeyMap km = new KeyMap();
            Root.SetupActionDefinitions(km);
            Root.SetupKeyMap(km);

            var doc = new DocGen(km);

            System.IO.File.WriteAllText("readme.txt", doc.Get());
        }
        else
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Root());
        }
    }
}
