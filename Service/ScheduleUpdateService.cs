using Scheduler.Db;
using Scheduler.Db.Models;

namespace Scheduler.Service
{
    public class ScheduleUpdaterService
    {
        private readonly IRepository<string, ScheduleHistory> scheduleHistoryRepository;

        public ScheduleUpdaterService(IRepository<string, ScheduleHistory> scheduleHistoryRepository)
        {
            this.scheduleHistoryRepository = scheduleHistoryRepository;
        }

        public async Task<ScheduleEntity> UpdateSchedule(ScheduleEntity schedule)
        {
            var isNewSchedule = false;
            var history = await scheduleHistoryRepository.Get(schedule.Id);
            if (history == null)
            {
                isNewSchedule = true;
                history = new ScheduleHistory(schedule.Id, new List<ScheduleEntity>());
            }
            else
            {
                var lastSchedule = history.Schedules.Last();

                var eventsMatched = 0;
                foreach (var newEvent in schedule.Events)
                {
                    foreach (var previousEvent in lastSchedule.Events)
                    {
                        if (IsSameEvent(newEvent, previousEvent))
                        {
                            eventsMatched++;
                            newEvent.Id = previousEvent.Id;
                            break;
                        }
                        else
                        {
                            newEvent.Id = Guid.NewGuid();
                        }
                    }
                }

                Console.WriteLine($"matched e {eventsMatched}, lastSched {lastSchedule.Events.Length}, new {schedule.Events.Length}");

                if (eventsMatched == lastSchedule.Events.Length && lastSchedule.Events.Length == schedule.Events.Length)
                {
                    return lastSchedule;
                }
            }

            schedule.UpdateTime = DateTime.UtcNow;
            history.Schedules.Add(schedule);

            if (isNewSchedule)
            {
                foreach (var e in schedule.Events)
                {
                    e.Id = Guid.NewGuid();
                }
                await scheduleHistoryRepository.Create(history);
            }
            else
            {
                await scheduleHistoryRepository.Update(history);
            }

            return schedule;
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

    }
}
