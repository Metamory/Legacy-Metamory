using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Metamory.Api.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Metamory.Api.Providers.AzureStorage
{
	public class AzureTableStorageStatusRepository : IStatusRepository
	{
		private readonly CloudTableClient _tableClient;

		public AzureTableStorageStatusRepository(CloudStorageAccount storageAccount)
		{
			_tableClient = storageAccount.CreateCloudTableClient();
		}

		[Obsolete("Use async version instead")]
		public IEnumerable<ContentStatusEntity> GetStatusEntries(string siteId, string contentId)
		{
			var table = _tableClient.GetTableReference(siteId);
			if (!table.Exists()) return Enumerable.Empty<ContentStatusEntity>();

			var query = new TableQuery<ContentStatusEntity>()
				.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, contentId));
			var statusEntries = table.ExecuteQuery(query);
			return statusEntries;
		}

	
		public async Task<IEnumerable<ContentStatusEntity>> GetStatusEntriesAsync(string siteId, string contentId)
		{
			var table = _tableClient.GetTableReference(siteId);
			if (!await table.ExistsAsync()) return Enumerable.Empty<ContentStatusEntity>();

			var query = new TableQuery<ContentStatusEntity>()
				.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, contentId));

			//var statusEntries = table.ExecuteQuery(query);
			var statusEntries = new List<ContentStatusEntity>();
			TableContinuationToken token = null;
			do
			{
				var res = await table.ExecuteQuerySegmentedAsync(query,token);
				statusEntries.AddRange(res.Results);

				token = res.ContinuationToken;
			} while (token != null);
	
			return statusEntries;
		}

		[Obsolete("Use async version instead")]
		public void AddStatusEntry(string siteId, ContentStatusEntity statusEntry)
		{
			var table = _tableClient.GetTableReference(siteId);
			table.CreateIfNotExists();
			var insertOperation = TableOperation.Insert(statusEntry);
			table.Execute(insertOperation);
		}

		public async Task AddStatusEntryAsync(string siteId, ContentStatusEntity statusEntry)
		{
			var table = _tableClient.GetTableReference(siteId);
			await table.CreateIfNotExistsAsync();
			var insertOperation = TableOperation.Insert(statusEntry);
			await table.ExecuteAsync(insertOperation);
		}
	}
}