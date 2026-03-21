namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public class MoveFileTests : FileToolsTests
{
	private const string DestinationFilePath = "destination-file-path";
	private const string SourceFilePath = "source-file-path";

	[Fact]
	public void CheckIfSourceFileExists()
	{
		fileTools.MoveFile(SourceFilePath, DestinationFilePath);

		A.CallTo(() => fileSystem.File.Exists(SourceFilePath)).MustHaveHappened();
	}

	[Fact]
	public void IfCanMoveFileReturnTrue()
	{
		fileTools.MoveFile(SourceFilePath, DestinationFilePath).Should().BeTrue();
	}

	[Fact]
	public void IfSourceFileDoesNotExistReturnFalse()
	{
		A.CallTo(() => fileSystem.File.Exists(A<string>._)).Returns(false);

		fileTools.MoveFile(SourceFilePath, DestinationFilePath).Should().BeFalse();
	}

	[Fact]
	public void MoveFile()
	{
		fileTools.MoveFile(SourceFilePath, DestinationFilePath);

		A.CallTo(() => fileSystem.File.Move(SourceFilePath, DestinationFilePath)).MustHaveHappened();
	}
}
