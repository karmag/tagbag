using System.Drawing;
using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public static class GuiTool
{
    public static Color BackColor = ColorTranslator.FromHtml("#373748");
    public static Color ForeColor = ColorTranslator.FromHtml("#e6e6e6");

    public static Color BackColorAlt = ColorTranslator.FromHtml("#453748");
    public static Color ForeColorAlt = ColorTranslator.FromHtml("#e3dbdb");

    public static Color BackColorDisabled = ColorTranslator.FromHtml("#666666");
    public static Color ForeColorDisabled = ColorTranslator.FromHtml("#999999");

    public static Control Setup(Control control)
    {
        SetColors(control);
        control.PreviewKeyDown += ForceInput;
        control.TabStop = false;
        return control;
    }

    public static Label Setup(Label label)
    {
        Setup((Control)label);
        label.TabStop = false;
        return label;
    }

    public static void Setup(Button button)
    {
        Setup((Control)button);
        button.EnabledChanged += (sender, args) =>
        {
            if (sender is Button b)
            {
                if (b.Enabled)
                {
                    b.BackColor = BackColor;
                    b.ForeColor = ForeColor;
                }
                else
                {
                    b.BackColor = BackColorDisabled;
                    b.ForeColor = ForeColorDisabled;
                }
            }
        };
    }

    public static void Setup(TextBoxBase text)
    {
        Setup((Control)text);
        text.Multiline = true;
        text.AcceptsTab = true;
    }

    public static void Setup(DataGridView table)
    {
        Setup((Control)table);
        table.DefaultCellStyle.BackColor = table.BackColor;
        table.DefaultCellStyle.ForeColor = table.ForeColor;
    }

    private static void ForceInput(object? _, PreviewKeyDownEventArgs args)
    {
        args.IsInputKey = true;
    }

    private static void SetColors(Control control)
    {
        control.BackColor = BackColor;
        control.ForeColor = ForeColor;
    }
}
