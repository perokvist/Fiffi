using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi.Streamstone
{
	public class EventEntity : TableEntity
	{
		public string Type { get; set; }
		public string Data { get; set; }
		public int Version { get; set; }
		public long Offset { get; set; }
		public long SequenceNumber { get; set; }

	}
}
