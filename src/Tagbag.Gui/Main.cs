using System;
using System.Windows.Forms;

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
        // foreach (var entry in tb.GetEntries())
        // {
        //     Tagbag.Core.TagbagUtil.PopulateImageTags(tb, entry);
        //     Tagbag.Core.TagbagUtil.PopulateFileTags(tb, entry);
        // }
        // tb.Save();

        ApplicationConfiguration.Initialize();
        Application.Run(new Root(tb));
    }    
}
