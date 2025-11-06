using System.Windows.Forms;

namespace ClineTools.Modules.Stacker.UI
{
    public class StackerPaneControl : UserControl
    {
        public StackerPaneControl()
        {
            Dock = DockStyle.Fill;
            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Stacker (Assembly Only)\r\n— future: candidate list, remaining depth, insert+mate —",
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            Controls.Add(lbl);
        }
    }
}