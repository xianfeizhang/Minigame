using System;

namespace ET
{
    [FriendOf(typeof(SessionIdleCheckerComponent))]
    public static class SessionIdleCheckerComponentSystem
    {
        [Invoke(TimerInvokeType.SessionIdleChecker)]
        public class SessionIdleChecker: ATimer<SessionIdleCheckerComponent>
        {
            protected override void Run(SessionIdleCheckerComponent self)
            {
                try
                {
                    self.Check();
                }
                catch (Exception e)
                {
                    Log.Error($"move timer error: {self.Id}\n{e}");
                }
            }
        }
    
        [EntitySystem]
        public class SessionIdleCheckerComponentAwakeSystem: AwakeSystem<SessionIdleCheckerComponent>
        {
            protected override void Awake(SessionIdleCheckerComponent self)
            {
                self.Awake();
            }
        }

        [EntitySystem]
        public class SessionIdleCheckerComponentDestroySystem: DestroySystem<SessionIdleCheckerComponent>
        {
            protected override void Destroy(SessionIdleCheckerComponent self)
            {
                self.Destroy();
            }
        }
        
        private static void Awake(this SessionIdleCheckerComponent self)
        {
            self.RepeatedTimer = TimerComponent.Instance.NewRepeatedTimer(SessionIdleCheckerComponentSystem.CheckInteral, TimerInvokeType.SessionIdleChecker, self);
        }
        
        private static void Destroy(this SessionIdleCheckerComponent self)
        {
            TimerComponent.Instance?.Remove(ref self.RepeatedTimer);
        }
        
        public const int CheckInteral = 2000;

        private static void Check(this SessionIdleCheckerComponent self)
        {
            Session session = self.GetParent<Session>();
            long timeNow = TimeHelper.ClientNow();

            if (timeNow - session.LastRecvTime < ConstValue.SessionTimeoutTime && timeNow - session.LastSendTime < ConstValue.SessionTimeoutTime)
            {
                return;
            }

            Log.Info($"session timeout: {session.Id} {timeNow} {session.LastRecvTime} {session.LastSendTime} {timeNow - session.LastRecvTime} {timeNow - session.LastSendTime}");
            session.Error = ErrorCore.ERR_SessionSendOrRecvTimeout;

            session.Dispose();
        }
    }
}