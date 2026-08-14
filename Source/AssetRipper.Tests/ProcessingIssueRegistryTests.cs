using AssetRipper.Processing;

namespace AssetRipper.Tests;

public sealed class ProcessingIssueRegistryTests
{
	[SetUp]
	public void SetUp()
	{
		ProcessingIssueRegistry.Clear();
		ProcessingIssueRegistry.Strict = false;
	}

	[TearDown]
	public void TearDown()
	{
		ProcessingIssueRegistry.Clear();
		ProcessingIssueRegistry.Strict = false;
	}

	[Test]
	public void BestEffortRecordsIssueAndDoesNotThrow()
	{
		Exception cause = new FormatException("optional asset is malformed");

		ProcessingIssueRegistry.Record("EditorFormatConversion", "BrokenClip", "AnimationClip", 42, "main", cause);
		ProcessingIssueRegistry.ThrowIfStrict("conversion failed", cause);

		IReadOnlyList<ProcessingIssue> issues = ProcessingIssueRegistry.Snapshot();
		Assert.That(issues, Has.Count.EqualTo(1));
		ProcessingIssue issue = issues[0];
		Assert.That(issue.Stage, Is.EqualTo("EditorFormatConversion"));
		Assert.That(issue.AssetName, Is.EqualTo("BrokenClip"));
		Assert.That(issue.ClassName, Is.EqualTo("AnimationClip"));
		Assert.That(issue.PathId, Is.EqualTo(42));
		Assert.That(issue.ExceptionType, Is.EqualTo(nameof(FormatException)));
		Assert.That(issue.Message, Is.EqualTo(cause.Message));
	}

	[Test]
	public void StrictModeThrowsWithOriginalException()
	{
		ProcessingIssueRegistry.Strict = true;
		Exception cause = new InvalidDataException("invalid serialized data");

		InvalidOperationException? thrown = Assert.Throws<InvalidOperationException>(() => ProcessingIssueRegistry.ThrowIfStrict("strict conversion failed", cause));

		Assert.That(thrown, Is.Not.Null);
		Assert.That(thrown!.Message, Does.Contain("strict conversion failed"));
		Assert.That(thrown.InnerException, Is.SameAs(cause));
	}
}
