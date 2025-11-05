using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
