using E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;
using Npgsql;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PosCheckoutPersistenceFailureTests
{
    [Theory]
    [InlineData("ux_sales_payments_3aae300c")]
    [InlineData("ux_sales_payment_transactions_e759526b")]
    public void PaymentIdempotencyUniqueConstraint_IsClassifiedAsConflict(string constraint)
    {
        var code = PosCheckoutRepository.ClassifyPersistenceFailure(
            PostgresErrorCodes.UniqueViolation, constraint);

        Assert.Equal("pos_checkout.idempotency_conflict", code);
    }

    [Theory]
    [InlineData("23503", "fk_sales_payments_sales_order")]
    [InlineData("23505", "uq_receipts_tenant_id_receipt_number")]
    [InlineData(null, null)]
    public void OtherDatabaseFailure_IsNotMislabelledAsIdempotencyConflict(
        string? state, string? constraint)
    {
        var code = PosCheckoutRepository.ClassifyPersistenceFailure(state, constraint);

        Assert.Equal("pos_checkout.persistence_failed", code);
    }
}
