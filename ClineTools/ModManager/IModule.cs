using SolidWorks.Interop.sldworks;

namespace ClineTools
{
    /// <summary>
    /// Defines a minimal lifecycle for an add-in module.
    /// - Initialize: wire events, allocate resources, register callbacks
    /// - Terminate: unhook events and release resources created by Initialize
    /// </summary>
    public interface IModule
    {
        void Initialize(ISldWorks swApp);
        void Terminate();
    }
}