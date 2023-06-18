namespace Scheduler.Db
{
    public interface IEntity<Key>
    {
        Key Id { get; set; }
    }
}
