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
        var values = new List<ConfigValue>(_Config.GetValues());
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

            var reset = new Button();
            GuiTool.Setup(reset);
            reset.Text = "Reset";
            reset.Font = font;
            reset.Height = height;

            if (cv is ConfigValue<string> cvs)
            {
                var button = new Button();
                GuiTool.Setup(button);
                button.Text = "Change";
                button.Font = font;
                button.Height = height;
                button.Width = 150;

                SetupStringBehavior(cvs, label, button, reset);
                table.Controls.Add(label);
                table.Controls.Add(button);
                table.Controls.Add(reset);
            }
            else
            {
                var input = new TextBox();
                GuiTool.Setup(input);
                input.Font = font;
                input.Height = height;
                input.Width = 150;
                input.TextAlign = HorizontalAlignment.Right;

                SetupIntBehavior(cv, label, input, reset);
                table.Controls.Add(label);
                table.Controls.Add(input);
                table.Controls.Add(reset);
            }
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

    private void SetupStringBehavior(ConfigValue<string> cv,
                                     Label label,
                                     Button button,
                                     Button reset)
    {
        button.Click += (_, _) =>
        {
            var font = new Font(FontFamily.GenericMonospace, 14);

            var form = new Form();
            form.Text = cv.Name;
            form.BackColor = GuiTool.BackColorAlt;

            var panel = new Panel();
            GuiTool.Setup(panel);
            panel.Height = 60;

            form.Controls.Add(panel);
            form.ClientSizeChanged += (_, _) =>
            {
                panel.Width = form.Width - 50;
                panel.Left = 25;
                panel.Top = (form.Height - panel.Height) / 2 - panel.Height / 2;
            };
            form.Width = 700;
            form.Height = 200;

            var input = new TextBox();
            input.Font = font;
            input.Text = cv.Get();

            var accept = new Button();
            GuiTool.Setup(accept);
            accept.Font = font;
            accept.Text = "Accept";
            accept.Width = 100;
            accept.Height = 25;
            accept.Enabled = false;

            var cancel = new Button();
            GuiTool.Setup(cancel);
            cancel.Font = font;
            cancel.Text = "Cancel";
            cancel.Width = 100;
            cancel.Height = 25;

            accept.Dock = DockStyle.Right;
            panel.Controls.Add(accept);
            cancel.Dock = DockStyle.Right;
            panel.Controls.Add(cancel);
            input.Dock = DockStyle.Top;
            panel.Controls.Add(input);

            input.TextChanged += (_, _) =>
            {
                accept.Enabled = cv.Check(Text) == null && Text != cv.Get();
            };

            accept.Click += (_, _) =>
            {
                form.Close();
                cv.Set(input.Text);
                reset.Enabled = !cv.IsDefault();
            };

            cancel.Click += (_, _) => form.Close();

            form.ShowDialog();
        };

        reset.Enabled = !cv.IsDefault();
        reset.Click += (_, _) => cv.Reset();
        cv.Changed += (_, _) => reset.Enabled = !cv.IsDefault();
    }

    private void SetupIntBehavior(ConfigValue cv,
                                  Label label,
                                  TextBox input,
                                  Button reset)
    {
        var setInput = (Object val) =>
        {
            if (cv.SetRaw(val) is string msg)
            {
                label.Text = msg;
                label.BackColor = Color.DarkRed;
                reset.Enabled = true;
            }
            else
            {
                label.Text = cv.Name;
                label.BackColor = GuiTool.BackColor;
                input.Text = cv.GetRaw()?.ToString();
                reset.Enabled = !cv.IsDefault();
            }
        };

        input.TextChanged += (_, _) => { setInput(input.Text); };
        reset.Click += (_, _) => { cv.Reset(); };

        cv.ChangedRaw += (_, val) => {
            label.Text = cv.Name;
            input.Text = val.ToString();
            reset.Enabled = !cv.IsDefault();
        };

        setInput(cv.GetRaw());
    }
}
