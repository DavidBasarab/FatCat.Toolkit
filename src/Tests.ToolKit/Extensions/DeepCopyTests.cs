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

	[Fact]
	public void CanDeepCopyObjectWithAbstractProperty()
	{
		var concreteInput = new ConcreteInput { Name = "HDMI-1", Port = 42 };

		var original = new ObjectWithAbstractProperty
		{
			Label = "Test Device",
			Input = concreteInput,
		};

		var copy = original.DeepCopy();

		copy.Should().BeEquivalentTo(original);
		copy.Input.Should().BeOfType<ConcreteInput>();
		copy.Input.Should().NotBeSameAs(original.Input);
	}

	[Fact]
	public void CanDeepCopyListOfAbstractTypes()
	{
		var original = new ObjectWithAbstractList
		{
			Inputs = new List<AbstractInput>
			{
				new ConcreteInput { Name = "HDMI-1", Port = 1 },
				new AnotherConcreteInput { Name = "SDI-1", Channel = 7 },
			},
		};

		var copy = original.DeepCopy();

		copy.Should().BeEquivalentTo(original);
		copy.Inputs[0].Should().BeOfType<ConcreteInput>();
		copy.Inputs[1].Should().BeOfType<AnotherConcreteInput>();
		copy.Inputs.Should().NotBeSameAs(original.Inputs);
	}

	[Fact]
	public void ModifyingCopyDoesNotAffectOriginalSubObject()
	{
		var original = Faker.Create<ObjectToCopy>();
		var originalName = original.SubObject.Name;

		var copy = original.DeepCopy();

		copy.SubObject.Name = "CHANGED";

		original.SubObject.Name.Should().Be(originalName);
	}

	[Fact]
	public void ModifyingCopyDoesNotAffectOriginalList()
	{
		var original = Faker.Create<ObjectToCopy>();
		var originalCount = original.List.Count;

		var copy = original.DeepCopy();

		copy.List.Add(new SubObject { Name = "New Item", Number = 999 });

		original.List.Count.Should().Be(originalCount);
	}

	[Fact]
	public void ModifyingCopyListElementDoesNotAffectOriginal()
	{
		var original = Faker.Create<ObjectToCopy>();
		var originalFirstItemName = original.List[0].Name;

		var copy = original.DeepCopy();

		copy.List[0].Name = "CHANGED";

		original.List[0].Name.Should().Be(originalFirstItemName);
	}

	[Fact]
	public void CopiedObjectReferencesAreNotSameAsOriginal()
	{
		var original = Faker.Create<ObjectToCopy>();

		var copy = original.DeepCopy();

		copy.Should().NotBeSameAs(original);
		copy.SubObject.Should().NotBeSameAs(original.SubObject);
		copy.List.Should().NotBeSameAs(original.List);
		copy.Numbers.Should().NotBeSameAs(original.Numbers);
	}

	[Fact]
	public void ModifyingCopyAbstractListElementDoesNotAffectOriginal()
	{
		var original = new ObjectWithAbstractList
		{
			Inputs = new List<AbstractInput>
			{
				new ConcreteInput { Name = "HDMI-1", Port = 1 },
				new AnotherConcreteInput { Name = "SDI-1", Channel = 7 },
			},
		};

		var copy = original.DeepCopy();

		copy.Inputs[0].Name = "CHANGED";
		((AnotherConcreteInput)copy.Inputs[1]).Channel = 999;

		original.Inputs[0].Name.Should().Be("HDMI-1");
		((AnotherConcreteInput)original.Inputs[1]).Channel.Should().Be(7);
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

	public abstract class AbstractInput
	{
		public string Name { get; set; }
	}

	public class ConcreteInput : AbstractInput
	{
		public int Port { get; set; }
	}

	public class AnotherConcreteInput : AbstractInput
	{
		public int Channel { get; set; }
	}

	public class ObjectWithAbstractProperty
	{
		public string Label { get; set; }

		public AbstractInput Input { get; set; }
	}

	public class ObjectWithAbstractList
	{
		public List<AbstractInput> Inputs { get; set; }
	}
}
