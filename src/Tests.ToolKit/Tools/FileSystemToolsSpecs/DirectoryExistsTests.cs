using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace Tests.FatCat.Toolkit.Tools.FileSystemToolsSpecs;

public class DirectoryExistsTests : FileToolTests
{
	[Fact]
	public void CheckIfDirectoryExistsInFileSystem()
	{
		fileTools.DirectoryExists(directoryPath);

		VerifyDirectoryExistsWasCalled();
	}

	

	[Fact]
	public void FalseIfDirectoryIsNotFound()
	{
		directoryExists = false;

		fileTools.DirectoryExists(directoryPath)
			.Should()
			.BeFalse();
	}

	[Fact]
	public void TrueIfDirectoryIsFound()
	{
		fileTools.DirectoryExists(directoryPath)
			.Should()
			.BeTrue();
	}
}