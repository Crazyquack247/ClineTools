using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolidWorks.Interop.sldworks;

namespace ClineTools.Modules.ToolWiz
{
    public class ToolWizModule : IModule
    {
        private ISldWorks _swApp;
        public ToolWizForm _form;

        public void Initialize(ISldWorks swApp)
        {
            _swApp = swApp;
        }

        public void Terminate()
        {
            if (_form != null && !_form.IsDisposed)
                _form.Close();
            _form = null;
            _swApp = null;
        }

        public void Show()
        {
            if (_form == null || _form.IsDisposed)
                _form = new ToolWizForm(_swApp); // pass SW if you later build-to-SW
            _form.Show();
            _form.BringToFront();
        }
    }
}
