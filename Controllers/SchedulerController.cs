using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Scheduler.Db;
using Scheduler.Db.Models;
using Scheduler.Dtos;
using Scheduler.Tracking;
using System.Security.Claims;

namespace Scheduler.Controllers;

[ApiController]
[Route("/")]
public class SchedulerController : ControllerBase
{
    private readonly IRepository<string, ScheduleEntity> scheduleRepository;
    private readonly IRepository<Guid, UserEntity> userRepository;
    private readonly TrackingClient tracking;

    public SchedulerController(
        IRepository<string, ScheduleEntity> scheduleRepository,
        IRepository<Guid, UserEntity> userRepository,
        TrackingClient tracking)
    {
        this.scheduleRepository = scheduleRepository;
        this.userRepository = userRepository;
        this.tracking = tracking;
    }

    [HttpGet]
    [Route("/schedule")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule()
    {
        var schedule = await scheduleRepository.Get("Tomorrow");
        var attendance = new List<Guid>();
        IReadOnlyCollection<UserEntity> following = Array.Empty<UserEntity>();

        var user = await GetLoggedInUser();
        if (user.SavedUser)
        {
            attendance = user.Attending;
            following = await userRepository.GetAll(userRepository.filter.In(u => u.PublicId, user.Following));
        }

        _ = tracking.Track(EventName.ScheduleViewed, user.Id);
        var result = BuildScheduleResponce(schedule, user, user, attendance, following);
        return Ok(result);
    }

    [HttpGet]
    [Route("/schedule/{publicId}")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(string publicId)
    {
        var schedule = await scheduleRepository.Get("Tomorrow");
        var otherUser = await userRepository.Get(e => e.PublicId == Guid.Parse(publicId));
        if (otherUser == null)
        {
            return NotFound();
        }
        var attendance = otherUser.Attending;

        var user = await GetLoggedInUser();
        var following = new UserEntity[] { user };
        var result = BuildScheduleResponce(schedule, user, otherUser, attendance, following);
        _ = tracking.Track(EventName.OtherScheduleViewed, user.Id, new Dictionary<string, object> { { TrackingClient.OtherUserId, otherUser.Id } });
        return Ok(result);
    }

    private ScheduleDto BuildScheduleResponce(ScheduleEntity schedule, UserEntity me, UserEntity owner, IReadOnlyCollection<Guid> attendance, IReadOnlyCollection<UserEntity> following)
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

        var meDto = me.SavedUser ? new Attendee { Id = me.PublicId, Name = me.Name } : null;
        var ownerDto = owner.SavedUser ? new Attendee { Id = owner.PublicId, Name = owner.Name } : null;

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
        var userEntity = await GetAndSaveUser();

        var inList = userEntity.Attending.IndexOf(eventId) != -1;
        if (attending)
        {
            if (!inList)
            {
                userEntity.Attending.Add(eventId);
                await userRepository.Update(userEntity);
                _ = tracking.Track(EventName.Attending, userEntity.Id, new Dictionary<string, object> { { TrackingClient.EventId, eventId } });
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
        var user = await GetLoggedInUser();
        return Ok(user.Id.ToString());
    }

    [HttpPost]
    [Route("/name")]
    public async Task<ActionResult<Attendee>> GetShare(string name)
    {
        var user = await GetAndSaveUser();
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

        var user = await GetAndSaveUser();

        if (user.PublicId == publicId)
        {
            return Problem("Can't follow yourself");
        }

        var following = user.Following.Contains(publicId);

        if (following)
        {
            return Ok();
        }

        user.Following.Add(publicId);
        await userRepository.Update(user);
        _ = tracking.Track(EventName.Followed, user.Id, new Dictionary<string, object> { { TrackingClient.OtherUserId, toFollow.Id } });

        return Ok();
    }

    [HttpPost]
    [Route("/unfollow")]
    public async Task<IActionResult> UnFollow(Guid publicId)
    {
        var user = await GetAndSaveUser();
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
        if (!Guid.TryParse(secretId, out var guid))
        {
            return NotFound("SecretId is an invalid guid");
        }

        var user = CreateUser(guid);

        await HttpContext.SignInAsync(Convert(user));

        return Ok();
    }

    private async Task<UserEntity> GetAndSaveUser()
    {
        var user = await GetLoggedInUser();
        if (!user.SavedUser)
        {
            await SaveUser(user);
        }

        return user;
    }

    private async Task<UserEntity> GetLoggedInUser()
    {
        var userId = GetLoggedInUserId();
        if (userId == null)
        {
            var newUser = CreateUser();
            await HttpContext.SignInAsync(Convert(newUser));
            return newUser;
        }

        var user = await userRepository.Get(userId.Value);
        if (user == null)
        {
            user = CreateUser(userId);
        }
        else
        {
            user.SavedUser = true;
        }

        return user;
    }

    private UserEntity CreateUser(Guid? userId = null)
    {
        var userEntity = new UserEntity();

        userEntity.PublicId = Guid.NewGuid();
        userEntity.Id = userId == null ? Guid.NewGuid() : userId.Value;
        userEntity.Attending = new List<Guid>();
        userEntity.Following = new List<Guid>();
        userEntity.SavedUser = false;

        return userEntity;
    }

    private async Task<UserEntity> SaveUser(UserEntity userEntity)
    {
        if (userEntity.PublicId == Guid.Empty)
        {
            userEntity.PublicId = Guid.NewGuid();
        }

        await userRepository.Create(userEntity);
        userEntity.SavedUser = true;

        return userEntity;
    }

    private Guid? GetLoggedInUserId()
    {
        var claim = HttpContext.User.Claims.FirstOrDefault(kv => kv.Type == "Id");
        if (claim == null)
        {
            return null;
        }

        if (!Guid.TryParse(claim.Value, out var id))
        {
            return null;
        }

        return id;
    }

    public static ClaimsPrincipal Convert(UserEntity user)
    {
        var claims = new List<Claim>()
            {
                new Claim("Id", user.Id.ToString())
            };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
