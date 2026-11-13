using System.Diagnostics.CodeAnalysis;
using Autofac;
using FatCat.Toolkit.Caching;

namespace OneOffToolkitOnly;

[ExcludeFromCodeCoverage(Justification = "Autofac registration of one options instance for a hand-run sample; no logic")]
public class CacheCapSampleModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterInstance(new FatCatCacheOptions<CappedSampleItem> { MaximumItems = 1_000 });
	}
}
