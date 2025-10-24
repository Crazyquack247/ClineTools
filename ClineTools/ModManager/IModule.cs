using SolidWorks.Interop.sldworks;

namespace ClineTools
{
    public interface IModule
    {
        // Initialize interface

        void Initialize(ISldWorks swApp);
        void Terminate();

    }
}
