using AssetRipper.Assets;
using AssetRipper.SourceGenerated.Extensions;
using System.Collections.Concurrent;

namespace AssetRipper.Processing;

/// <summary>
/// Collects recoverable processing failures without hiding them from callers.
/// The default mode is best-effort: one malformed optional asset is recorded and
/// skipped while the rest of the import can continue. Strict mode rethrows.
/// </summary>
public static class ProcessingIssueRegistry
{
	private static readonly ConcurrentQueue<ProcessingIssue> issues = new();

	public static bool Strict { get; set; }

	public static IReadOnlyList<ProcessingIssue> Snapshot() => issues.ToArray();

	public static void Clear()
	{
		while (issues.TryDequeue(out _))
		{
		}
	}

	public static void Record(IUnityObjectBase asset, string stage, Exception exception)
	{
		Record(
			stage,
			asset.GetBestName(),
			asset.ClassName,
			asset.PathID,
			asset.Collection.Name,
			exception);
	}

	public static void Record(string stage, string assetName, string className, long pathId, string collection, Exception exception)
	{
		issues.Enqueue(new ProcessingIssue(
			stage,
			assetName,
			className,
			pathId,
			collection,
			exception.GetType().Name,
			exception.Message));
	}

	public static void ThrowIfStrict(string message, Exception exception)
	{
		if (Strict)
		{
			throw new InvalidOperationException(message, exception);
		}
	}
}

public sealed record ProcessingIssue(
	string Stage,
	string AssetName,
	string ClassName,
	long PathId,
	string Collection,
	string ExceptionType,
	string Message);
