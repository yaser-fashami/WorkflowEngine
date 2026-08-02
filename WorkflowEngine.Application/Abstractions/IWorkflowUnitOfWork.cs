public interface IWorkflowUnitOfWork
{
    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
