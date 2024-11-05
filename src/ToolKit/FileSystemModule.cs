using System.IO.Abstractions;
using Autofac;

namespace FatCat.Toolkit;

public class FileSystemModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<FileSystem>().As<IFileSystem>();
	}
}
