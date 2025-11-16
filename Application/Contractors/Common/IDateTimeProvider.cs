namespace Application.Contractors.Common
{
    public interface IDateTimeProvider
    {
        public DateTime UtcNow { get; }
    }
}
