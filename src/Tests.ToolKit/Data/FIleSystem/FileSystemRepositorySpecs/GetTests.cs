namespace Tests.FatCat.Toolkit.Data.FIleSystem.FileSystemRepositorySpecs;

public class GetTests : FileSystemRepositoryTests
{
	public GetTests()
	{
		SetUpFileExists();
	}

	[Fact]
	public async Task CheckIfDataDirectoryExists()
	{
		await repository.Get();

		A.CallTo(() => fileSystem.Directory.Exists(DataDirectory)).MustHaveHappened();
	}

	[Fact]
	public async Task CheckIfFileExists()
	{
		await repository.Get();

		A.CallTo(() => fileSystem.File.Exists(TestFileDataObjectPath)).MustHaveHappened();
	}

	[Fact]
	public async Task ConvertJsonToTestFileDataObject()
	{
		await repository.Get();

		A.CallTo(() => jsonHelper.Deserialize<TestFileDataObject>(dataJson)).MustHaveHappened();
	}

	[Fact]
	public async Task GetExecutingDirectory()
	{
		await repository.Get();

		A.CallTo(() => applicationTools.ExecutingDirectory).MustHaveHappened();
	}

	[Fact]
	public async Task GetFileText()
	{
		await repository.Get();

		A.CallTo(() => fileSystem.File.ReadAllTextAsync(TestFileDataObjectPath, default)).MustHaveHappened();
	}

	[Fact]
	public async Task IfDataDirectoryDoesNotExistReturnNewObject()
	{
		A.CallTo(() => fileSystem.Directory.Exists(DataDirectory)).Returns(false);

		await RunDataFileNotFoundTest();
	}

	[Fact]
	public async Task IfFileDoesNotExistReturnNewObject()
	{
		A.CallTo(() => fileSystem.File.Exists(A<string>._)).Returns(false);

		await RunDataFileNotFoundTest();
	}

	[Fact]
	public async Task IfTestFileDataObjectIsNotNullReturnTestFileDataObject()
	{
		var prePopulatedData = Faker.Create<TestFileDataObject>();

		repository.Data = prePopulatedData;

		var result = await repository.Get();

		result.Should().Be(prePopulatedData);

		VerifyNoCallsToFileSystemMade();
	}

	[Fact]
	public async Task ReturnTestFileDataObjectObject()
	{
		var result = await repository.Get();

		result.Should().Be(testObject);
	}

	[Fact]
	public async Task SaveDataObjectOnRepository()
	{
		await repository.Get();

		repository.Data.Should().Not.BeNull();

		repository.Data.Should().Be(testObject);
	}

	private async Task RunDataFileNotFoundTest()
	{
		var result = await repository.Get();

		result.Should().Be(new TestFileDataObject());

		VerifyNoCallsToFileSystemMade();
	}

	private void SetUpFileExists()
	{
		A.CallTo(() => fileSystem.Directory.Exists(A<string>._)).Returns(true);

		A.CallTo(() => fileSystem.File.Exists(A<string>._)).Returns(true);
	}

	private void VerifyNoCallsToFileSystemMade()
	{
		A.CallTo(() => jsonHelper.Deserialize<TestFileDataObject>(A<string>._)).MustNotHaveHappened();

		A.CallTo(() => fileSystem.File.ReadAllTextAsync(A<string>._, default)).MustNotHaveHappened();
	}
}
