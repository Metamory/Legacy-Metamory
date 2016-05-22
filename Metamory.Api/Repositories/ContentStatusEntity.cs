using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Metamory.Api.Repositories
{
	public class ContentStatusEntity : TableEntity
	{
		public DateTimeOffset StartTime { get; set; }
		public string Status { get; set; }
		public string VersionId { get; set; }
		public string Responsible { get; set; }

		public ContentStatusEntity(string contentId, string version)
		{
			PartitionKey = contentId;
			RowKey = version;
		}

		public ContentStatusEntity() { }

	}
}