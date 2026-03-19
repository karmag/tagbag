using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class Options : Panel
{
    private Config _Config;

    public Options(Config config)
    {
        _Config = config;

        GuiTool.Setup(this);

        LayoutComponents();
    }

    private void LayoutComponents()
    {
        var values = new List<ConfigValue<int>>(_Config.GetValues());
        var font = new Font(FontFamily.GenericMonospace, 14);
        const int height = 30;

        var table = new TableLayoutPanel();
        GuiTool.Setup(table);
        table.RowCount = values.Count;
        table.ColumnCount = 3;
        table.Dock = DockStyle.Fill;

        foreach (var cv in values)
        {
            var label = new Label();
            GuiTool.Setup(label);
            label.Text = cv.Name;
            label.Font = font;
            label.Height = height;
            label.Width = 400;

            var input = new TextBox();
            GuiTool.Setup(input);
            input.Font = font;
            input.Height = height;
            input.TextAlign = HorizontalAlignment.Right;

            var reset = new Button();
            GuiTool.Setup(reset);
            reset.Text = "Reset";
            reset.Font = font;
            reset.Height = height;

            SetupRowBehavior(cv, label, input, reset);

            table.Controls.Add(label);
            table.Controls.Add(input);
            table.Controls.Add(reset);
        }

        var save = new Button();
        GuiTool.Setup(save);
        save.Text = "Save";
        save.Font = font;
        save.Height = height;
        save.Click += (_, _) => { _Config.Save(); };

        table.Controls.Add(new Label());
        table.Controls.Add(new Label());
        table.Controls.Add(save);

        Controls.Add(table);
    }

    private void SetupRowBehavior(ConfigValue<int> cv,
                                  Label label,
                                  TextBox input,
                                  Button reset)
    {
        var setInput = (string s) =>
        {
            try
            {
                var i = int.Parse(s);
                if (cv.Set(i) is string msg)
                {
                    label.Text = msg;
                    label.BackColor = Color.DarkRed;
                    reset.Enabled = true;
                }
                else
                {
                    label.Text = cv.Name;
                    label.BackColor = GuiTool.BackColor;
                    input.Text = s;
                    reset.Enabled = !cv.IsDefault();
                }
            }
            catch (Exception)
            {
                label.Text = "Malformed integer value";
                label.BackColor = Color.DarkRed;
                reset.Enabled = true;
            }
        };

        input.TextChanged += (_, _) => { setInput(input.Text); };
        reset.Click += (_, _) => { cv.Reset(); };
        cv.Changed += (_, i) => { setInput(i.ToString()); };

        setInput(cv.Get().ToString());
    }
}
