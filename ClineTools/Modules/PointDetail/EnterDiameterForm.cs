using System;
using System.Windows.Forms;

namespace ClineTools.Modules.PointDetail.UI
{
    public sealed class EnterDiameterForm : Form
    {
        private readonly TextBox _txtDia = new TextBox();
        private readonly ComboBox _cmbUnit = new ComboBox();
        private readonly Button _btnBack = new Button();
        private readonly Button _btnInsert = new Button();
        private readonly Button _btnCancel = new Button();

        public string DiameterText => _txtDia.Text.Trim();
        public string SelectedUnit => _cmbUnit.SelectedItem?.ToString() ?? "in";

        public EnterDiameterForm()
        {
            Text = "Insert Point Detail";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(420, 170);

            var lbl = new Label
            {
                Text = "Enter Point Diameter",
                AutoSize = true
            };

            _txtDia.Width = 260;

            _cmbUnit.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbUnit.Items.AddRange(new object[] { "in", "mm" });
            _cmbUnit.SelectedIndex = 0;
            _cmbUnit.Width = 110;

            _btnBack.Text = "Back";
            _btnBack.Width = 90;
            _btnBack.DialogResult = DialogResult.Retry;

            _btnCancel.Text = "Cancel";
            _btnCancel.Width = 90;
            _btnCancel.DialogResult = DialogResult.Cancel;

            _btnInsert.Text = "Insert Detail";
            _btnInsert.Width = 140;
            _btnInsert.DialogResult = DialogResult.OK;

            AcceptButton = _btnInsert;
            CancelButton = _btnCancel;

            var inputRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12, 8, 12, 0),
                WrapContents = false
            };
            inputRow.Controls.Add(_txtDia);
            inputRow.Controls.Add(_cmbUnit);

            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(12, 0, 12, 12),
                WrapContents = false
            };
            buttonRow.Controls.Add(_btnBack);
            buttonRow.Controls.Add(_btnCancel);
            buttonRow.Controls.Add(_btnInsert);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(12, 10, 12, 0)
            };
            lbl.Left = 0;
            lbl.Top = 0;
            header.Controls.Add(lbl);

            Controls.Add(buttonRow);
            Controls.Add(inputRow);
            Controls.Add(header);
        }
    }
}