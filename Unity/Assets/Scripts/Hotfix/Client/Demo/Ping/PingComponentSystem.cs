using System;

namespace ET.Client
{
    [EntitySystem]
    public class PingComponentAwakeSystem: AwakeSystem<PingComponent>
    {
        protected override void Awake(PingComponent self)
        {
            PingAsync(self).Coroutine();
        }

        private static async ETTask PingAsync(PingComponent self)
        {
            Session session = self.GetParent<Session>();
            long instanceId = self.InstanceId;
            
            while (true)
            {
                if (self.InstanceId != instanceId)
                {
                    return;
                }

                long time1 = TimeHelper.ClientNow();
                try
                {
                    C2G_Ping c2GPing = NetServices.Instance.FetchMessage<C2G_Ping>();
                    G2C_Ping response = await session.Call(c2GPing) as G2C_Ping;

                    if (self.InstanceId != instanceId)
                    {
                        return;
                    }

                    long time2 = TimeHelper.ClientNow();
                    self.Ping = time2 - time1;
                    
                    TimeInfo.Instance.ServerMinusClientTime = response.Time + (time2 - time1) / 2 - time2;

                    NetServices.Instance.RecycleMessage(response);
                    
                    await TimerComponent.Instance.WaitAsync(2000);
                }
                catch (RpcException e)
                {
                    // session断开导致ping rpc报错，记录一下即可，不需要打成error
                    Log.Info($"ping error: {self.Id} {e.Error}");
                    return;
                }
                catch (Exception e)
                {
                    Log.Error($"ping error: \n{e}");
                }
            }
        }
    }

    [EntitySystem]
    public class PingComponentDestroySystem: DestroySystem<PingComponent>
    {
        protected override void Destroy(PingComponent self)
        {
            self.Ping = default;
        }
    }
}