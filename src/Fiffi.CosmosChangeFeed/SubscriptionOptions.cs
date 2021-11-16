using System;
using System.ComponentModel.DataAnnotations;

namespace Fiffi.CosmosChangeFeed;

public class SubscriptionOptions
{
    [Required]
    public string ConnectionString
        => $"AccountEndpoint={ServiceUri};AccountKey={Key}";
    //AccountEndpoint=https://accountname.documents.azure.com:443/‌​;AccountKey=accountk‌​ey==;Database=database

    [Required]
    public string InstanceName { get; set; }

    [Required]
    public string ProcessorName { get; set; }

    [Required]
    public string Key { get; set; }

    [Required]
    public Uri ServiceUri { get; set; }

    [Required]
    public string DatabaseName { get; set; }

    [Required]
    public string ContainerId { get; set; }

}
