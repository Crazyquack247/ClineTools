using System;
using System.IO;

namespace ClineTools.Modules.Release
{
    internal static class ReleaseConfig
    {
        // Root where your engineering design files live
        // Example:
        // F:\Engineer\AUTOCAD\DRAWING FILES\CASE\CT-123\REV -\
        public static readonly string EngineerDrawingRoot =
            @"F:\Engineer\AUTOCAD\DRAWING FILES";

        // Root where Engineering Transfer folders live
        // Example:
        // F:\Edgecam\Engineering Transfer\CASE\CT-123\
        public static readonly string EdgecamTransferRoot =
            @"F:\Edgecam\Engineering Transfer";

        // Normalized versions (full path, no trailing slashes)
        public static string EngineerDrawingRootNormalized => NormalizeRoot(EngineerDrawingRoot);
        public static string EdgecamTransferRootNormalized => NormalizeRoot(EdgecamTransferRoot);

        public static bool RootsExist()
        {
            return Directory.Exists(EngineerDrawingRootNormalized)
                && Directory.Exists(EdgecamTransferRootNormalized);
        }

        private static string NormalizeRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            try
            {
                return Path.GetFullPath(path.Trim().TrimEnd('\\', '/'));
            }
            catch
            {
                return path.Trim().TrimEnd('\\', '/');
            }
        }
    }
}
