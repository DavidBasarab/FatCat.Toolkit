using System.IO.Abstractions;

namespace FatCat.Toolkit;

public interface IFileSystemTools
{
	Task AppendToFile(string path, string text);

	void AppendToFileSync(string path, string text);

	void DeleteDirectory(string path, bool recursive = true);

	bool DeleteFile(string path);

	bool DirectoryExists(string path);

	void EnsureDirectory(string path);

	void EnsureFile(string path);

	bool FileExists(string path);

	List<string> GetDirectories(string path);

	IFileInfo GetFileMetaData(string fullPath);

	List<string> GetFiles(string directoryPath);

	List<IFileInfo> GetFilesWithMetaData(string directoryPath);

	bool MoveDirectory(string sourceDirectory, string destinationDirectory);

	bool MoveFile(string sourcePath, string sourceDestination);

	Task<byte[]> ReadAllBytes(string path);

	byte[] ReadAllBytesSync(string path);

	Task<List<string>> ReadAllLines(string path);

	List<string> ReadAllLinesSync(string path);

	Task<string> ReadAllText(string path);

	string ReadAllTextSync(string path);

	Task WriteAllBytes(string path, byte[] bytes);

	Task WriteAllText(string path, string text);

	void WriteAllTextSync(string path, string text);
}

public class FileSystemTools(IFileSystem fileSystem) : IFileSystemTools
{
	public async Task AppendToFile(string path, string text)
	{
		EnsureFile(path);

		await fileSystem.File.AppendAllTextAsync(path, text);
	}

	public void AppendToFileSync(string path, string text)
	{
		EnsureFile(path);

		fileSystem.File.AppendAllText(path, text);
	}

	public void DeleteDirectory(string path, bool recursive = true)
	{
		if (DirectoryExists(path))
		{
			fileSystem.Directory.Delete(path, recursive);
		}
	}

	public bool DeleteFile(string path)
	{
		if (!FileExists(path))
		{
			return false;
		}

		fileSystem.File.Delete(path);

		return true;
	}

	public bool DirectoryExists(string path)
	{
		return fileSystem.Directory.Exists(path);
	}

	public void EnsureDirectory(string path)
	{
		if (DirectoryExists(path))
		{
			return;
		}

		fileSystem.Directory.CreateDirectory(path);
	}

	public void EnsureFile(string path)
	{
		EnsureDirectory(Path.GetDirectoryName(path)!);

		if (FileExists(path))
		{
			return;
		}

		using var _ = fileSystem.File.Create(path);
	}

	public bool FileExists(string path)
	{
		return fileSystem.File.Exists(path);
	}

	public List<string> GetDirectories(string path)
	{
		if (!DirectoryExists(path))
		{
			return new List<string>();
		}

		var directories = fileSystem.Directory.GetDirectories(path);

		return directories.ToList();
	}

	public IFileInfo GetFileMetaData(string fullPath)
	{
		return fileSystem.FileInfo.New(fullPath);
	}

	public List<string> GetFiles(string directoryPath)
	{
		if (!DirectoryExists(directoryPath))
		{
			return new List<string>();
		}

		var files = fileSystem.Directory.GetFiles(directoryPath);

		return files.ToList();
	}

	public List<IFileInfo> GetFilesWithMetaData(string directoryPath)
	{
		var files = GetFiles(directoryPath);

		return files.Select(GetFileMetaData).ToList();
	}

	public bool MoveDirectory(string sourceDirectory, string destinationDirectory)
	{
		if (!DirectoryExists(sourceDirectory))
		{
			return false;
		}

		fileSystem.Directory.Move(sourceDirectory, destinationDirectory);

		return true;
	}

	public bool MoveFile(string sourcePath, string sourceDestination)
	{
		if (!FileExists(sourcePath))
		{
			return false;
		}

		fileSystem.File.Move(sourcePath, sourceDestination);

		return true;
	}

	public async Task<byte[]> ReadAllBytes(string path)
	{
		return FileDoesNotExist(path) ? [] : await fileSystem.File.ReadAllBytesAsync(path);
	}

	public byte[] ReadAllBytesSync(string path)
	{
		return FileDoesNotExist(path) ? [] : fileSystem.File.ReadAllBytes(path);
	}

	public async Task<List<string>> ReadAllLines(string path)
	{
		if (!fileSystem.File.Exists(path))
		{
			return [];
		}

		var lines = await fileSystem.File.ReadAllLinesAsync(path);

		return lines.ToList();
	}

	public List<string> ReadAllLinesSync(string path)
	{
		if (!fileSystem.File.Exists(path))
		{
			return [];
		}

		var lines = fileSystem.File.ReadAllLines(path);

		return lines.ToList();
	}

	public async Task<string> ReadAllText(string path)
	{
		return FileDoesNotExist(path) ? string.Empty : await fileSystem.File.ReadAllTextAsync(path);
	}

	public string ReadAllTextSync(string path)
	{
		return FileDoesNotExist(path) ? string.Empty : fileSystem.File.ReadAllText(path);
	}

	public async Task WriteAllBytes(string path, byte[] bytes)
	{
		EnsureFile(path);

		await fileSystem.File.WriteAllBytesAsync(path, bytes);
	}

	public async Task WriteAllText(string path, string text)
	{
		EnsureFile(path);

		await fileSystem.File.WriteAllTextAsync(path, text);
	}

	public void WriteAllTextSync(string path, string text)
	{
		EnsureFile(path);

		fileSystem.File.WriteAllText(path, text);
	}

	private bool FileDoesNotExist(string path)
	{
		return !FileExists(path);
	}
}
