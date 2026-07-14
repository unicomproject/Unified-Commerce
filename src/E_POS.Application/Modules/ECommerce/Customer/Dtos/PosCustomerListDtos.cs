namespace E_POS.Application.Modules.ECommerce.Customer.Dtos;

public sealed record PosCustomerCreateRequestDto(
    string FullName,
    string Phone,
    string? Email);

public sealed record PosCustomerListItemResponseDto(
    Guid CustomerId,
    string FullName,
    string? Phone,
    string? Email,
    string Status);

public sealed record PosCustomerListResponseDto(
    IReadOnlyList<PosCustomerListItemResponseDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
