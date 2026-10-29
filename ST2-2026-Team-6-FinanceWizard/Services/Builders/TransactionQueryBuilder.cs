using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Builders
{
    /// <summary>
    /// Builder Pattern for building a transaction query step by step.
    ///
    /// Why Builder for LINQ?
    /// - The transaction list page has several independent filters (user, dates, text, includes, ordering).
    /// - Building the query inline becomes a wall of "if" statements and complicated nesting.
    /// - The builder gives one method per concern. The caller can chain only what is needed,
    ///   and the resulting IQueryable is still translated to SQL by EF Core — no extra client-side evaluation.
    ///
    /// Each method returns "this", so calls are fluent and chainable.
    /// The query stays deferred (nothing hits the DB) until the caller materialises it
    /// with ToListAsync / CountAsync, so the controller can run CountAsync + Skip/Take on the same query.
    /// </summary>
    public class TransactionQueryBuilder
    {
        private IQueryable<Transaction> _query;

        public TransactionQueryBuilder(IQueryable<Transaction> initialQuery)
        {
            _query = initialQuery;
        }

        /// <summary>
        /// Restricts the query to the user's own wallets. This must be applied first
        /// as a security gate, so no other filter can accidentally bypass it.
        /// </summary>
        public TransactionQueryBuilder FilterByUser(long userId)
        {
            _query = _query.Where(t => t.Wallet.UserId == userId);
            return this;
        }

        /// <summary>
        /// Adds an optional date range. Both ends are optional:
        /// - fromDate (inclusive) filters the start of the day
        /// - toDate   (inclusive) filters using exclusive-end-of-day, so the whole "to" day is included
        /// Using "< exclusiveEnd" instead of "<= toDate" is the correct way to include a whole day.
        /// </summary>
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

        /// <summary>
        /// Case-insensitive substring search on the description.
        /// The keyword is trimmed and lowered in C# (not in SQL) because some DB collations
        /// are case-sensitive, and doing it here guarantees the same behaviour everywhere.
        /// </summary>
        public TransactionQueryBuilder SearchText(string? keyword)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim().ToLower();
                _query = _query.Where(t => t.Description != null && t.Description.ToLower().Contains(term));
            }
            return this;
        }

        /// <summary>
        /// Eagerly loads related entities (wallet, merchant, category, tags) so the view
        /// can render them without triggering extra SQL queries per row (N+1 problem).
        /// </summary>
        public TransactionQueryBuilder IncludeDetails()
        {
            _query = _query
                .Include(t => t.Wallet)
                .Include(t => t.Merchant)
                .Include(t => t.Category)
                .Include(t => t.TransactionTags).ThenInclude(tt => tt.Tag);
            return this;
        }

        /// <summary>
        /// Orders the query newest-first. Required for Skip/Take to work predictably.
        /// </summary>
        public TransactionQueryBuilder OrderByDescendingDate()
        {
            _query = _query.OrderByDescending(t => t.Date);
            return this;
        }

        /// <summary>
        /// Returns the composed query. The caller can then materialise it (ToList, Count, ...).
        /// </summary>
        public IQueryable<Transaction> Build() => _query;
    }
}
