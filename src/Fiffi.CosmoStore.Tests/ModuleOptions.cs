using System;

namespace Fiffi.CosmoStore.Tests
{
    public class ModuleOptions
    {
        public string ConnectionString
            => $"AccountEndpoint={ServiceUri};AccountKey={Key}";
        //AccountEndpoint=https://accountname.documents.azure.com:443/‌​;AccountKey=accountk‌​ey==;Database=database

        public Uri ServiceUri { get; set; }
        public string Key { get; set; }
    }
}