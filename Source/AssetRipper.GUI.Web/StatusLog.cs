using AssetRipper.Import.Logging;
using System.Collections.Concurrent;

namespace AssetRipper.GUI.Web;

internal static class StatusLog
{
	private const int MaximumLines = 120;
	private static readonly ConcurrentQueue<string> lines = new();
	private static int initialized;

	public static void Initialize()
	{
		if (Interlocked.Exchange(ref initialized, 1) == 0)
		{
			Logger.Add(new StatusLogger());
		}
	}

	public static string[] Snapshot()
	{
		Initialize();
		return lines.ToArray();
	}

	private static void AddLine(string line)
	{
		lines.Enqueue(line);
		while (lines.Count > MaximumLines && lines.TryDequeue(out _))
		{
		}
	}

	private sealed class StatusLogger : ILogger
	{
		public void Log(LogType type, LogCategory category, string message)
		{
			string prefix = type switch
			{
				LogType.Error => "Error",
				LogType.Warning => "Warning",
				LogType.Verbose => "Verbose",
				LogType.Debug => "Debug",
				_ => category switch
				{
					LogCategory.Export or LogCategory.ExportProgress => "Export",
					LogCategory.Import => "Import",
					_ => "Status",
				},
			};
			string normalized = message.Trim().Replace("\r\n", " ").Replace('\n', ' ');
			if (normalized.Length > 500)
			{
				normalized = normalized[..500] + "…";
			}
			AddLine($"[{prefix}] {normalized}");
		}

		public void BlankLine(int numLines)
		{
			if (numLines > 0)
			{
				AddLine(string.Empty);
			}
		}
	}
}
