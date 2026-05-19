namespace Awin.Affiliate.Infrastructure;

/// <summary>Base type for every exception raised by the Awin SDK.</summary>
public class AwinAffiliateException : Exception
{
    /// <summary>Creates a new <see cref="AwinAffiliateException"/>.</summary>
    public AwinAffiliateException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a new <see cref="AwinAffiliateException"/> wrapping an inner exception.</summary>
    public AwinAffiliateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
