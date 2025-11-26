using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public static class GuiTool
{
    public static Control Setup(Control control)
    {
        control.PreviewKeyDown += ForceInput;
        control.TabStop = false;
        return control;
    }

    public static void Setup(TextBox text)
    {
        text.PreviewKeyDown += ForceInput;
        text.Multiline = true;
        text.AcceptsTab = true;
        text.TabStop = false;
    }

    public static void Setup(RichTextBox text)
    {
        text.PreviewKeyDown += ForceInput;
        text.Multiline = true;
        text.AcceptsTab = true;
        text.TabStop = false;
    }

    private static void ForceInput(object? _, PreviewKeyDownEventArgs args)
    {
        args.IsInputKey = true;
    }
}
