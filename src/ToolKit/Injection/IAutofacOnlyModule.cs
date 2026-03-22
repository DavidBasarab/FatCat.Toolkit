namespace FatCat.Toolkit.Injection;

/// <summary>
/// Marks a module as Autofac-only. SystemScope MS DI initialization skips these modules
/// and relies on convention scanning instead.
/// </summary>
public interface IAutofacOnlyModule { }
