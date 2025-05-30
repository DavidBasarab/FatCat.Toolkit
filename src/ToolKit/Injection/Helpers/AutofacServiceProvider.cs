﻿using System.Diagnostics.CodeAnalysis;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit.Injection.Helpers;

/// <summary>
///  Autofac implementation of the ASP.NET Core <see cref="IServiceProvider" />.
/// </summary>
/// <seealso cref="System.IServiceProvider" />
/// <seealso cref="Microsoft.Extensions.DependencyInjection.ISupportRequiredService" />
public class AutofacServiceProvider : IServiceProvider, ISupportRequiredService, IDisposable, IAsyncDisposable
{
	private bool _disposed;

	/// <summary>
	///  Gets the underlying instance of <see cref="ILifetimeScope" />.
	/// </summary>
	public ILifetimeScope LifetimeScope { get; }

	/// <summary>
	///  Initializes a new instance of the <see cref="AutofacServiceProvider" /> class.
	/// </summary>
	/// <param name="lifetimeScope">
	///  The lifetime scope from which services will be resolved.
	/// </param>
	public AutofacServiceProvider(ILifetimeScope lifetimeScope)
	{
		LifetimeScope = lifetimeScope;
	}

	/// <summary>
	///  Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	///  Performs a dispose operation asynchronously.
	/// </summary>
	[SuppressMessage(
		"Usage",
		"CA1816:Dispose methods should call SuppressFinalize",
		Justification = "DisposeAsync should also call SuppressFinalize (see various .NET internal implementations)."
	)]
	public async ValueTask DisposeAsync()
	{
		if (!_disposed)
		{
			_disposed = true;
			await LifetimeScope.DisposeAsync();
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>
	///  Gets service of type <paramref name="serviceType" /> from the
	///  <see cref="AutofacServiceProvider" /> and requires it be present.
	/// </summary>
	/// <param name="serviceType">
	///  An object that specifies the type of service object to get.
	/// </param>
	/// <returns>
	///  A service object of type <paramref name="serviceType" />.
	/// </returns>
	/// <exception cref="Autofac.Core.Registration.ComponentNotRegisteredException">
	///  Thrown if the <paramref name="serviceType" /> isn't registered with the container.
	/// </exception>
	/// <exception cref="Autofac.Core.DependencyResolutionException">
	///  Thrown if the object can't be resolved from the container.
	/// </exception>
	public object GetRequiredService(Type serviceType)
	{
		return LifetimeScope.Resolve(serviceType);
	}

	/// <summary>
	///  Gets the service object of the specified type.
	/// </summary>
	/// <param name="serviceType">
	///  An object that specifies the type of service object to get.
	/// </param>
	/// <returns>
	///  A service object of type <paramref name="serviceType" />; or <see langword="null" />
	///  if there is no service object of type <paramref name="serviceType" />.
	/// </returns>
	public object GetService(Type serviceType)
	{
		return LifetimeScope.ResolveOptional(serviceType);
	}

	/// <summary>
	///  Releases unmanaged and - optionally - managed resources.
	/// </summary>
	/// <param name="disposing">
	///  <see langword="true" /> to release both managed and unmanaged resources;
	///  <see langword="false" /> to release only unmanaged resources.
	/// </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			_disposed = true;

			if (disposing)
			{
				LifetimeScope.Dispose();
			}
		}
	}
}
