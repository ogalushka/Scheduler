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
    public async Task<ActionResult<ScheduleDto>> GetSchedule()
    {
        var schedule = await scheduleRepository.Get("Tomorrow");
        var attendance = new List<Guid>();
        IReadOnlyCollection<UserEntity> following = Array.Empty<UserEntity>();

        var user = await GetUser();
        if (user != null)
        {
            attendance = user.Attending;
            following = await userRepository.GetAll(userRepository.filter.In(u => u.PublicId, user.Following));
        }
        var result = BuildScheduleResponce(schedule, user, user, attendance, following);
        return Ok(result);
    }

    [HttpGet]
    [Route("/schedule/{publicId}")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(string publicId)
    {
        var schedule = await scheduleRepository.Get("Tomorrow");
        var following = Array.Empty<UserEntity>();
        var otherUser = await userRepository.Get(Guid.Parse(publicId));
        if (otherUser == null)
        {
            return NotFound();
        }
        var attendance = otherUser.Attending;

        var user = await GetUser();
        if (user != null)
        {
            following = new UserEntity[] { user };
        }
        var result = BuildScheduleResponce(schedule, user, otherUser, attendance, following);
        return Ok(result);
    }

    private ScheduleDto BuildScheduleResponce(ScheduleEntity schedule, UserEntity? me, UserEntity? owner, IReadOnlyCollection<Guid> attendance, IReadOnlyCollection<UserEntity> following)
    {
        var weeks = schedule.Weeks.Select((w, i) =>
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

        var meDto = me == null ? null : new Attendee { Id = me.PublicId, Name = me.Name };
        var ownerDto = owner == null ? null : new Attendee { Id = owner.PublicId, Name = owner.Name };

        return new ScheduleDto
        {
            Me = meDto,
            Owner = ownerDto,
            Schedule = weeks
        };
    }

    private Event BuildEvent(EventEntity e, IReadOnlyCollection<Guid> attendance, IReadOnlyCollection<UserEntity> followingUsers)
    {
        return new Event
        {
            Id = e.Id,
            TimeStart = e.Start,
            TimeEnd = e.End,
            Artist = e.Artist,

            Attending = attendance.Contains(e.Id),
            Attendees = followingUsers
                .Where(u => u.Attending.Contains(e.Id))
                .Select(u => new Attendee
                {
                    Id = u.Id,
                    Name = u.Name
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

    [HttpPost]
    [Route("/name")]
    public async Task<ActionResult<Attendee>> GetShare(string name)
    {
        var user = await GetOrCreateUser();
        if (user.Name != name)
        {
            user.Name = name;
            await userRepository.Update(user);
        }
        return Ok(new Attendee { Id = user.PublicId, Name = user.Name });
    }

    [HttpPost]
    [Route("/follow")]
    public async Task<IActionResult> Follow(Guid publicId)
    {
        var toFollow = await userRepository.Get(u => u.PublicId == publicId);

        if (toFollow == null)
        {
            return NotFound("Following non existent user"); 
        }

        var user = await GetOrCreateUser();

        var following = user.Following.Contains(publicId);

        if (following)
        {
            return Ok();
        }

        user.Following.Add(publicId);
        await userRepository.Update(user);

        return Ok();
    }

    [HttpPost]
    [Route("/unfollow")]
    public async Task<IActionResult> UnFollow(Guid publicId)
    {
        var user = await GetOrCreateUser();
        var removeIndex = user.Following.IndexOf(publicId);
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
        UserEntity? userEntity = null;
        if (userId != null)
        {
            userEntity = await userRepository.Get(userId.Value);
        }

        if (userEntity == null)
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
        userEntity.Following = new List<Guid>();

        await userRepository.Create(userEntity);

        return userEntity;
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
