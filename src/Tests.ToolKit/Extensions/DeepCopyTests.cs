using FatCat.Toolkit;
using FatCat.Toolkit.Extensions;

namespace Tests.FatCat.Toolkit.Extensions;

public class DeepCopyTests
{
	[Fact]
	public void CanCopyWithNullObject()
	{
		var original = Faker.Create<ObjectToCopy>();

		original.SubObject = null;

		var copy = original.DeepCopy();

		copy.Should().BeEquivalentTo(original);

		copy.SubObject.Should().BeNull();
	}

	[Fact]
	public void CanDeepCopyAnObject()
	{
		var original = Faker.Create<ObjectToCopy>();

		var copy = original.DeepCopy();

		copy.Should().BeEquivalentTo(original);
	}

	[Fact]
	public void CanDeepCopyWithASubSubObject()
	{
		var original = Faker.Create<ObjectToCopy>();

		original.SubObject.SubSub = null;

		var copy = original.DeepCopy();

		copy.Should().BeEquivalentTo(original);
	}

	public class ObjectToCopy : EqualObject
	{
		public DateTime ADate { get; set; }

		public string AnotherString { get; set; }

		public string FirstName { get; set; }

		public List<SubObject> List { get; set; }

		public List<int> Numbers { get; set; }

		public int SomeNumber { get; set; }

		public SubObject SubObject { get; set; }
	}

	public class SubObject : EqualObject
	{
		public string Name { get; set; }

		public List<string> Names { get; set; }

		public int Number { get; set; }

		public SubSubObject SubSub { get; set; }
	}

	public class SubSubObject : EqualObject
	{
		public string Name { get; set; }
	}
}
