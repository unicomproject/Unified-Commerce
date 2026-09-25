using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

/// <summary>
/// Server-side fetch of external product image candidates (scanner-first Use This Product).
/// Never trusts the URL as product media — bytes are validated and staged via the existing media pipeline.
/// </summary>
public interface IExternalImageCandidateFetcher
{
    /// <summary>
    /// Downloads image bytes from an absolute http(s) URL into a <see cref="MediaUploadFile"/>.
    /// Caller owns and must dispose <see cref="MediaUploadFile.Content"/>.
    /// </summary>
    Task<ApplicationResult<MediaUploadFile>> FetchAsync(
        string imageUrl,
        CancellationToken cancellationToken);
}
