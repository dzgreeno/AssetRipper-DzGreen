using AssetRipper.Import.Logging;
using System.Collections.Concurrent;

namespace AssetRipper.GUI.Web;

internal static class StatusLog
{
	private const int MaximumLines = 120;
	private static readonly ConcurrentQueue<string> lines = new();
	private static readonly ConcurrentQueue<string> completeLines = new();
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

	public static string GetCompleteText()
	{
		Initialize();
		return string.Join(Environment.NewLine, completeLines) + Environment.NewLine;
	}

	private static void AddLine(string line)
	{
		lines.Enqueue(line);
		completeLines.Enqueue(line);
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
			string[] messageLines = (message ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			AddLine($"[{prefix}] {messageLines[0]}");
			foreach (string messageLine in messageLines.AsSpan(1))
			{
				AddLine(messageLine);
			}
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
