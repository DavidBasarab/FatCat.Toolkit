namespace FatCat.Toolkit.Data.Mongo;

/// <summary>
/// One page of a filtered, sorted query, together with the total number of documents that matched the
/// filter. <b><see cref="TotalCount" /> is every matching document, not the size of <see cref="Items" /></b>
/// — a caller paging through results needs to know how much lies beyond the page, and a count that
/// respected <c>skip</c>/<c>limit</c> would only re-report the page size (ADR-4).
/// </summary>
public class PagedResults<T> : EqualObject
	where T : MongoObject
{
	public List<T> Items { get; set; } = [];

	public long TotalCount { get; set; }
}
