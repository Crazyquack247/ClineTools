using SolidWorks.Interop.sldworks;
using System.Collections.Generic;
using System.Windows;

namespace INJECTOR
{
    public class ModManager
    {
        private readonly ISldWorks swApp;
        private readonly List<IModule> _modules = new List<IModule>();

        public ModManager(ISldWorks swApp)
        {
            this.swApp = swApp;
        }
        public void LoadModules()
        {
            _modules.AddRange(new IModule[]
            {
                // List modules here

                new Modules.OnSaveModule(),
            });

            foreach (var module in _modules)
                module.Initialize(swApp);
        }

        public void UnloadModules()
        {
            foreach (var module in _modules)
                module.Terminate();

            _modules.Clear();
        }

        public T GetModule<T>() where T : class, IModule
        {
            foreach (var module in _modules)
            {
                if (module is T match)
                    return match;
            }
            return null;
        }
    }
}
