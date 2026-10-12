namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

// Join entity: Transaction <-> Tag (many-to-many)
public class TransactionTag
{
    public long TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    public long TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
