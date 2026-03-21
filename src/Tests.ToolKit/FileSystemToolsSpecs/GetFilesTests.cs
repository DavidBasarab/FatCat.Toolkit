namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public class GetFilesTests : FileToolsTests
{
	private readonly List<string> fileList;

	public GetFilesTests()
	{
		fileList = Faker.Create<List<string>>();

		A.CallTo(() => fileSystem.Directory.GetFiles(directoryPath)).Returns(fileList.ToArray());
	}

	[Fact]
	public void IfDirectoryDoesNotExistReturnEmptyList()
	{
		directoryExists = false;

		var files = fileTools.GetFiles(directoryPath);

		files.Should().BeEmpty();
	}

	[Fact]
	public void ReturnFilesFromFileSystem()
	{
		var files = fileTools.GetFiles(directoryPath);

		files.Should().BeEquivalentTo(fileList);
	}

	[Fact]
	public void VerifyDirectoryExists()
	{
		fileTools.GetFiles(directoryPath);

		VerifyDirectoryExistsWasCalled();
	}

	[Fact]
	public void WillGetFiles()
	{
		fileTools.GetFiles(directoryPath);

		A.CallTo(() => fileSystem.Directory.GetFiles(directoryPath)).MustHaveHappened();
	}
}
