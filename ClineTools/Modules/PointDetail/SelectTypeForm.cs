using System;
using System.Windows.Forms;

namespace ClineTools.Modules.PointDetail.UI
{
    public sealed class SelectTypeForm : Form
    {
        private readonly ListBox _list = new ListBox();
        private readonly Button _btnContinue = new Button();
        private readonly Button _btnCancel = new Button();

        public string SelectedType { get; private set; } = string.Empty;

        public SelectTypeForm()
        {
            Text = "Insert Point Detail";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(420, 340);

            var lbl = new Label
            {
                Text = "Select Type",
                AutoSize = true
            };

            _list.Dock = DockStyle.Fill;

            // TODO: replace these with your real types
            _list.Items.AddRange(new object[]
            {
                "G-Drill",
                "2 Flute Spiral Drill"
                // "3 Flute Spiral Drill"
            });

            if (_list.Items.Count > 0)
                _list.SelectedIndex = 0;

            _btnContinue.Text = "Continue";
            _btnContinue.Width = 120;
            _btnContinue.Click += (s, e) =>
            {
                if (_list.SelectedItem == null)
                {
                    MessageBox.Show(
                        "Please select a type.",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                SelectedType = _list.SelectedItem.ToString();
                DialogResult = DialogResult.OK;
                Close();
            };

            _btnCancel.Text = "Cancel";
            _btnCancel.Width = 120;
            _btnCancel.DialogResult = DialogResult.Cancel;

            AcceptButton = _btnContinue;
            CancelButton = _btnCancel;

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(12, 10, 12, 0)
            };
            header.Controls.Add(lbl);

            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(12, 8, 12, 12),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            buttonRow.Controls.Add(_btnContinue);
            buttonRow.Controls.Add(_btnCancel);

            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 4, 12, 4)
            };
            body.Controls.Add(_list);

            Controls.Add(body);
            Controls.Add(buttonRow);
            Controls.Add(header);
        }
    }
}