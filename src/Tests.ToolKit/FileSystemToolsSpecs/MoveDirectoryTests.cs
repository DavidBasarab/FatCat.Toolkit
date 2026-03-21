namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public class MoveDirectoryTests : FileToolsTests
{
	private readonly string destinationDirectoryPath;
	private readonly string sourceDirectoryPath;

	public MoveDirectoryTests()
	{
		sourceDirectoryPath = directoryPath;
		destinationDirectoryPath = Faker.RandomString();
	}

	[Fact]
	public void CheckIfSourceDirectoryExists()
	{
		fileTools.MoveDirectory(sourceDirectoryPath, destinationDirectoryPath);

		VerifyDirectoryExistsWasCalled();
	}

	[Fact]
	public void IfCanMoveDirectoryReturnTrue()
	{
		fileTools.MoveDirectory(sourceDirectoryPath, destinationDirectoryPath).Should().BeTrue();
	}

	[Fact]
	public void IfSourceDirectoryDoesNotExistReturnFalse()
	{
		directoryExists = false;

		fileTools.MoveDirectory(sourceDirectoryPath, destinationDirectoryPath).Should().BeFalse();
	}

	[Fact]
	public void MoveDirectory()
	{
		fileTools.MoveDirectory(sourceDirectoryPath, destinationDirectoryPath);

		A.CallTo(() => fileSystem.Directory.Move(sourceDirectoryPath, destinationDirectoryPath))
			.MustHaveHappened();
	}
}
