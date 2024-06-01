using Microsoft.AspNetCore.Mvc;
using Scheduler.Db;
using Scheduler.Db.Models;
using Scheduler.Service;

namespace Scheduler.Controllers;

[ApiController]
[Route("/config")]
public class ConfigController : ControllerBase
{
    private readonly IRepository<string, ScheduleHistory> scheduleHistoryRepository;
    private readonly ScheduleUpdaterService scheduleUpdaterService;

    public ConfigController(
        IRepository<string, ScheduleHistory> scheduleHistoryRepository,
        ScheduleUpdaterService scheduleUpdaterService)
    {
        this.scheduleHistoryRepository = scheduleHistoryRepository;
        this.scheduleUpdaterService = scheduleUpdaterService;
    }

    [HttpGet]
    [Route("/config/active")]
    public async Task<ActionResult<IReadOnlyCollection<ScheduleEntity>>> GetAll()
    {
        var allSchedules = await scheduleHistoryRepository.GetAll();
        return Ok(allSchedules.Select(s => s.Schedules.Last()));
    }

    [HttpGet]
    [Route("/config/history/all")]
    public async Task<ActionResult<IReadOnlyCollection<ScheduleHistory>>> GetAllHistory()
    {
        var allSchedules = await scheduleHistoryRepository.GetAll();
        return Ok(allSchedules);
    }

    [HttpPost]
    [Route("/config/update")]
    public async Task<ActionResult<ScheduleEntity>> UpdateSchedule([FromBody] ScheduleEntity schedule)
    {
        try
        {
            var updated = await scheduleUpdaterService.UpdateSchedule(schedule);
            return Ok(updated);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    [Route("/config/history")]
    public async Task<ActionResult<ScheduleHistory>> GetHistory(string id)
    {
        var history = await scheduleHistoryRepository.Get(id);
        if (history == null)
        {
            return BadRequest($"Schedule {id} does not exists");
        }
        Console.WriteLine($"history length = {history.Schedules.Count}");
        return Ok(history);
    }


    /*
    [HttpPost]
    [Route("/flatconfig")]
    public async Task<ActionResult<ScheduleEntity>> AddScheduleFlat([FromBody] ScheduleEntity schedule)
    {
        var exists = await scheduleRepository.Get(schedule.Id);
        if (exists != null)
        {
            return BadRequest($"Schedule {schedule.Id} allready exists");
        }

        FillInEmptyIds(schedule);

        await scheduleRepository.Create(schedule);

        return Ok(schedule);
    }
    */

    /*
    [HttpPut]
    [Route("/config")]
    public async Task<ActionResult<ScheduleEntity>> UpdateSchedule([FromBody] ScheduleEntity schedule)
    {
        var exists = await scheduleRepository.Get(schedule.Id);
        if (exists == null)
        {
            return BadRequest($"Schedule {schedule.Id} does not exist");
        }

        FillInEmptyIds(schedule);

        await scheduleRepository.Update(schedule);

        return Ok(schedule);
    }
    */

    [HttpDelete]
    [Route("/config")]
    public async Task<ActionResult<ScheduleEntity>> DeleteSchedule(string scheduleId)
    {
        var scheduleHistory = await scheduleHistoryRepository.Get(scheduleId);

        if (scheduleHistory == null)
        {
            return BadRequest($"Schedule {scheduleId} history does not exists");
        }

        await scheduleHistoryRepository.Remove(scheduleId);

        return Ok(scheduleHistory);
    }

    [HttpPost]
    [Route("/seed")]
    public async Task<ActionResult> Seed()
    {
        await scheduleHistoryRepository.Remove("Tomorrow");

        var schedule = new ScheduleEntity("Tomorrow", new EventEntity[] {
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 18, 16, 00, 00), new DateTime(2023, 6, 18, 17, 0, 0), "Dude 1", "Week 1", "Sunday", new DateOnly(2023, 6, 18), "Stage 1"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 18, 17, 00, 00), new DateTime(2023, 6, 18, 18, 0, 0), "Dude 2", "Week 1", "Sunday", new DateOnly(2023, 6, 18), "Stage 1"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 18, 16, 00, 00), new DateTime(2023, 6, 18, 18, 0, 0), "Dude 3", "Week 1", "Sunday", new DateOnly(2023, 6, 18), "Stage 2"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 19, 16, 00, 00), new DateTime(2023, 6, 19, 17, 0, 0), "Dude 3", "Week 1", "Monday", new DateOnly(2023, 6, 19), "Stage 1"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 19, 17, 00, 00), new DateTime(2023, 6, 19, 18, 0, 0), "Dude 2", "Week 1", "Monday", new DateOnly(2023, 6, 19), "Stage 1"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 19, 16, 00, 00), new DateTime(2023, 6, 19, 18, 0, 0), "Dude 1", "Week 1", "Monday", new DateOnly(2023, 6, 19), "Stage 2"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 25, 16, 00, 00), new DateTime(2023, 6, 25, 17, 0, 0), "Dude 1", "Week 2", "Sunday", new DateOnly(2023, 6, 25), "Stage 1"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 25, 17, 00, 00), new DateTime(2023, 6, 25, 18, 0, 0), "Dude 2", "Week 2", "Sunday", new DateOnly(2023, 6, 25), "Stage 1"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 25, 16, 00, 00), new DateTime(2023, 6, 25, 18, 0, 0), "Dude 3", "Week 2", "Sunday", new DateOnly(2023, 6, 25), "Stage 2"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 26, 16, 00, 00), new DateTime(2023, 6, 26, 17, 0, 0), "Dude 3", "Week 2", "Monday", new DateOnly(2023, 6, 26), "Stage 1"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 26, 17, 00, 00), new DateTime(2023, 6, 26, 18, 0, 0), "Dude 2", "Week 2", "Monday", new DateOnly(2023, 6, 26), "Stage 1"),
            new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 26, 16, 00, 00), new DateTime(2023, 6, 26, 18, 0, 0), "Dude 1", "Week 2", "Monday", new DateOnly(2023, 6, 26), "Stage 2"),
        }, 
        DateTime.UtcNow);

        await scheduleHistoryRepository.Create(new ScheduleHistory("Tomorrow", new ScheduleEntity[] { schedule }.ToList()));

        return Ok();
    }

    private bool IsSameEvent(EventEntity e1, EventEntity e2)
    {
        return e1.Date == e2.Date
            && e1.Artist == e2.Artist
            && e1.Day == e2.Day
            && e1.End == e2.End
            && e1.Location == e2.Location
            && e1.Start == e2.Start
            && e1.Weekend == e2.Weekend;

    }

    private void FillInEmptyIds(ScheduleEntity schedule)
    {
        foreach (var e in schedule.Events)
        {
            if (e.Id == Guid.Empty)
            {
                e.Id = Guid.NewGuid();
            }
        }
    }
}