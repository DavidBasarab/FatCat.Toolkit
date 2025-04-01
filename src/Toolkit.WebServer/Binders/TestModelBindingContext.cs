using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FatCat.Toolkit.WebServer.Binders;

public class TestModelBindingContext(TestValueProvider testValueProvider) : ModelBindingContext
{
	public override ActionContext ActionContext { get; set; }

	public override string BinderModelName { get; set; }

	public override BindingSource BindingSource { get; set; }

	public override string FieldName { get; set; }

	public override bool IsTopLevelObject { get; set; }

	public override object Model { get; set; }

	public override ModelMetadata ModelMetadata { get; set; }

	public override string ModelName { get; set; }

	public override ModelStateDictionary ModelState { get; set; }

	public override Func<ModelMetadata, bool> PropertyFilter { get; set; }

	public override ModelBindingResult Result { get; set; }

	public TestValueProvider TestValueProvider { get; set; } = testValueProvider;

	public override ValidationStateDictionary ValidationState { get; set; }

	public override IValueProvider ValueProvider
	{
		get => TestValueProvider;
		set { }
	}

	public TestModelBindingContext()
		: this(new TestValueProvider()) { }

	public void AddTestValues(string name, string value)
	{
		TestValueProvider.AddTestValues(name, value);
	}

	public override NestedScope EnterNestedScope(
		ModelMetadata modelMetadata,
		string fieldName,
		string modelName,
		object model
	)
	{
		throw new NotImplementedException();
	}

	public override NestedScope EnterNestedScope()
	{
		throw new NotImplementedException();
	}

	public void RemoveTestValues(string name)
	{
		TestValueProvider.RemoveTestValues(name);
	}

	protected override void ExitNestedScope()
	{
		throw new NotImplementedException();
	}
}
