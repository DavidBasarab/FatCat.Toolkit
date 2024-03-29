using System.IO.Abstractions;
using FatCat.Toolkit;

namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public abstract class FileToolsTests
{
	protected readonly string directoryPath;
	protected readonly string filePath;
	protected readonly IFileSystem fileSystem;
	protected readonly FileSystemTools fileTools;

	protected bool directoryExists = true;
	private bool fileExists = true;

	protected FileToolsTests()
	{
		fileSystem = A.Fake<IFileSystem>();

		fileTools = new FileSystemTools(fileSystem);

		directoryPath = $@"C:\SomePath\ToUse\{Faker.RandomString()}";

		filePath = Path.Join(directoryPath, $"{Faker.RandomString()}.txt");

		SetUpFileExists();
		SetUpDirectoryExists();
	}

	protected void SetFileDoesNotExist()
	{
		fileExists = false;
	}

	protected void VerifyDirectoryExistsWasCalled()
	{
		A.CallTo(() => fileSystem.Directory.Exists(directoryPath)).MustHaveHappened();
	}

	protected void VerifyFileExistWasCalled()
	{
		A.CallTo(() => fileSystem.File.Exists(filePath)).MustHaveHappened();
	}

	private void SetUpDirectoryExists()
	{
		A.CallTo(() => fileSystem.Directory.Exists(A<string>._)).ReturnsLazily(() => directoryExists);
	}

	private void SetUpFileExists()
	{
		A.CallTo(() => fileSystem.File.Exists(A<string>._)).ReturnsLazily(() => fileExists);
	}
}
