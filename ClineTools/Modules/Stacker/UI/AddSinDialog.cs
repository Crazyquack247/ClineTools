using System.Windows.Forms;

namespace ClineTools.Modules.Stacker.UI
{
    public partial class AddSinDialog : Form
    {
        public AddSinDialog()
        {
            InitializeComponent();
        }

        public string EnteredSin => txtSin.Text;
    }
}