namespace ST2_2026_Team_6_FinanceWizard.Services.Builders
{
    public class TransactionQueryBuilder
    {
        private IQueryable<Transaction> _query;

        public TransactionQueryBuilder(IQueryable<Transaction> initialQuery)
        {
            _query = initialQuery;
        }

        public TransactionQueryBuilder FilterByUser(long userId)
        {
            _query = _query.Where(t => t.Wallet.UserId == userId);
            return this;
        }

        public TransactionQueryBuilder FilterByDateRange(DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue)
                _query = _query.Where(t => t.Date >= fromDate.Value.Date);

            // Include the whole "to" day
            if (toDate.HasValue)
            {
                var exclusiveEnd = toDate.Value.Date.AddDays(1);
                _query = _query.Where(t => t.Date < exclusiveEnd);
            }
            return this;
        }

        public TransactionQueryBuilder SearchText(string? keyword)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim().ToLower();
                _query = _query.Where(t => t.Description != null && t.Description.ToLower().Contains(term));
            }
            return this;
        }

        public TransactionQueryBuilder OrderByDescendingDate()
        {
            _query = _query.OrderByDescending(t => t.Date);
            return this;
        }

        public IQueryable<Transaction> Build() => _query;
    }
}
