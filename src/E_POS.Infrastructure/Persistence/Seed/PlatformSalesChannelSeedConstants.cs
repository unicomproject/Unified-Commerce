namespace E_POS.Infrastructure.Persistence.Seed;

public static class PlatformSalesChannelSeedConstants
{
    public static readonly Guid PhysicalChannelId = Guid.Parse("d0000000-0000-4000-8000-000000000001");
    public static readonly Guid OnlineChannelId = Guid.Parse("d0000000-0000-4000-8000-000000000002");
    public static readonly Guid PosChannelId = Guid.Parse("d0000000-0000-4000-8000-000000000003");

    public const string PosChannelCode = "POS";
    public const string PosChannelName = "Point of Sale";
    public const string PosChannelType = "POS";
}
