    using System;

    public interface IPlayerMovementEventProvider
    {
        public delegate void OnJump();
        public delegate void OnMove();
        public event OnMove OnMoveEvent;
        public event OnJump OnJumpEvent;

    }
