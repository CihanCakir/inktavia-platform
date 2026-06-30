using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

public sealed class InvoiceNumberSequenceRepository : IInvoiceNumberSequenceRepository
{
    private readonly PaymentDbContext _db;
    public InvoiceNumberSequenceRepository(PaymentDbContext db) => _db = db;

    public Task<InvoiceNumberSequenceEntity?> GetAsync(
        string prefix, int year, int month, CancellationToken ct = default)
        => _db.InvoiceNumberSequences
            .FirstOrDefaultAsync(
                x => x.Prefix == prefix && x.Year == year && x.Month == month,
                ct);

    public Task AddAsync(InvoiceNumberSequenceEntity sequence, CancellationToken ct = default)
        => _db.InvoiceNumberSequences.AddAsync(sequence, ct).AsTask();

    public void Update(InvoiceNumberSequenceEntity sequence)
        => _db.InvoiceNumberSequences.Update(sequence);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
