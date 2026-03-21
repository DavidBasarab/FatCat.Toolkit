namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public class AppendToFileTests : TestsToEnsureFileExists
{
	private readonly string textToAppend;

	public AppendToFileTests()
	{
		textToAppend = Faker.RandomString();
	}

	[Fact]
	public async Task UseFileSystemToAppendText()
	{
		await fileTools.AppendToFile(filePath, textToAppend);

		A.CallTo(() => fileSystem.File.AppendAllTextAsync(filePath, textToAppend, default)).MustHaveHappened();
	}

	protected override async Task RunMethodToTest()
	{
		await fileTools.AppendToFile(filePath, textToAppend);
	}
}
