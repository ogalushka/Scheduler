using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Scheduler.Db;
using Scheduler.Db.Models;
using Scheduler.Dtos;
using System.Security.Claims;

namespace Scheduler.Controllers;

[ApiController]
[Route("/")]
public class SchedulerController : ControllerBase
{
    private readonly IRepository<string, ScheduleEntity> scheduleRepository;
    private readonly IRepository<Guid, UserEntity> userRepository;

    public SchedulerController(IRepository<string, ScheduleEntity> scheduleRepository, IRepository<Guid, UserEntity> userRepository)
    {
        this.scheduleRepository = scheduleRepository;
        this.userRepository = userRepository;
    }

    [HttpGet]
    [Route("/schedule")]
    public async Task<ActionResult<Week[]>> GetSchedule()
    {
        var schedule = await scheduleRepository.Get("Tomorrow");

        var attendance = new List<Guid>();
        IReadOnlyCollection<FollowingData> following = Array.Empty<FollowingData>();

        var user = await GetUser();
        if (user != null)
        {
            attendance = user.Attending;
            var followingIds = user.Following.Select(f => f.Id).ToArray();
            var followingUsers = await userRepository.GetAll(userRepository.filter.In(u => u.PublicId, followingIds));

            following = followingUsers.Select(u => new FollowingData(u, GetNameById(u.PublicId))).ToArray();

            string GetNameById(Guid id)
            {
                var match = user.Following.Find(u => u.Id == id);
                return match != null ? match.Name : "";
            }
        }

        var result = schedule.Weeks.Select((w, i) =>
            new Week
            {
                WeekName = w.Name,
                WeekNumber = i,
                Days = w.Days.Select(d =>
                    new Day
                    {
                        Date = d.Date,
                        WeekDay = d.Name,
                        Stages = d.Locations.Select(l => new Location 
                        { 
                            Stage = l.Name,
                            Artists = l.Events.Select(e => BuildEvent(e, attendance, following)).ToArray()
                        }).ToArray()
                    }).ToArray()
            }).ToArray();

        return Ok(result);
    }

    private Event BuildEvent(EventEntity e, List<Guid> attendance, IReadOnlyCollection<FollowingData> followingEntities)
    {
        var followingAttending = followingEntities.Where(f => f.user.Attending.Contains(e.Id));
        return new Event
        {
            Id = e.Id,
            TimeStart = e.Start,
            TimeEnd = e.End,
            Artist = e.Artist,

            Attending = attendance.Contains(e.Id),
            Atendees = followingEntities
                .Where(f => f.user.Attending.Contains(e.Id))
                .Select(f => new Atendee
                {
                    Id = f.user.Id,
                    Name = f.name
                }).ToArray()
        };
    }
    
    [HttpPost]
    [Route("/attend")]
    public async Task<ActionResult> ChangeStatus(Guid eventId, bool attending)
    {
        var userEntity = await GetOrCreateUser();
        var inList = userEntity.Attending.IndexOf(eventId) != -1;
        if (attending)
        {
            if (!inList)
            {
                userEntity.Attending.Add(eventId);
                await userRepository.Update(userEntity);
            }
        }
        else
        {
            if (inList)
            {
                userEntity.Attending.Remove(eventId);
                await userRepository.Update(userEntity);
            }
        }

        return Ok();
    }

    [HttpGet]
    [Route("/secret")]
    public async Task<ActionResult<string>> GetSecret()
    {
        var user = await GetOrCreateUser();
        return Ok(user.Id.ToString());
    }

    [HttpGet]
    [Route("/share")]
    public async Task<ActionResult<string>> GetShare()
    {
        var user = await GetOrCreateUser();
        return Ok(user.PublicId.ToString());
    }

    [HttpGet]
    [Route("/follow")]
    public async Task<IActionResult> Follow(Guid publicId, string name)
    {
        var toFollow = await userRepository.Get(u => u.PublicId == publicId);

        if (toFollow == null)
        {
            return NotFound("Following non existent user"); 
        }

        var user = await GetOrCreateUser();

        var following = user.Following.Any(u => u.Id == publicId);

        if (following)
        {
            return Ok();
        }

        user.Following.Add(new FollowingEntity { Id = publicId, Name = name });
        await userRepository.Update(user);

        return Ok();
    }

    [HttpGet]
    [Route("/unfollow")]
    public async Task<IActionResult> UnFollow(Guid publicId)
    {
        var user = await GetOrCreateUser();
        var removeIndex = user.Following.FindIndex(u => u.Id == publicId);
        if (removeIndex != -1)
        {
            user.Following.RemoveAt(removeIndex);
            await userRepository.Update(user);
        }

        return Ok();
    }

    [HttpPost]
    [Route("/login")]
    public async Task<IActionResult> Login(string secretId)
    {
        var guid = Guid.Parse(secretId);
        var user = await userRepository.Get(guid);

        if (user == null)
        {
            return NotFound();
        }

        await HttpContext.SignInAsync(Convert(guid));

        return Ok();
    }

    private async Task<UserEntity> GetOrCreateUser()
    {
        var userId = GetUserId();
        UserEntity userEntity;
        if (userId != null)
        {
            userEntity = await userRepository.Get(userId.Value);
        }
        else
        {
            userEntity = await CreateUser();
            await HttpContext.SignInAsync(Convert(userEntity.Id));
        }

        return userEntity;
    }

    private async Task<UserEntity?> GetUser()
    {
        var userId = GetUserId();
        if (userId != null)
        {
            var user = await userRepository.Get(userId.Value);
            return user;
        }
        return null;
    }

    private async Task<UserEntity> CreateUser()
    {
        var userEntity = new UserEntity();

        userEntity.PublicId = Guid.NewGuid();
        userEntity.Id = Guid.NewGuid();
        userEntity.Attending = new List<Guid>();
        userEntity.Following = new List<FollowingEntity>();

        await userRepository.Create(userEntity);

        return userEntity;
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

    private Guid? GetUserId()
    {
        var claim = HttpContext.User.Claims.FirstOrDefault(kv => kv.Type == "Id");
        if (claim == null)
        {
            return null;
        }

        return Guid.Parse(claim.Value);
    }

    public static ClaimsPrincipal Convert(Guid id)
    {
        var claims = new List<Claim>()
            {
                new Claim("Id", id.ToString())
            };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}

public record class FollowingData(UserEntity user, string name);