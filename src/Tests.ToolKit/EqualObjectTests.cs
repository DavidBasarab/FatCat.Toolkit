#nullable enable
using FatCat.Toolkit;

namespace Tests.FatCat.Toolkit;

public class EqualObjectTests
{
	[Fact]
	public void IfOneIsNullThenTheyAreNotEqual()
	{
		var (firstObject, secondObject) = GetObjects();

		secondObject = null;

		var result = firstObject == secondObject;

		result.Should().BeFalse();
	}

	[Fact]
	public void TwoObjectsAreNotTheSameIfAPropertyIsDifferent()
	{
		var (firstObject, secondObject) = GetObjects();

		secondObject.LastName = Faker.RandomString();

		var result = firstObject.Equals(secondObject);

		result.Should().BeFalse();
	}

	[Fact]
	public void TwoObjectsAreTheSameForEqualsOperator()
	{
		var (firstObject, secondObject) = GetObjects();

		var result = firstObject == secondObject;

		result.Should().BeTrue();
	}

	[Fact]
	public void TwoObjectsAreTheSameWithEqualsMethod()
	{
		var (firstObject, secondObject) = GetObjects();

		var result = firstObject.Equals(secondObject);

		result.Should().BeTrue();
	}

	private static (TestObject, TestObject) GetObjects()
	{
		var firstObject = Faker.Create<TestObject>();

		var secondObject = new TestObject
		{
			Length = firstObject.Length,
			CreatedDate = firstObject.CreatedDate,
			FirstName = firstObject.FirstName,
			LastName = firstObject.LastName,
			TheNumber = firstObject.TheNumber,
			On = firstObject.On,
			SomeId = firstObject.SomeId,
		};

		return (firstObject, secondObject);
	}

	private class TestObject : EqualObject
	{
		public DateTime CreatedDate { get; set; }

		public string FirstName { get; set; } = null!;

		public string LastName { get; set; } = null!;

		public TimeSpan Length { get; set; }

		public bool On { get; set; }

		public Guid SomeId { get; set; }

		public int TheNumber { get; set; }
	}
}
