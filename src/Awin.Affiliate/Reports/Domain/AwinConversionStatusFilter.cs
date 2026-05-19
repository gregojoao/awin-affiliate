namespace Awin.Affiliate.Reports.Domain;

/// <summary>Filter applied to <c>ListConversionsAsync</c>.</summary>
public enum AwinConversionStatusFilter
{
    /// <summary>Return every status. Same as omitting the filter.</summary>
    All = 0,

    /// <summary>Approved transactions only.</summary>
    Approved,

    /// <summary>Pending transactions only.</summary>
    Pending,

    /// <summary>Declined transactions only.</summary>
    Declined
}
