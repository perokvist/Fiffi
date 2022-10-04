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
            CategoryWithinStreamFilter f => current.Category(f),
            _ => current
        });

    public static Query Date(this Query q, DateStreamFilter filter)
     => q
        .WhereGreaterThanOrEqualTo(nameof(EventData.Created), filter.StartDate)
        .WhereLessThanOrEqualTo(nameof(EventData.Created), filter.EndDate);

    public static IEnumerable<DocumentSnapshot> Category(this IEnumerable<DocumentSnapshot> documents, CategoryWithinStreamFilter filter)
        => documents
            .Where(x => x.GetValue<string>(nameof(EventData.EventStreamId)).StartsWith(filter.CategoryName, StringComparison.InvariantCultureIgnoreCase));


}
