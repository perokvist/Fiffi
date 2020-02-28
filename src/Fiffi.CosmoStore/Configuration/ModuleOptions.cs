using System;
using System.ComponentModel.DataAnnotations;

namespace Fiffi.CosmoStore.Configuration
{
    public class ModuleOptions
    {
        public ModuleOptions()
        {
            TypeResolver = Fiffi.TypeResolver.Default();
        }

        [Required]
        public string ConnectionString
            => $"AccountEndpoint={ServiceUri};AccountKey={Key}==";
        //AccountEndpoint=https://accountname.documents.azure.com:443/‌​;AccountKey=accountk‌​ey==;Database=database

        [Required]
        public string HostName { get; set; }

        public Func<string, Type> TypeResolver { get; set; }

        [Required]
        public string Key { get; set; }

        [Required]
        public Uri ServiceUri { get; set; }
    }
}
