using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Metamory.Api.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Metamory.Api.Providers.AzureStorage
{
	public class AzureBlobContentRepository : IContentRepository
	{
		private static class MetadataKeynames
		{
			public const string PreviousVersion = "PreviousVersion";
			public const string Timestamp = "Timestamp";
			public const string Author = "Author";
			public const string Label = "Label";
		}

		private readonly CloudBlobClient _blobClient;

		public AzureBlobContentRepository(CloudStorageAccount storageAccount)
		{
			_blobClient = storageAccount.CreateCloudBlobClient();
		}

		[Obsolete("Use async version instead")]
		public string DownloadContentToStream(string siteId, string contentId, string versionId, Stream memoryStream)
		{
			var container = _blobClient.GetContainerReference(siteId);
			var blobname = string.Format("{0}/{1}", contentId, versionId);
			var blobRef = container.GetBlockBlobReference(blobname);
			blobRef.FetchAttributes();
			var contentType = blobRef.Properties.ContentType;
			blobRef.DownloadToStream(memoryStream);
			return contentType;
		}

		public async Task<string> DownloadContentToStreamAsync(string siteId, string contentId, string versionId, Stream memoryStream)
		{
			var container = _blobClient.GetContainerReference(siteId);
			var blobname = string.Format("{0}/{1}", contentId, versionId);
			var blobRef = container.GetBlockBlobReference(blobname);
			await blobRef.FetchAttributesAsync();
			var contentType = blobRef.Properties.ContentType;
			await blobRef.DownloadToStreamAsync(memoryStream);
			return contentType;
		}

		[Obsolete("Use async version instead")]
		public void AddContent(string siteId, string contentId, string versionId, Stream contentStream, string contentType, DateTimeOffset now, string previousVersionId, string author, string label)
		{
			var blobname = string.Format("{0}/{1}", contentId, versionId);
			var container = _blobClient.GetContainerReference(siteId);
			container.CreateIfNotExists();
			var blockBlob = container.GetBlockBlobReference(blobname);
			blockBlob.UploadFromStream(contentStream);

			Action<string, string> setMetadata = (val, key) => { if (!string.IsNullOrWhiteSpace(val)) blockBlob.Metadata[key] = val; };

			setMetadata(previousVersionId, MetadataKeynames.PreviousVersion);
			setMetadata(now.ToString("o"), MetadataKeynames.Timestamp);
			setMetadata(author, MetadataKeynames.Author);
			setMetadata(label, MetadataKeynames.Label);
			blockBlob.SetMetadata();
			blockBlob.Properties.ContentType = contentType;
			blockBlob.SetProperties();

			//TODO: Arjan: Make sure meta is stored
		}

		public async Task AddContentAsync(string siteId, string contentId, string versionId, Stream contentStream, string contentType, DateTimeOffset now, string previousVersionId, string author, string label)
		{
			var blobname = string.Format("{0}/{1}", contentId, versionId);
			var container = _blobClient.GetContainerReference(siteId);
			await container.CreateIfNotExistsAsync();
			var blockBlob = container.GetBlockBlobReference(blobname);
			await blockBlob.UploadFromStreamAsync(contentStream);

			Action<string, string> setMetadata = (val, key) => { if (!string.IsNullOrWhiteSpace(val)) blockBlob.Metadata[key] = val; };

			setMetadata(previousVersionId, MetadataKeynames.PreviousVersion);
			setMetadata(now.ToString("o"), MetadataKeynames.Timestamp);
			setMetadata(author, MetadataKeynames.Author);
			setMetadata(label, MetadataKeynames.Label);
			await blockBlob.SetMetadataAsync();
			blockBlob.Properties.ContentType = contentType;
			await blockBlob.SetPropertiesAsync();

			//TODO: Arjan: Make sure meta is stored
		}

		[Obsolete("Use async version instead")]
		public IEnumerable<VersionCargo> GetVersions(string siteId, string contentId)
		{
			Func<IDictionary<string, string>, string, string> getMetadataIfExists =
				(metadata, key) => metadata.ContainsKey(key) ? metadata[key] : null;

			var container = _blobClient.GetContainerReference(siteId);
			if (!container.Exists()) return Enumerable.Empty<VersionCargo>();

			var contentDirectory = container.GetDirectoryReference(contentId);
			var versionList = contentDirectory.ListBlobs(blobListingDetails: BlobListingDetails.Metadata)
				.Cast<CloudBlockBlob>()
				.Select(x => new VersionCargo
				{
					Version = x.Name.Split('/')[1],
					PreviousVersion = getMetadataIfExists(x.Metadata, MetadataKeynames.PreviousVersion),
					Timestamp = DateTimeOffset.Parse(x.Metadata[MetadataKeynames.Timestamp]),
					Author = getMetadataIfExists(x.Metadata, MetadataKeynames.Author),
					Label = getMetadataIfExists(x.Metadata, MetadataKeynames.Label)
				});

			return versionList;
		}

		public async Task<IEnumerable<VersionCargo>> GetVersionsAsync(string siteId, string contentId)
		{
			Func<IDictionary<string, string>, string, string> getMetadataIfExists =
				(metadata, key) => metadata.ContainsKey(key) ? metadata[key] : null;

			var container = _blobClient.GetContainerReference(siteId);
			if (!await container.ExistsAsync()) return Enumerable.Empty<VersionCargo>();

			var contentDirectory = container.GetDirectoryReference(contentId);

			var options = new BlobRequestOptions();
			BlobContinuationToken token = null;
			var versionList = new List<VersionCargo>();
			do
			{
				var res = await contentDirectory.ListBlobsSegmentedAsync(false, BlobListingDetails.Metadata, null, token, options, null);
				versionList.AddRange(res.Results
					.Cast<CloudBlockBlob>()
					.Select(x => new VersionCargo
					{
						Version = x.Name.Split('/')[1],
						PreviousVersion = getMetadataIfExists(x.Metadata, MetadataKeynames.PreviousVersion),
						Timestamp = DateTimeOffset.Parse(x.Metadata[MetadataKeynames.Timestamp]),
						Author = getMetadataIfExists(x.Metadata, MetadataKeynames.Author),
						Label = getMetadataIfExists(x.Metadata, MetadataKeynames.Label)
					}));

				token = res.ContinuationToken;
			} while (token != null);

			return versionList;
		}
	}
}