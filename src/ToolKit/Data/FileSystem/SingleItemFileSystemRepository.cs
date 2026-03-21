#nullable enable
using System.IO.Abstractions;
using FatCat.Toolkit.Json;

namespace FatCat.Toolkit.Data.FileSystem;

public interface ISingleItemFileSystemRepository<T>
	where T : FileSystemDataObject, new()
{
	public bool Exists();

	public Task<T> Get();

	public Task Save(T item);
}

public class SingleItemFileSystemRepository<T>(
	IFileSystem fileSystem,
	IApplicationTools applicationTools,
	IJsonOperations jsonOperations
) : ISingleItemFileSystemRepository<T>
	where T : FileSystemDataObject, new()
{
	public T? Data { get; set; }

	private string DataDirectory
	{
		get
		{
			return Path.Join(applicationTools.ExecutingDirectory, "Data");
		}
	}

	private bool DataDirectoryDoesNotExist
	{
		get
		{
			return !fileSystem.Directory.Exists(DataDirectory);
		}
	}

	private bool DataFileNotFound
	{
		get
		{
			return !fileSystem.File.Exists(DataPath);
		}
	}

	private string DataPath
	{
		get
		{
			return Path.Join(DataDirectory, $"{typeof(T).Name}.data");
		}
	}

	public bool Exists()
	{
		return fileSystem.File.Exists(DataPath);
	}

	public async Task<T> Get()
	{
		if (Data != null)
		{
			return Data;
		}

		if (DataDirectoryDoesNotExist || DataFileNotFound)
		{
			return new T();
		}

		var json = await fileSystem.File.ReadAllTextAsync(DataPath);

		Data = jsonOperations.Deserialize<T>(json);

		return Data!;
	}

	public async Task Save(T item)
	{
		Data = item;

		var json = jsonOperations.Serialize(Data);

		if (DataDirectoryDoesNotExist)
		{
			fileSystem.Directory.CreateDirectory(DataDirectory);
		}

		await fileSystem.File.WriteAllTextAsync(DataPath, json);
	}
}
