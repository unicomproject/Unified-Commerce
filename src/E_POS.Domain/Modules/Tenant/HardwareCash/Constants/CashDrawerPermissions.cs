namespace E_POS.Domain.Modules.Tenant.HardwareCash.Constants;

public static class CashDrawerPermissions
{
    public const string View = "cash_drawer.view";
    public const string Manage = "cash_drawer.manage";
    public const string CreateMovement = "cash_drawer.movement.create";

    /// <summary>
    /// Canonical cash-drawer codes (Chunk 2 definitions).
    /// Chunk 6 enforces Cash In / Cash Out / Cash Drop independently on create-movement.
    /// </summary>
    public static class Canonical
    {
        public const string PositionView = "pos.cash_drawer.position.view";
        public const string PhysicalManage = "pos.cash_drawer.physical.manage";
        public const string MovementsCreate = "pos.cash_drawer.movements.create";
        public const string CashIn = "pos.cash_drawer.movements.cash_in";
        public const string CashOut = "pos.cash_drawer.movements.cash_out";
        public const string CashDrop = "pos.cash_drawer.movements.cash_drop";
    }
}
