using System;

namespace CustomNamespace.Timers
{
    /// <summary>
    /// Timer that ticks at a specific frequency. (N times per second)
    /// </summary>
    public class FrequencyTimer : Timer 
    {
        public uint TicksPerSecond { get; private set; }
        float m_timeThreshold;

        public FrequencyTimer(uint ticksPerSecond) : base(0) 
        {
            CalculateTimeThreshold(ticksPerSecond);
        }

        public override void Tick() 
        {
            if (!IsRunning) return;
            CurrentTime += GetDeltaTime();
            while (CurrentTime >= m_timeThreshold) 
            {
                CurrentTime -= m_timeThreshold;
                OnTimerTick.Invoke();
            }
        }

        public override bool IsFinished => !IsRunning;

        public override void Reset() 
        {
            base.Reset();
            CurrentTime = 0;
        }

        public void Reset(uint newTicksPerSecond) 
        {
            CalculateTimeThreshold(newTicksPerSecond);
            Reset();
        }

        void CalculateTimeThreshold(uint ticksPerSecond) 
        {
            TicksPerSecond = ticksPerSecond;
            m_timeThreshold = 1f / TicksPerSecond;
        }
        /// <summary>
        /// Adds a callback to be invoked when the timer starts
        /// </summary>
        public new FrequencyTimer OnStart(Action callback)
        {
            base.OnStart(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer stops/completes
        /// </summary>
        public new FrequencyTimer OnComplete(Action callback)
        {
            base.OnComplete(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked on each timer tick
        /// </summary>
        public new FrequencyTimer OnTick(Action callback)
        {
            base.OnTick(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer is paused
        /// </summary>
        public new FrequencyTimer OnPause(Action callback)
        {
            base.OnPause(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer is resumed
        /// </summary>
        public new FrequencyTimer OnResume(Action callback)
        {
            base.OnResume(callback);
            return this;
        }
        
        /// <summary>
        /// Sets whether this timer uses unscaled time (ignores Time.timeScale)
        /// </summary>
        public new FrequencyTimer SetUseUnscaledTime(bool useUnscaled)
        {
            base.SetUseUnscaledTime(useUnscaled);
            return this;
        }
        /// <summary>
        /// Sets the ticks per second of the timer
        /// </summary>
        public FrequencyTimer WithTicksPerSecond(uint ticksPerSecond)
        {
            Reset(ticksPerSecond);
            return this;
        }
    }
}