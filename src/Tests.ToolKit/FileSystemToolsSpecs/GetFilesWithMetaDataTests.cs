using System.IO.Abstractions;
using FatCat.Toolkit.Testing;

namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public class GetFilesWithMetaDataTests : FileToolsTests
{
	private readonly List<string> fileList;
	private List<IFileInfo> fileInfos;

	public GetFilesWithMetaDataTests()
	{
		fileList = Faker.Create<List<string>>();

		A.CallTo(() => fileSystem.Directory.GetFiles(directoryPath)).Returns(fileList.ToArray());

		SetUpFileInfos();
	}

	[Fact]
	public void CreateNewFileInfoForeachFile()
	{
		fileTools.GetFilesWithMetaData(directoryPath);

		foreach (var file in fileList)
		{
			A.CallTo(() => fileSystem.FileInfo.New(file)).MustHaveHappened();
		}
	}

	[Fact]
	public void IfDirectoryDoesNotExistReturnEmptyList()
	{
		directoryExists = false;

		var files = fileTools.GetFiles(directoryPath);

		files.Should().BeEmpty();
	}

	[Fact]
	public void ReturnListOfFileInfo()
	{
		var files = fileTools.GetFilesWithMetaData(directoryPath);

		files.Should().BeEquivalentTo(fileInfos);
	}

	[Fact]
	public void VerifyDirectoryExists()
	{
		fileTools.GetFilesWithMetaData(directoryPath);

		VerifyDirectoryExistsWasCalled();
	}

	[Fact]
	public void WillGetFiles()
	{
		fileTools.GetFilesWithMetaData(directoryPath);

		A.CallTo(() => fileSystem.Directory.GetFiles(directoryPath)).MustHaveHappened();
	}

	private static IFileInfo CreateNewIFileInfo(int index, string file)
	{
		var fileInfo = A.Fake<IFileInfo>();

		A.CallTo(() => fileInfo.Length).Returns(100 + index);

		A.CallTo(() => fileInfo.FullName).Returns(file);

		A.CallTo(() => fileInfo.Name).Returns(Faker.RandomString());

		return fileInfo;
	}

	private void SetUpFileInfos()
	{
		fileInfos = new List<IFileInfo>();

		for (var index = 0; index < fileList.Count; index++)
		{
			var file = fileList[index];

			var fileInfo = CreateNewIFileInfo(index, file);

			fileInfos.Add(fileInfo);
		}

		A.CallTo(() => fileSystem.FileInfo.New(A<string>._)).ReturnsNextFromSequence(fileInfos);
	}
}
