using System;

namespace E_POS.Application.Modules.Shared.Storage.Contracts;

public interface IAzureSasTokenProvider
{
    /// <summary>
    /// Generates a read-only SAS token for the specified blob and appends it to the blob URL.
    /// Returns the original URL if the blob URL is invalid or empty.
    /// </summary>
    /// <param name="blobUrl">The full public URL of the blob.</param>
    /// <returns>The blob URL with the SAS token appended.</returns>
    string AppendReadSasToken(string blobUrl);
}
