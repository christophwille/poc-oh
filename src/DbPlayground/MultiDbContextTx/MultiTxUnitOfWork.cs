using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MultiDbContextTx
{
    public class MultiTxUnitOfWork
    {
        private readonly SqlServerBloggingContext _bloggingContext;
        private readonly SqlServerEmailContext _emailContext;

        public MultiTxUnitOfWork(SqlServerBloggingContext bloggingContext, SqlServerEmailContext emailContext)
        {
            _bloggingContext = bloggingContext;
            _emailContext = emailContext;
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            IDbContextTransaction tx = await _bloggingContext.Database.BeginTransactionAsync();
            await _emailContext.Database.UseTransactionAsync(tx.GetDbTransaction());

            return tx;
        }
    }
}
