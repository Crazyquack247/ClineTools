using SolidWorks.Interop.sldworks;

namespace INJECTOR
{
    public interface IModule
    {
        // Initialize interface

        void Initialize(ISldWorks swApp);
        void Terminate();

    }
}
