using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClineTools.Modules.Stacker.UI
{
    public sealed class AssignSinPreviewForm : Form
    {
        private readonly TextBox _sinBox = new TextBox { Dock = DockStyle.Top, ReadOnly = true };
        private readonly TextBox _jsonBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false
        };
        private readonly Button _ok = new Button { Text = "Save", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 100 };
        private readonly Button _cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 100 };

        public AssignSinPreviewForm(string normalizedSin, string prettyJson)
        {
            Text = "Confirm Stacker Data Card";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(700, 500);

            _sinBox.Text = normalizedSin;
            _jsonBox.Text = prettyJson;

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 42 };
            bottom.Controls.Add(_ok);
            bottom.Controls.Add(_cancel);

            Controls.Add(_jsonBox);
            Controls.Add(_sinBox);
            Controls.Add(bottom);
            AcceptButton = _ok;
            CancelButton = _cancel;
        }
    }
}