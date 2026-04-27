using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;

namespace ClineTools
{
    public class ModManager
    {
        private readonly ISldWorks _swApp;
        private readonly List<IModule> _modules = new List<IModule>();
        private bool _isLoaded;

        public ModManager(ISldWorks swApp)
        {
            _swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
        }

        /// <summary>
        /// Instantiate and initialize all modules. Safe to call multiple times.
        /// </summary>
        public void LoadModules()
        {
            if (_isLoaded)
                return;

            _modules.Clear();

            // -------------------------------{ Loaded Modules }-------------------------------
            
            //_modules.Add(new ClineTools.Modules.OnSaveModule());
            _modules.Add(new ClineTools.Modules.Release.ReleaseModule());
            _modules.Add(new ClineTools.Modules.WhereUsed.WhereUsedModule());
            // -------------------------------------{ End }------------------------------------

            var initialized = new List<IModule>();

            try
            {
                foreach (var module in _modules)
                {
                    module.Initialize(_swApp);
                    initialized.Add(module);
                }

                _isLoaded = true;
                DebugTrace.Log($"ModManager.LoadModules: loaded {_modules.Count} modules.");
            }
            catch (Exception ex)
            {
                DebugTrace.DumpOnError(ex, "ModManager.LoadModules");

                // Roll back anything that successfully initialized
                for (int i = initialized.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        initialized[i].Terminate();
                    }
                    catch (Exception termEx)
                    {
                        DebugTrace.DumpOnError(termEx, "ModManager.LoadModules.RollbackTerminate");
                    }
                }

                _modules.Clear();
                _isLoaded = false;

                throw;
            }
        }

        /// <summary>
        /// Terminate all modules (reverse order). Safe to call multiple times.
        /// </summary>
        public void UnloadModules()
        {
            if (!_isLoaded && _modules.Count == 0)
                return;

            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                try
                {
                    _modules[i].Terminate();
                }
                catch (Exception ex)
                {
                    DebugTrace.DumpOnError(ex, $"ModManager.UnloadModules.Terminate[{_modules[i].GetType().Name}]");
                }
            }

            _modules.Clear();
            _isLoaded = false;

            DebugTrace.Log("ModManager.UnloadModules: modules unloaded.");
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