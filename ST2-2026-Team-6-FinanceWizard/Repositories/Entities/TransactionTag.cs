namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

/// <summary>
/// Join entity: Transaction <-> Tag (many-to-many).
///
/// Design notes:
/// - In EF Core, a many-to-many relation normally uses a hidden join table.
///   Here the join is materialised as a real entity because we need composite-key
///   cascade-delete configuration (see ApplicationDbContext).
/// - Composite primary key: (TransactionId, TagId) — prevents duplicate tag attachments
///   at the DB level, not just in code.
/// </summary>
public class TransactionTag
{
    public long TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    public long TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
