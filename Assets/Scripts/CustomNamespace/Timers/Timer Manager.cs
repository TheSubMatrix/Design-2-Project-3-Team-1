using System.Collections.Generic;

namespace CustomNamespace.Timers
{
    public static class TimerManager
    {
        static readonly List<Timer> s_timers = new();
        public static void RegisterTimer(Timer timer) => s_timers.Add(timer);
        public static void DeregisterTimer(Timer timer) => s_timers.Remove(timer);

        public static void UpdateTimers()
        {
            foreach (Timer timer in s_timers)
            {
                timer.Tick();
            }
        }
        public static void Clear() => s_timers.Clear();
    }
}