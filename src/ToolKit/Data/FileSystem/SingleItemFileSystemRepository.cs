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

public class SingleItemFileSystemRepository<T> : ISingleItemFileSystemRepository<T>
	where T : FileSystemDataObject, new()
{
	private readonly IApplicationTools applicationTools;
	private readonly IFileSystem fileSystem;
	private readonly IJsonOperations jsonOperations;

	public T? Data { get; set; }

	private string DataDirectory
	{
		get => Path.Join(applicationTools.ExecutingDirectory, "Data");
	}

	private bool DataDirectoryDoesNotExist
	{
		get => !fileSystem.Directory.Exists(DataDirectory);
	}

	private bool DataFileNotFound
	{
		get => !fileSystem.File.Exists(DataPath);
	}

	private string DataPath
	{
		get => Path.Join(DataDirectory, $"{typeof(T).Name}.data");
	}

	public SingleItemFileSystemRepository(
		IFileSystem fileSystem,
		IApplicationTools applicationTools,
		IJsonOperations jsonOperations
	)
	{
		this.fileSystem = fileSystem;
		this.applicationTools = applicationTools;
		this.jsonOperations = jsonOperations;
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
