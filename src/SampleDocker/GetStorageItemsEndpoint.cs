using Azure.Identity;
using Azure.Storage.Blobs;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Extensions;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace SampleDocker;

[AllowAnonymous]
public class GetStorageItemsEndpoint : Endpoint
{
	private const string ContainerName = "quickstartblobs-da3c108f-fb28-47cb-8e57-4a2578253c3c";
	private readonly IConfiguration configuration;

	public GetStorageItemsEndpoint(IConfiguration configuration)
	{
		this.configuration = configuration;
	}

	[HttpPost("api/store")]
	public async Task<WebResult> GetStorageItems()
	{
		BlobServiceClient blobServiceClient;

		var inCloud = configuration["InCloud"].ToBool();

		if (inCloud)
		{
			var blobUrl = configuration["BlobUrl"];

			blobServiceClient = new BlobServiceClient(new Uri(blobUrl), new DefaultAzureCredential());
		}
		else
		{
			var blobConnectionString = configuration.GetConnectionString("BlobConnection");

			ConsoleLog.WriteCyan(blobConnectionString);

			// TODO: Replace <storage-account-name> with your actual storage account name

			blobServiceClient = new BlobServiceClient(blobConnectionString);
		}

		//Create a unique name for the container

		// Create the container and return a container client object
		var items = blobServiceClient.GetBlobContainers();

		foreach (var item in items)
		{
			ConsoleLog.WriteCyan($"{item.Name}");
		}

		BlobContainerClient containerClient;

		if (items.All(i => i.Name != ContainerName))
		{
			containerClient = await blobServiceClient.CreateBlobContainerAsync(ContainerName);
		}
		else
		{
			containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
		}

		// Create a local file in the ./data/ directory for uploading and downloading
		var localPath = "data";
		Directory.CreateDirectory(localPath);
		var fileName = "quickstart_Dude.txt";
		var localFilePath = Path.Combine(localPath, fileName);

		if (System.IO.File.Exists(localFilePath))
		{
			System.IO.File.Delete(localFilePath);
		}

		// Write text to the file
		await System.IO.File.WriteAllTextAsync(localFilePath, $"Hello, World! | <{DateTime.Now:hh:mm:ss}>");

		// Get a reference to a blob
		var blobClient = containerClient.GetBlobClient(fileName);

		Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

		// Upload data from the local file
		await blobClient.UploadAsync(localFilePath, true);

		// Download the blob to a local file
		// Append the string "DOWNLOADED" before the .txt extension
		// so you can compare the files in the data directory
		var downloadFilePath = localFilePath.Replace(".txt", "DOWNLOADED.txt");

		ConsoleLog.Write($"Full Path <{downloadFilePath}>");

		// Download the blob's contents and save it to a file
		await blobClient.DownloadToAsync(downloadFilePath);

		var downloadedText = await System.IO.File.ReadAllTextAsync(downloadFilePath);

		ConsoleLog.WriteMagenta(downloadedText);

		return Ok(downloadedText);
	}
}
