using FatCat.Toolkit.Data;

namespace Tests.FatCat.Toolkit.Data;

public class EmbeddedResourceRepositoryTests
{
	private const string ValidResourceName = "Tests.FatCat.Toolkit.Data.ResourceItemToGet.txt";
	private readonly EmbeddedResourceRepository repository;

	public EmbeddedResourceRepositoryTests()
	{
		repository = new EmbeddedResourceRepository();
	}

	[Fact]
	public void CanGetAManifestStream()
	{
		var stream = repository.GetStream(GetType().Assembly, ValidResourceName);

		stream.Should().NotBeNull();

		var streamReader = new StreamReader(stream);

		var text = streamReader.ReadToEnd();

		text.Should().Be("Dreams find fulfillment in time");
	}

	[Fact]
	public void CanGetEmbeddedResourceText()
	{
		var text = repository.GetText(GetType().Assembly, ValidResourceName);

		text.Should().Be("Dreams find fulfillment in time");
	}

	[Fact]
	public void IfResourceIsNotFoundReturnNull()
	{
		var resourceName = "Tests.FatCat.Toolkit.Data.ResourceItemToGetThatDoesNotExist.txt";

		var text = repository.GetText(GetType().Assembly, resourceName);

		text.Should().BeNullOrEmpty();
	}

	[Fact]
	public void IfResourceIsNotFoundStreamIsNull()
	{
		var resourceName = "Tests.FatCat.Toolkit.Data.ResourceItemToGetThatDoesNotExist.txt";

		var stream = repository.GetStream(GetType().Assembly, resourceName);

		stream.Should().BeNull();
	}

	[Fact]
	public void ReturnAllResourceNames()
	{
		var resources = repository.GetAllResourceNames(GetType().Assembly);

		resources.Count.Should().BeGreaterThanOrEqualTo(1);

		resources.Should().Contain(ValidResourceName);
	}
}
