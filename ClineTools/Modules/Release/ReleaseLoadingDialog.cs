using System;
using System.Windows.Forms;

namespace ClineTools.Modules.Release
{
    public class ReleaseLoadingDialog : Form
    {
        private Label lblStatus;
        private ProgressBar progress;

        public ReleaseLoadingDialog()
        {
            Width = 400;
            Height = 120;
            Text = "Processing...";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            ControlBox = false;

            lblStatus = new Label()
            {
                Text = "Working...",
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30
            };

            progress = new ProgressBar()
            {
                Style = ProgressBarStyle.Marquee,
                Dock = DockStyle.Bottom,
                Height = 25
            };

            Controls.Add(lblStatus);
            Controls.Add(progress);
        }

        public void UpdateStatus(string text)
        {
            lblStatus.Text = text;
            lblStatus.Refresh();
        }
    }
}