using System;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui;

static class Program
{
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI
        // settings or default font, see
        // https://aka.ms/applicationconfiguration.

        Tagbag.Core.Tagbag tb = Tagbag.Core.Tagbag.Open("test-data");
        //var scan = new Scanner(tb).PopulateAllTags();
        //scan.Scan(".");
        //tb.Save();
        // var y = 0;
        // var x = 1 / y;

        Json.PrettyPrint(tb);

        ApplicationConfiguration.Initialize();
        Application.Run(new Root());
    }    
}
