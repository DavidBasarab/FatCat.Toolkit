using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit.Injection;

public interface IToolkitModule
{
	void Register(IServiceCollection services);
}
