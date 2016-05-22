using System;

namespace Metamory.Api.Repositories
{
	public class VersionCargo
	{
		public string Version { get; set; }
		public string PreviousVersion { get; set; }
		public DateTimeOffset Timestamp { get; set; }
		public string Author { get; set; }
		public string Label { get; set; }
	}
}