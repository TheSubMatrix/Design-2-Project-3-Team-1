namespace CustomNamespace.Timers
{
    using System;
    using UnityEngine;
    public abstract class Timer : IDisposable
    {
        bool m_disposed;
        public float CurrentTime { get; protected set; }
        public bool IsRunning { get; private set; }
        protected float InitialTime;
        public float Progress => Mathf.Clamp01(CurrentTime / InitialTime);
        public bool UseUnscaledTime { get; set; }

        public Action OnTimerStart = delegate { };
        public Action OnTimerStop = delegate { };
        public Action OnTimerPause = delegate { };
        public Action OnTimerResume = delegate { };
        public Action OnTimerTick = delegate { };
        
        protected Timer(float initialTime)
        {
            InitialTime = initialTime;
        }

        public void Start()
        {
            CurrentTime = InitialTime;
            if (IsRunning) return;
            IsRunning = true;
            TimerManager.RegisterTimer(this);
            OnTimerStart.Invoke();
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            TimerManager.DeregisterTimer(this);
            OnTimerStop.Invoke();
        }
        
        public abstract void Tick();
        
        public abstract bool IsFinished { get; }
        
        public void Resume()
        {
            IsRunning = true;
            OnTimerResume.Invoke();
        }

        public void Pause()
        {
            IsRunning = false;
            OnTimerPause.Invoke();
        }

        public virtual void Reset() => CurrentTime = InitialTime;
        
        public virtual void Reset(float newTime)
        {
            InitialTime = newTime;
            Reset();
        }
        
        /// <summary>
        /// Gets the appropriate delta time based on UseUnscaledTime and TimeScale settings
        /// </summary>
        protected float GetDeltaTime()
        {
            return UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer starts
        /// </summary>
        public Timer OnStart(Action callback)
        {
            OnTimerStart += callback;
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer stops/completes
        /// </summary>
        public Timer OnComplete(Action callback)
        {
            OnTimerStop += callback;
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked on each timer tick
        /// </summary>
        public Timer OnTick(Action callback)
        {
            OnTimerTick += callback;
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer is paused
        /// </summary>
        public Timer OnPause(Action callback)
        {
            OnTimerPause += callback;
            return this;
        }
        
        /// <summary>
        /// Adds a callback to be invoked when the timer is resumed
        /// </summary>
        public Timer OnResume(Action callback)
        {
            OnTimerResume += callback;
            return this;
        }
        
        /// <summary>
        /// Sets whether this timer uses unscaled time (ignores Time.timeScale)
        /// </summary>
        public Timer SetUseUnscaledTime(bool useUnscaled)
        {
            UseUnscaledTime = useUnscaled;
            return this;
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if(m_disposed) return;
            if (disposing)
            {
                TimerManager.DeregisterTimer(this);
            }
            m_disposed = true;
        }
        
        ~Timer()
        {
            Dispose(false);
        }
    }
}