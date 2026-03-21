namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public class DeleteDirectoryTests : FileToolsTests
{
	[Fact]
	public void CheckIfDirectoryExists()
	{
		fileTools.DeleteDirectory(directoryPath);

		VerifyDirectoryExistsWasCalled();
	}

	[Fact]
	public void DeleteDirectoryIfFound()
	{
		fileTools.DeleteDirectory(directoryPath, false);

		A.CallTo(() => fileSystem.Directory.Delete(directoryPath, false)).MustHaveHappened();
	}

	[Fact]
	public void DeleteTheDirectoryRecursivelyIfFound()
	{
		fileTools.DeleteDirectory(directoryPath);

		A.CallTo(() => fileSystem.Directory.Delete(directoryPath, true)).MustHaveHappened();
	}

	[Fact]
	public void IfDirectoryIsNotFoundItIsNotDeleted()
	{
		directoryExists = false;

		fileTools.DeleteDirectory(directoryPath);

		A.CallTo(() => fileSystem.Directory.Delete(A<string>._)).MustNotHaveHappened();
	}
}
