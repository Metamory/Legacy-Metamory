﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Metamory.Api.Repositories;

namespace Metamory.Api
{
	public class ContentVersioningService : IDisposable
	{
		private readonly IStatusRepository _statusRepository;
		private readonly IContentRepository _contentRepository;
		private readonly VersioningService _versioningService;
		private readonly CanonicalizeService _canonicalizeService;

		public ContentVersioningService(IStatusRepository statusRepository,
			IContentRepository contentRepository,
			VersioningService versioningService,
			CanonicalizeService canonicalizeService)
		{
			_statusRepository = statusRepository;
			_contentRepository = contentRepository;
			_versioningService = versioningService;
			_canonicalizeService = canonicalizeService;
		}

		public async Task<string> GetCurrentlyPublishedVersionIdAsync(string siteId, string contentId, DateTimeOffset now)
		{
			siteId = _canonicalizeService.Canonicalize(siteId);
			contentId = _canonicalizeService.Canonicalize(contentId);

			var statusEntries = await _statusRepository.GetStatusEntriesAsync(siteId, contentId);
			var publishedVersionId = _versioningService.GetCurrentlyPublishedVersion(now, statusEntries);
			return publishedVersionId;
		}

		public async Task<string> DownloadPublishedContentToStreamAsync(string siteId, string contentId, DateTimeOffset now, Stream target)
		{
			var publishedVersionId = await GetCurrentlyPublishedVersionIdAsync(siteId, contentId, now);
			if (publishedVersionId == null) return null;

			var contentType = await DownloadContentToStreamAsync(siteId, contentId, publishedVersionId, target);
			return contentType;
		}

		public async Task ChangeStatusForContentAsync(string siteId, string contentId, string versionId,
			string status, string responsible, DateTimeOffset now, DateTimeOffset? startDate)
		{
			siteId = _canonicalizeService.Canonicalize(siteId);
			contentId = _canonicalizeService.Canonicalize(contentId);
			versionId = _canonicalizeService.Canonicalize(versionId);

			var statusEntry = new ContentStatusEntity
			{
				PartitionKey = contentId,
				RowKey = Guid.NewGuid().ToString(),
				Timestamp = now,
				StartTime = startDate ?? now,
				VersionId = versionId,
				Status = status,
				Responsible = responsible
			};
			await _statusRepository.AddStatusEntryAsync(siteId, statusEntry);
		}

		public async Task<IEnumerable<ContentMetadata>> GetVersionsAsync(string siteId, string contentId)
		{
			siteId = _canonicalizeService.Canonicalize(siteId);
			contentId = _canonicalizeService.Canonicalize(contentId);

			var versions = await _contentRepository.GetVersionsAsync(siteId, contentId);
			var statusEntries = await _statusRepository.GetStatusEntriesAsync(siteId, contentId);

			var now = DateTimeOffset.Now;
			var publishedVersionId = _versioningService.GetCurrentlyPublishedVersion(now, statusEntries);

			return from version in versions
				   orderby version.Timestamp
				   select new ContentMetadata
				   {
					   VersionId = version.Version,
					   PreviousVersionId = version.PreviousVersion,
					   Timestamp = version.Timestamp,
					   Author = version.Author,
					   Label = version.Label,
					   IsPublished = publishedVersionId == version.Version
				   };
		}

		public async Task<ContentMetadata> GetMetadataAsync(string siteId, string contentId, string versionId)
		{
			var versions = await GetVersionsAsync(siteId, contentId);
			return versions.SingleOrDefault(x => x.VersionId == versionId);
		}

		public async Task<string> DownloadContentToStreamAsync(string siteId, string contentId, string versionId, Stream target)
		{
			siteId = _canonicalizeService.Canonicalize(siteId);
			contentId = _canonicalizeService.Canonicalize(contentId);
			versionId = _canonicalizeService.Canonicalize(versionId);

			var contentType = await _contentRepository.DownloadContentToStreamAsync(siteId, contentId, versionId, target);

			return contentType;
		}

		public async Task<ContentMetadata> StoreAsync(string siteId,
			string contentId,
			DateTimeOffset now,
			Stream contentStream,
			string contentType,
			string previousVersionId = null,
			string author = null,
			string label = null)
		{
			siteId = _canonicalizeService.Canonicalize(siteId);
			contentId = _canonicalizeService.Canonicalize(contentId);
			previousVersionId = _canonicalizeService.Canonicalize(previousVersionId);

			var versionId = Guid.NewGuid().ToString();

			var status = "Draft";
			var statusEntry = new ContentStatusEntity
			{
				PartitionKey = contentId,
				RowKey = Guid.NewGuid().ToString(),
				Timestamp = now,
				StartTime = now,
				VersionId = versionId,
				Status = status
			};
			var t1 = _statusRepository.AddStatusEntryAsync(siteId, statusEntry);

			var t2 = _contentRepository.AddContentAsync(siteId, contentId, versionId, contentStream, contentType, now, previousVersionId, author, label);

			await Task.WhenAll(new[]{t1, t2});

			return new ContentMetadata
			{
				VersionId = versionId,
				Timestamp = now,
				PreviousVersionId = previousVersionId,
				Author = author,
				Label = label
			};
		}

		//public void DeleteContent(string siteId, string contentId)
		//{
		//	siteId = _canonicalizeService.Canonicalize(siteId);
		//	contentId = _canonicalizeService.Canonicalize(contentId);

		//	throw new NotImplementedException();
		//}
		
		void IDisposable.Dispose()
		{
			// ...
		}
	}
}