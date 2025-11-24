using System;

namespace CustomNamespace.Timers
{
    /// <summary>
    /// A timer that counts down until its completion
    /// </summary>
    public class CountdownTimer : Timer
    {
        public CountdownTimer(float initialTime) : base(initialTime)
        {
            
        }

        public override void Tick()
        {
            if (IsRunning && CurrentTime > 0)
            {
                CurrentTime -= GetDeltaTime();
                OnTimerTick.Invoke();
            }

            if (IsRunning && CurrentTime <= 0)
            {
                Stop();
            }
        }
        public override bool IsFinished => CurrentTime <= 0;
        /// <summary>
        /// Adds a callback to be invoked when the timer starts
        /// </summary>
        public new CountdownTimer OnStart(Action callback)
        {
            base.OnStart(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer stops/completes
        /// </summary>
        public new CountdownTimer OnComplete(Action callback)
        {
            base.OnComplete(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked on each timer tick
        /// </summary>
        public new CountdownTimer OnTick(Action callback)
        {
            base.OnTick(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer is paused
        /// </summary>
        public new CountdownTimer OnPause(Action callback)
        {
            base.OnPause(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer is resumed
        /// </summary>
        public new CountdownTimer OnResume(Action callback)
        {
            base.OnResume(callback);
            return this;
        }
        
        /// <summary>
        /// Sets whether this timer uses unscaled time (ignores Time.timeScale)
        /// </summary>
        public new CountdownTimer SetUseUnscaledTime(bool useUnscaled)
        {
            base.SetUseUnscaledTime(useUnscaled);
            return this;
        }
        /// <summary>
        /// Sets the countdown time
        /// </summary>
        public CountdownTimer WithTime(float time)
        {
            InitialTime =  time;
            Reset();
            return this;
        }
    }
}