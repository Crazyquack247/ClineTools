using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ClineTools
{
    internal static class DebugTrace
    {
        private struct DebugEntry
        {
            public DateTime TimestampUtc;
            public string Message;
        }

        private static readonly object _sync = new object();
        private static readonly List<DebugEntry> _entries = new List<DebugEntry>();

        // Rolling in-memory window
        private static readonly TimeSpan _window = TimeSpan.FromMinutes(5);

        // Safety rail: prevents runaway memory use if logging gets spammy.
        private const int MaxEntries = 2000;

        // Toggle logging without recompiling (default ON).
        // You can disable via env var: CLINE_TOOLS_DEBUGTRACE=0
        public static bool Enabled { get; set; } = ReadEnabledFromEnvironment(defaultValue: true);

        /// <summary>
        /// Record a debug action into a rolling buffer window.
        /// </summary>
        public static void Log(string message)
        {
            if (!Enabled)
                return;

            if (string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                // Useful while debugging in VS Output window
                Debug.WriteLine("[ClineTools] " + message);
            }
            catch
            {
                // Never let diagnostics throw
            }

            lock (_sync)
            {
                _entries.Add(new DebugEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    Message = message
                });

                PruneExpired_NoLock();
                TrimToMax_NoLock();
            }
        }

        public static void LogCommand(
    string eventName,
    int commandId,
    string commandName,
    int activation)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                string line =
                    timestamp + "\t" +
                    eventName + "\t" +
                    commandId + "\t" +
                    commandName + "\t" +
                    activation;

                Log(line); // <-- reuse existing logging pipeline
            }
            catch
            {
                // Never allow logging to crash SolidWorks
            }
        }

        /// <summary>
        /// Dump the last window of actions plus exception info to a log file.
        /// Logging must never crash the add-in.
        /// </summary>
        public static string DumpOnError(Exception ex, string context = null)
        {
            return DumpInternal(ex, context, isSnapshotOnly: false);
        }

        /// <summary>
        /// Dump the last window of actions WITHOUT an exception.
        /// Useful to confirm logging is functioning.
        /// </summary>
        public static string DumpSnapshot(string context = null)
        {
            return DumpInternal(ex: null, context: context, isSnapshotOnly: true);
        }

        /// <summary>
        /// Returns the directory logs are written to (resolved).
        /// </summary>
        public static string GetResolvedLogDirectory()
        {
            return ResolveLogDirectory();
        }

        private static string DumpInternal(Exception ex, string context, bool isSnapshotOnly)
        {
            if (!Enabled)
                return null;

            try
            {
                string logDir = ResolveLogDirectory();
                Directory.CreateDirectory(logDir);

                string kind = isSnapshotOnly ? "Snapshot" : "Error";
                string fileName = $"ClineTools_{kind}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_UTC.txt";
                string fullPath = Path.Combine(logDir, fileName);

                List<DebugEntry> snapshot;
                lock (_sync)
                {
                    PruneExpired_NoLock();
                    TrimToMax_NoLock();
                    snapshot = _entries.OrderBy(e => e.TimestampUtc).ToList();
                }

                using (var writer = new StreamWriter(fullPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                {
                    writer.WriteLine("ClineTools Debug Log");
                    writer.WriteLine($"TimestampUtc : {DateTime.UtcNow:O}");
                    writer.WriteLine($"Machine      : {Environment.MachineName}");
                    writer.WriteLine($"User         : {Environment.UserName}");
                    writer.WriteLine($"Process      : {SafeGetProcessName()}");
                    writer.WriteLine($".NET         : {Environment.Version}");
                    writer.WriteLine($"OS           : {Environment.OSVersion}");

                    if (!string.IsNullOrWhiteSpace(context))
                        writer.WriteLine($"Context      : {context}");

                    writer.WriteLine();

                    if (ex != null)
                    {
                        writer.WriteLine("Exception:");
                        writer.WriteLine(ex);
                        writer.WriteLine();
                    }

                    writer.WriteLine($"Last {_window.TotalMinutes:0} minutes of actions (UTC):");
                    writer.WriteLine("--------------------------------------------------");

                    foreach (var entry in snapshot)
                        writer.WriteLine($"{entry.TimestampUtc:O} - {entry.Message}");
                }

                try { Debug.WriteLine("[ClineTools] DebugTrace wrote: " + fullPath); } catch { }

                return fullPath;
            }
            catch
            {
                // Never let logging crash the add-in
                return null;
            }
        }

        private static void PruneExpired_NoLock()
        {
            var cutoffUtc = DateTime.UtcNow - _window;
            _entries.RemoveAll(e => e.TimestampUtc < cutoffUtc);
        }

        private static void TrimToMax_NoLock()
        {
            if (_entries.Count <= MaxEntries)
                return;

            int removeCount = _entries.Count - MaxEntries;
            _entries.Sort((a, b) => a.TimestampUtc.CompareTo(b.TimestampUtc));
            _entries.RemoveRange(0, removeCount);
        }

        /// <summary>
        /// Prefer per-user LocalAppData (most reliable), fallback to ProgramData.
        /// </summary>
        private static string ResolveLogDirectory()
        {
            // Preferred: shared drive (your current intent)
            string preferred = @"F:\Engineer\_STANDARD LIBRARY\AUTOMATION TOOLS\Error Logs";
            try
            {
                Directory.CreateDirectory(preferred);
                return preferred;
            }
            catch
            {
                // If mapped drive isn't available (elevation/session/VPN), fall back
            }

            // Fallback: per-user local logs (always writable)
            try
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrWhiteSpace(local))
                    return Path.Combine(local, "ClineTools", "Logs");
            }
            catch { }

            // Last resort: temp
            return Path.Combine(Path.GetTempPath(), "ClineTools", "Logs");
        }

        private static bool ReadEnabledFromEnvironment(bool defaultValue)
        {
            try
            {
                string v = Environment.GetEnvironmentVariable("CLINE_TOOLS_DEBUGTRACE");
                if (string.IsNullOrWhiteSpace(v))
                    return defaultValue;

                v = v.Trim();

                if (v == "0" || v.Equals("false", StringComparison.OrdinalIgnoreCase) || v.Equals("off", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("on", StringComparison.OrdinalIgnoreCase))
                    return true;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private static string SafeGetProcessName()
        {
            try { return Process.GetCurrentProcess()?.ProcessName ?? "(unknown)"; }
            catch { return "(unknown)"; }
        }
    }
}