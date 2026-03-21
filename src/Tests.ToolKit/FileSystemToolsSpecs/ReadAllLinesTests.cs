namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public class ReadAllLinesTests : FileToolsTests
{
	private readonly List<string> fileLines;

	public ReadAllLinesTests()
	{
		fileLines = Faker.Create<List<string>>();

		A.CallTo(() => fileSystem.File.ReadAllLinesAsync(A<string>._, default)).Returns(fileLines.ToArray());
	}

	[Fact]
	public async Task CheckIfFileExists()
	{
		await fileTools.ReadAllLines(filePath);

		VerifyFileExistWasCalled();
	}

	[Fact]
	public async Task IfFileDoesNotExistReturnEmptyList()
	{
		SetFileDoesNotExist();

		var resultList = await fileTools.ReadAllLines(filePath);

		resultList.Should().BeEmpty();
	}

	[Fact]
	public async Task ReadAllFileLines()
	{
		await fileTools.ReadAllLines(filePath);

		A.CallTo(() => fileSystem.File.ReadAllLinesAsync(filePath, default)).MustHaveHappened();
	}

	[Fact]
	public async Task ReturnListOfLines()
	{
		var resultList = await fileTools.ReadAllLines(filePath);

		resultList.Should().BeEquivalentTo(fileLines);
	}
}