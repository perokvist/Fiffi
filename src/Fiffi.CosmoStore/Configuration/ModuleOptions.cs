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
            public Uri ServiceUri { get; set; }

            [Required]
            public string Key { get; set; }

            [Required]
            public string HostName { get; set; }

            public Func<string, Type> TypeResolver { get; set; }
        }
}
