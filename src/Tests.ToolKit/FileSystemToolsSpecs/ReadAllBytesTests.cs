namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public class ReadAllBytesTests : FileToolsTests
{
	private readonly byte[] fileBytes;

	public ReadAllBytesTests()
	{
		fileBytes = Faker.Create<byte[]>();

		A.CallTo(() => fileSystem.File.ReadAllBytesAsync(A<string>._, default)).Returns(fileBytes);
	}

	[Fact]
	public async Task CheckIfFileExists()
	{
		await fileTools.ReadAllBytes(filePath);

		VerifyFileExistWasCalled();
	}

	[Fact]
	public async Task IfTheFileDoesNotExistReturnAnEmptyArray()
	{
		SetFileDoesNotExist();

		var resultingBytes = await fileTools.ReadAllBytes(filePath);

		resultingBytes.Should().BeEmpty();
	}

	[Fact]
	public async Task ReadAllBytesAsync()
	{
		await fileTools.ReadAllBytes(filePath);

		A.CallTo(() => fileSystem.File.ReadAllBytesAsync(filePath, default)).MustHaveHappened();
	}

	[Fact]
	public async Task ReturnFileBytes()
	{
		var resultingBytes = await fileTools.ReadAllBytes(filePath);

		resultingBytes.Should().BeEquivalentTo(fileBytes);
	}
}
