using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ClineTools.Modules.Release
{
    internal static class PathSafety
    {
        // Example expected: F:\Edgecam\Engineering Transfer\<Company>\<CT-123>\
        // This is a conservative check: we refuse if it's not clearly inside EdgecamTransferRoot.
        public static bool TryValidateEngineeringTransferFolder(string folderPath, out string reason)
        {
            reason = string.Empty;

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                reason = "Export folder path is empty.";
                return false;
            }

            string full;
            try
            {
                full = Normalize(folderPath);
            }
            catch (Exception ex)
            {
                reason = $"Export folder path could not be resolved: {ex.Message}";
                return false;
            }

            // Reject drive roots like "F:\" or "C:\"
            if (IsDriveRoot(full))
            {
                reason = $"Export folder resolves to a drive root ({full}). Refusing for safety.";
                return false;
            }

            // Must be under EdgecamTransferRoot
            string allowedRoot = Normalize(ReleaseConfig.EdgecamTransferRoot);
            if (!IsUnderRoot(full, allowedRoot))
            {
                reason =
                    "Export folder is not under the allowed Engineering Transfer root.\n\n" +
                    $"Allowed root:\n{allowedRoot}\n\n" +
                    $"Chosen folder:\n{full}\n\n" +
                    "Refusing for safety.";
                return false;
            }

            // Must include at least: root + company + part folder
            // (i.e. at least 2 segments under the root)
            var rel = full.Substring(allowedRoot.Length).Trim('\\', '/');
            var parts = rel.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                reason =
                    "Export folder is too shallow under the Engineering Transfer root.\n\n" +
                    "Expected:\n<Root>\\<Company>\\<PartNumber>\\\n\n" +
                    $"Got:\n{full}";
                return false;
            }

            // Optional: enforce that part folder looks like CT-### or CT-#### etc.
            // If you have other part formats, we can loosen this later.
            string partFolder = parts[1];
            if (!LooksLikePartFolder(partFolder))
            {
                reason =
                    "Export folder does not look like a part-number folder.\n\n" +
                    "This guard prevents accidental exports into a broad company folder.\n\n" +
                    $"Folder:\n{full}\n\n" +
                    $"Part segment detected:\n{partFolder}";
                return false;
            }

            return true;
        }

        public static void EnsureSafeOrThrow(string folderPath)
        {
            if (!TryValidateEngineeringTransferFolder(folderPath, out string reason))
                throw new InvalidOperationException(reason);
        }

        public static bool IsUnderRoot(string path, string root)
        {
            path = Normalize(path);
            root = Normalize(root);

            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string p)
        {
            // Ensure consistent trailing backslash for roots
            string full = Path.GetFullPath(p.Trim());
            if (!full.EndsWith("\\"))
                full += "\\";
            return full;
        }

        private static bool IsDriveRoot(string fullPathWithSlash)
        {
            // "F:\" length = 3. We also treat "\\server\share\" as root-like if user ever uses UNC.
            string trimmed = fullPathWithSlash.TrimEnd('\\');

            // Drive root "X:"
            if (trimmed.Length == 2 && trimmed[1] == ':')
                return true;

            // Drive root "X:\"
            if (trimmed.Length == 3 && trimmed[1] == ':' && trimmed[2] == '\\')
                return true;

            // UNC root patterns are trickier; we’re conservative:
            // if it looks like "\\server\share" (2 segments) we consider it too shallow.
            if (trimmed.StartsWith("\\\\"))
            {
                var segs = trimmed.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length <= 2)
                    return true;
            }

            return false;
        }

        private static bool LooksLikePartFolder(string partFolder)
        {
            if (string.IsNullOrWhiteSpace(partFolder))
                return false;

            // Adjust this pattern if needed.
            // Accept: CT-1, CT-12, CT-123, CT-1234, CT-12345...
            return Regex.IsMatch(partFolder.Trim(), @"^CT-\d+$", RegexOptions.IgnoreCase);
        }
    }
}