using System;
using System.IO;
using System.Text;

namespace MulliganMadness.Utils
{
    /// <summary>Session debug NDJSON writer (debug mode). Keep tiny; remove after verified.</summary>
    internal static class DebugAgentLog
    {
        private const string Path = "/var/home/bukey/MulliganMadness/.cursor/debug-762e72.log";
        private const string SessionId = "762e72";

        internal static void Write(string hypothesisId, string location, string message, string dataJson = null)
        {
            try
            {
                // Tick probes spam; keep those file-only. Everything else also hits BepInEx
                // so a friend's quit log is usable (the NDJSON file is host-workspace only).
                if (!string.Equals(message, "tick", StringComparison.Ordinal))
                {
                    Plugin.Instance?.Log($"MMDbg [{hypothesisId}] {location} {message} {dataJson}");
                }

                var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var sb = new StringBuilder(256);
                sb.Append("{\"sessionId\":\"").Append(SessionId).Append("\",");
                sb.Append("\"hypothesisId\":\"").Append(Esc(hypothesisId)).Append("\",");
                sb.Append("\"location\":\"").Append(Esc(location)).Append("\",");
                sb.Append("\"message\":\"").Append(Esc(message)).Append("\",");
                sb.Append("\"data\":").Append(string.IsNullOrEmpty(dataJson) ? "{}" : dataJson).Append(',');
                sb.Append("\"timestamp\":").Append(ts).Append('}');
                File.AppendAllText(Path, sb.ToString() + "\n");
            }
            catch
            {
                // never break pick flow
            }
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
        }
    }
}
