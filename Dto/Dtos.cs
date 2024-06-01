namespace Scheduler.Dtos;

public class Event
{
    public Guid Id { get; set; }

    public DateTime TimeStart { get; set; }
    public DateTime TimeEnd { get; set; }
    public string Artist { get; set; } = "";
    public bool Attending { get; set; }
    public Attendee[] Attendees { get; set; } = Array.Empty<Attendee>();
    public string Day { get; set; } = "";
    public DateOnly Date { get; set; }
    public string Location { get; set; } = "";
    public string Weekend { get; set; } = "";
}

public class Attendee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<Guid> Following { get; set; } = new List<Guid>();
}

public class ScheduleDto
{
    public Attendee? Me { get; set; } = null;
    public Attendee? Owner { get; set; } = null;
    public Event[] Events { get; set; } = Array.Empty<Event>();
}
