using Microsoft.AspNetCore.Mvc;
using Scheduler.Db;
using Scheduler.Db.Models;

namespace Scheduler.Controllers;

[ApiController]
[Route("/config")]
public class ConfigController : ControllerBase
{
    private readonly IRepository<string, ScheduleEntity> scheduleRepository;

    public ConfigController(IRepository<string, ScheduleEntity> scheduleRepository)
    {
        this.scheduleRepository = scheduleRepository;
    }

    [HttpGet]
    [Route("/")]
    public async Task<ActionResult<IReadOnlyCollection<ScheduleEntity>>> GetAll()
    {
        var allSchedules = await scheduleRepository.GetAll();
        return Ok(allSchedules);
    }

    [HttpPost]
    [Route("/")]
    public async Task<ActionResult<ScheduleEntity>> AddSchedule([FromBody] ScheduleEntity schedule)
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

    [HttpPut]
    [Route("/")]
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

    [HttpDelete]
    [Route("/")]
    public async Task<ActionResult<ScheduleEntity>> DeleteSchedule(string scheduleId)
    {
        var schedule = await scheduleRepository.Get(scheduleId);
        if (schedule == null)
        {
            return BadRequest($"Schedule {scheduleId} does not exists");
        }

        await scheduleRepository.Remove(scheduleId);

        return Ok(schedule);
    }

    [HttpPost]
    [Route("/seed")]
    public async Task<ActionResult> Seed()
    {
        await scheduleRepository.Remove("Tomorrow");

        var schedule = new ScheduleEntity("Tomorrow", new WeekEntity[] {
            new WeekEntity("Week 1", new DayEntity[] {
                new DayEntity(new DateOnly(2023, 6, 18), "Sunday", new LocationEntity[] {
                    new LocationEntity("Stage 1", new EventEntity[] {
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 18, 16, 00, 00), new DateTime(2023, 6, 18, 17, 0, 0), "Dude 1"),
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 18, 17, 00, 00), new DateTime(2023, 6, 18, 18, 0, 0), "Dude 2")
                    }),
                    new LocationEntity("Stage 2", new EventEntity[] {
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 18, 16, 00, 00), new DateTime(2023, 6, 18, 18, 0, 0), "Dude 3"),
                    })
                }),
                new DayEntity(new DateOnly(2023, 6, 19), "Monday", new LocationEntity[] {
                    new LocationEntity("Stage 1", new EventEntity[] {
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 19, 16, 00, 00), new DateTime(2023, 6, 19, 17, 0, 0), "Dude 3"),
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 19, 17, 00, 00), new DateTime(2023, 6, 19, 18, 0, 0), "Dude 2"),
                    }),
                    new LocationEntity("Stage 2", new EventEntity[] {
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 19, 16, 00, 00), new DateTime(2023, 6, 19, 18, 0, 0), "Dude 1"),
                    }),
                }),
            }),
            new WeekEntity("Week 2", new DayEntity[] {
                new DayEntity(new DateOnly(2023, 6, 25), "Sunday", new LocationEntity[] {
                    new LocationEntity("Stage 1", new EventEntity[] {
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 25, 16, 00, 00), new DateTime(2023, 6, 25, 17, 0, 0), "Dude 1"),
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 25, 17, 00, 00), new DateTime(2023, 6, 25, 18, 0, 0), "Dude 2")
                    }),
                    new LocationEntity("Stage 2", new EventEntity[] {
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 25, 16, 00, 00), new DateTime(2023, 6, 25, 18, 0, 0), "Dude 3"),
                    })
                }),
                new DayEntity(new DateOnly(2023, 6, 26), "Monday", new LocationEntity[] {
                    new LocationEntity("Stage 1", new EventEntity[] {
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 26, 16, 00, 00), new DateTime(2023, 6, 26, 17, 0, 0), "Dude 3"),
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 26, 17, 00, 00), new DateTime(2023, 6, 26, 18, 0, 0), "Dude 2"),
                    }),
                    new LocationEntity("Stage 2", new EventEntity[] {
                        new EventEntity(Guid.NewGuid(), new DateTime(2023, 6, 26, 16, 00, 00), new DateTime(2023, 6, 26, 18, 0, 0), "Dude 1"),
                    }),
                }),
            }),
        });

        await scheduleRepository.Create(schedule);

        return Ok();
    }

    private void FillInEmptyIds(ScheduleEntity schedule)
    {
        foreach (var week in schedule.Weeks)
        {
            foreach (var day in week.Days)
            {
                foreach (var location in day.Locations)
                {
                    foreach (var e in location.Events)
                    {
                        if (e.Id == Guid.Empty)
                        {
                            e.Id = Guid.NewGuid();
                        }
                    }
                }
            }
        }
    }
}