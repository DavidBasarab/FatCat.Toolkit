#nullable enable
namespace FatCat.Toolkit.Data;

public interface IEnvironmentRepository
{
	public string? Get(string name);

	public string? Get(string name, EnvironmentVariableTarget target);

	public void Set(string name, string? value);

	public void Set(string name, string? value, EnvironmentVariableTarget target);
}

public class EnvironmentRepository : IEnvironmentRepository
{
	public string? Get(string name)
	{
		return Environment.GetEnvironmentVariable(name);
	}

	public string? Get(string name, EnvironmentVariableTarget target)
	{
		return Environment.GetEnvironmentVariable(name, target);
	}

	public void Set(string name, string? value)
	{
		Environment.SetEnvironmentVariable(name, value);
	}

	public void Set(string name, string? value, EnvironmentVariableTarget target)
	{
		Environment.SetEnvironmentVariable(name, value, target);
	}
}
