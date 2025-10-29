using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swcommands;
using System;
using System.IO;
using System.Windows.Forms;
using ClineTools.Modules.OnSave;

namespace ClineTools.Modules.RakeGen
{
    internal class RakeGenModule
    {
        private ISldWorks swApp;
        private readonly int _3dSketch = (int)swCommands_e.swCommands_3DSketch;
        private readonly int _InsertLine = (int)swCommands_e.swCommands_Line;

        //public void Initialize(ISldWorks swApp)
        //{
        //    this.swApp = swApp;

        //    // Attach to SolidWorks event interface

        //    ((DSldWorksEvents_Event)swApp).CommandOpenPreNotify += OnCommandPre;
        //}

        //public void Terminate()
        //{
        //    ((DSldWorksEvents_Event)swApp).CommandOpenPreNotify -= OnCommandPre;
        //}

        //private int OnCommandPre(int command, int userActivationType)
        //{ 
        
        //}
    }
}
