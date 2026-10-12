using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.UnitOfWork;

internal sealed class DbSession(IDbConnectionFactory connectionFactory) : IAsyncDisposable
{
    private readonly SqlConnection _connection = connectionFactory.CreateConnection();
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private SqlTransaction? _transaction;
    private bool _disposed;

    public SqlTransaction? Transaction => _transaction;

    public async Task<SqlConnection> GetOpenConnectionAsync()
    {
        await _connectionGate.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            return _connection;
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active for this unit of work.");
        }

        var connection = await GetOpenConnectionAsync();
        _transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        var transaction = _transaction ?? throw new InvalidOperationException("No transaction is active.");
        try
        {
            await transaction.CommitAsync();
        }
        finally
        {
            _transaction = null;
            await transaction.DisposeAsync();
        }
    }

    public async Task RollbackAsync()
    {
        var transaction = _transaction ?? throw new InvalidOperationException("No transaction is active.");
        try
        {
            await transaction.RollbackAsync();
        }
        finally
        {
            _transaction = null;
            await transaction.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connectionGate.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }

            if (_transaction is not null)
            {
                try
                {
                    await _transaction.RollbackAsync();
                }
                finally
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }

            await _connection.DisposeAsync();
            _disposed = true;
        }
        finally
        {
            _connectionGate.Release();
        }
    }


}
