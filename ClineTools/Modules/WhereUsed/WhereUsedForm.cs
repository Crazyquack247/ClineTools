using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ClineTools.Modules.WhereUsed
{
    public sealed class WhereUsedForm : Form
    {
        private readonly TextBox _tb;
        private readonly Label _lbl;

        public WhereUsedForm(string partPath, IList<string> assemblies)
        {
            Text = "Where Used";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            _lbl = new Label
            {
                Dock = DockStyle.Top,
                Height = 42,
                Text = $"PART:\r\n{partPath}"
            };

            _tb = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new System.Drawing.Font("Consolas", 9f)
            };

            var close = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                Text = "Close"
            };
            close.Click += (s, e) => Close();

            Controls.Add(_tb);
            Controls.Add(_lbl);
            Controls.Add(close);

            Load += (s, e) =>
            {
                if (assemblies == null || assemblies.Count == 0)
                {
                    _tb.Text = "(No assemblies found that reference this part.)";
                    return;
                }

                _tb.Text = string.Join(Environment.NewLine, assemblies);
            };
        }
    }
}