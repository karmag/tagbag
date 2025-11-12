using System;
using System.IO;
using System.Windows.Forms;

namespace Tagbag.Gui;

public static class GuiCommand
{
    public static void NewTagbag(Data data)
    {
        using (var dialog = new FolderBrowserDialog())
        {
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            if (data.Tagbag != null)
            {
                var path = Path.GetDirectoryName(data.Tagbag.Path);
                if (path != null)
                    dialog.InitialDirectory = path;
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var path = dialog.SelectedPath;
                try
                {
                    var tb = Tagbag.Core.Tagbag.New(path);
                    data.SetTagbag(tb);
                }
                catch (ArgumentException e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }
    }

    public static void OpenTagbag(Data data)
    {
        using (var dialog = new OpenFileDialog())
        {
            if (data.Tagbag != null)
                dialog.InitialDirectory = Path.GetDirectoryName(data.Tagbag.Path);
            else
                dialog.InitialDirectory = Directory.GetCurrentDirectory();

            dialog.Filter = "tagbag files (*.tagbag)|*.tagbag";
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var path = dialog.FileName;
                data.SetTagbag(Tagbag.Core.Tagbag.Open(path));
            }
        }
    }
}
