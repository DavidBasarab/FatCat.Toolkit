namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public class GetDirectoriesTests : FileToolsTests
{
	private List<string> directories;

	public GetDirectoriesTests()
	{
		SetUpGetDirectories();
	}

	[Fact]
	public void GetDirectoriesFromFileSystem()
	{
		fileTools.GetDirectories(directoryPath);

		A.CallTo(() => fileSystem.Directory.GetDirectories(directoryPath)).MustHaveHappened();
	}

	[Fact]
	public void IfDirectoryDoesNotExistReturnEmptyList()
	{
		directoryExists = false;

		fileTools.GetDirectories(directoryPath).Should().BeEmpty();
	}

	[Fact]
	public void ReturnDirectories()
	{
		fileTools.GetDirectories(directoryPath).Should().BeEquivalentTo(directories);
	}

	[Fact]
	public void VerifyDirectoryExists()
	{
		fileTools.GetDirectories(directoryPath);

		VerifyDirectoryExistsWasCalled();
	}

	private void SetUpGetDirectories()
	{
		directories = Faker.Create<List<string>>();

		A.CallTo(() => fileSystem.Directory.GetDirectories(A<string>._)).Returns(directories.ToArray());
	}
}
