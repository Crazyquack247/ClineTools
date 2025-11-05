// Modules/Stacker/Sin/ISinDecoder.cs
namespace ClineTools.Modules.Stacker.Sin
{
    public interface ISinDecoder
    {
        bool CanHandle(string normalizedSin);
        /// <summary>
        /// Returns a lightweight, type-specific anonymous object (will be JSON-serialized).
        /// Throw ArgumentException for invalid SINs the decoder claims it can handle.
        /// </summary>
        object DecodeToCard(string normalizedSin);
        /// <summary> Human-readable type name (e.g., "Insert", "InsertScrew"). </summary>
        string TypeName { get; }
    }
}