using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiffi.FireStore;
public static class FilterExtensions
{
    public static Query ApplyFilters(this Query q, params IStreamFilter[] filters)
     => filters.Aggregate(q, (current, filter) =>
            filter switch
            {
                DateStreamFilter f => current.Date(f),
                _ => current
            });

    public static IEnumerable<DocumentSnapshot> ApplyFilters(this IEnumerable<DocumentSnapshot> documents, params IStreamFilter[] filters)
     => filters.Aggregate(documents, (current, filter) =>
        filter switch
        {
            CategoryStreamFilter f => current.Category(f),
            CategoryMetaDataStreamFilter f => current.CategoryMetaData(f),
            _ => current
        });

    public static Query Date(this Query q, DateStreamFilter filter)
     => q
        .WhereGreaterThanOrEqualTo(nameof(EventData.Created), filter.StartDate)
        .WhereLessThanOrEqualTo(nameof(EventData.Created), filter.EndDate)
        .OrderBy(nameof(EventData.Created));

    public static IEnumerable<DocumentSnapshot> Category(this IEnumerable<DocumentSnapshot> documents, CategoryStreamFilter filter)
        => documents
            .Where(x => !x.Id.Contains("|head"))
            .Where(x => !x.Id.Contains("|snapshot"))
            .Where(x => x.GetValue<string>(nameof(EventData.EventStreamId)).StartsWith(filter.CategoryName, StringComparison.InvariantCultureIgnoreCase));

    public static IEnumerable<DocumentSnapshot> CategoryMetaData(this IEnumerable<DocumentSnapshot> documents, CategoryMetaDataStreamFilter filter)
    => documents
        .Where(x => !x.Id.Contains("|head"))
        .Where(x => !x.Id.Contains("|snapshot"))
        .Where(x => x.GetValue<string>(new FieldPath(filter.LowerCasePath ? "data" : "Data", filter.LowerCasePath ? "meta" : "Meta", "streamname")).Split('/').Last().StartsWith(filter.CategoryName, StringComparison.InvariantCultureIgnoreCase));
}
