using System;
using UnityEngine;

namespace CustomNamespace.Timers
{
    /// <summary>
    /// Countdown timer that fires an event every interval until completion.
    /// </summary>
    public class IntervalTimer : Timer 
    {
        float m_interval;
        float m_nextInterval;

        public Action OnTimerInterval = delegate { };

        public IntervalTimer(float totalTime, float intervalSeconds) : base(totalTime) {
            m_interval = intervalSeconds;
            m_nextInterval = totalTime - m_interval;
        }

        public override void Tick() {
            if (IsRunning && CurrentTime > 0) {
                CurrentTime -= GetDeltaTime();
                OnTimerTick.Invoke();
                while (CurrentTime <= m_nextInterval && m_nextInterval >= 0) {
                    OnTimerInterval.Invoke();
                    m_nextInterval -= m_interval;
                }
            }
            if (!IsRunning || !(CurrentTime <= 0)) return;
            CurrentTime = 0;
            Stop();
        }
        public override bool IsFinished => CurrentTime <= 0;
        
        /// <summary>
        /// Adds a callback to be invoked when the timer starts
        /// </summary>
        public new IntervalTimer OnStart(Action callback)
        {
            base.OnStart(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer stops/completes
        /// </summary>
        public new IntervalTimer OnComplete(Action callback)
        {
            base.OnComplete(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked on each timer tick
        /// </summary>
        public new IntervalTimer OnTick(Action callback)
        {
            base.OnTick(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer is paused
        /// </summary>
        public new IntervalTimer OnPause(Action callback)
        {
            base.OnPause(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer is resumed
        /// </summary>
        public new IntervalTimer OnResume(Action callback)
        {
            base.OnResume(callback);
            return this;
        }
        
        /// <summary>
        /// Sets whether this timer uses unscaled time (ignores Time.timeScale)
        /// </summary>
        public new IntervalTimer SetUseUnscaledTime(bool useUnscaled)
        {
            base.SetUseUnscaledTime(useUnscaled);
            return this;
        }
        
        /// <summary>
        /// Sets the action that occurs when the timer reaches its interval
        /// </summary>
        public IntervalTimer OnInterval(Action callback)
        {
            OnTimerInterval += callback;
            return this;
        }
        /// <summary>
        /// Sets the interval of the timer
        /// </summary>
        public IntervalTimer WithInterval(float intervalSeconds)
        {
            m_interval =  intervalSeconds;
            m_nextInterval = CurrentTime - m_interval;
            return this;
        }
    }
}