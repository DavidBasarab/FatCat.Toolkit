using System.IO.Abstractions;
using FatCat.Toolkit;
using FatCat.Toolkit.Data.FileSystem;
using FatCat.Toolkit.Json;

namespace Tests.FatCat.Toolkit.Data.FIleSystem.FileSystemRepositorySpecs;

public abstract class FileSystemRepositoryTests
{
	protected const string ExecutingDirectory = @"C:\Some\Install\Directory";

	protected static string DataDirectory
	{
		get
		{
			return Path.Join(ExecutingDirectory, "Data");
		}
	}

	protected static string TestFileDataObjectPath
	{
		get
		{
			return Path.Join(DataDirectory, "TestFileDataObject.data");
		}
	}

	protected readonly SingleItemFileSystemRepository<TestFileDataObject> repository;
	protected IApplicationTools applicationTools = null!;
	protected string dataJson = null!;
	protected IFileSystem fileSystem = null!;
	protected IJsonOperations jsonHelper = null!;
	protected TestFileDataObject testObject = null!;

	protected FileSystemRepositoryTests()
	{
		SetUpFileSystem();
		SetUpApplicationTools();
		SetUpJsonHelper();

		repository = new SingleItemFileSystemRepository<TestFileDataObject>(
			fileSystem,
			applicationTools,
			jsonHelper
		);
	}

	private void SetUpApplicationTools()
	{
		applicationTools = A.Fake<IApplicationTools>();

		A.CallTo(() => applicationTools.ExecutingDirectory).Returns(ExecutingDirectory);
	}

	private void SetUpFileSystem()
	{
		fileSystem = A.Fake<IFileSystem>();

		dataJson = Faker.RandomString();

		A.CallTo(() => fileSystem.File.ReadAllTextAsync(A<string>._, default)).Returns(dataJson);
	}

	private void SetUpJsonHelper()
	{
		jsonHelper = A.Fake<IJsonOperations>();

		testObject = Faker.Create<TestFileDataObject>();

		A.CallTo(() => jsonHelper.Deserialize<TestFileDataObject>(A<string>._)).Returns(testObject);
	}
}
