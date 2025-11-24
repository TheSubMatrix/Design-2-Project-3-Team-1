using System;
using UnityEngine;

namespace CustomNamespace.Timers
{
    /// <summary>
    /// Timer that counts up from zero to infinity.  Great for measuring durations.
    /// </summary>
    public class StopwatchTimer : Timer
    {
        public StopwatchTimer(float initialTime) : base(initialTime) { }
        public StopwatchTimer() : base(0){}

        public override void Tick()
        {
            if (!IsRunning) return;
            CurrentTime += GetDeltaTime();
            OnTimerTick.Invoke();
        }

        public override bool IsFinished => false;
        /// <summary>
        /// Adds a callback to be invoked when the timer starts
        /// </summary>
        public new StopwatchTimer OnStart(Action callback)
        {
            base.OnStart(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer stops/completes
        /// </summary>
        public new StopwatchTimer OnComplete(Action callback)
        {
            base.OnComplete(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked on each timer tick
        /// </summary>
        public new StopwatchTimer OnTick(Action callback)
        {
            base.OnTick(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer is paused
        /// </summary>
        public new StopwatchTimer OnPause(Action callback)
        {
            base.OnPause(callback);
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer is resumed
        /// </summary>
        public new StopwatchTimer OnResume(Action callback)
        {
            base.OnResume(callback);
            return this;
        }
        
        /// <summary>
        /// Sets whether this timer uses unscaled time (ignores Time.timeScale)
        /// </summary>
        public new StopwatchTimer SetUseUnscaledTime(bool useUnscaled)
        {
            base.SetUseUnscaledTime(useUnscaled);
            return this;
        }
    }
}