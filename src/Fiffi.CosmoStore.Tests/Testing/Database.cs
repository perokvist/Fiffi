using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi.CosmoStore.Testing
{
    public static class Database
    {
        public static async Task DeleteEventStoreAsync(Uri serviceUri, string key)
        {
            using (var c = new DocumentClient(serviceUri, key))
            {
                var database = c.CreateDatabaseQuery()
                    .Where(x => x.Id == "EventStore")
                    .ToList();
                if (database.Any())
                {
                    _ = await c.DeleteDatabaseAsync(database.First().SelfLink);
                }
            }

        }

    }
}
