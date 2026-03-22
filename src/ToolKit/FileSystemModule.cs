using System.IO.Abstractions;
using Autofac;
using FatCat.Toolkit.Injection;
using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit;

public class FileSystemModule : Module, IToolkitModule
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<FileSystem>().As<IFileSystem>();
	}

	public void Register(IServiceCollection services)
	{
		services.AddScoped<IFileSystem, FileSystem>();
	}
}
