namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public sealed class BrandPersistenceException : Exception
{
    public BrandPersistenceException(string errorCode, Exception? innerException = null)
        : base(errorCode, innerException) => ErrorCode = errorCode;

    public string ErrorCode { get; }
}
