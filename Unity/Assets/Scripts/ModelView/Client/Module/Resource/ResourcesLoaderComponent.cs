using System.Collections.Generic;

namespace ET.Client
{
    [FriendOf(typeof(ResourcesLoaderComponent))]
    public static class ResourcesLoaderComponentSystem
    {
        [EntitySystem]
            public class ResourcesLoaderComponentDestroySystem: DestroySystem<ResourcesLoaderComponent>
            {
                protected override void Destroy(ResourcesLoaderComponent self)
                {
                    async ETTask UnLoadAsync()
                    {
                        using (ListComponent<string> list = ListComponent<string>.Create())
                        {
                            list.AddRange(self.LoadedResource);
                            self.LoadedResource = null;
        
                            if (TimerComponent.Instance == null)
                            {
                                return;
                            }
                            
                            // 延迟5秒卸载包，因为包卸载是引用计数，5秒之内假如重新有逻辑加载了这个包，那么可以避免一次卸载跟加载
                            await TimerComponent.Instance.WaitAsync(5000);
        
                            foreach (string abName in list)
                            {
                                using CoroutineLock coroutineLock =
                                        await CoroutineLockComponent.Instance.Wait(CoroutineLockType.ResourcesLoader, abName.GetHashCode(), 0);
                                {
                                    if (ResourcesComponent.Instance == null)
                                    {
                                        return;
                                    }
        
                                    await ResourcesComponent.Instance.UnloadBundleAsync(abName);
                                }
                            }
                        }
                    }
        
                    UnLoadAsync().Coroutine();
                }
            }
        
        public static async ETTask LoadAsync(this ResourcesLoaderComponent self, string ab)
        {
            using CoroutineLock coroutineLock = await CoroutineLockComponent.Instance.Wait(CoroutineLockType.ResourcesLoader, ab.GetHashCode(), 0);
                    
            if (self.IsDisposed)
            {
                Log.Error($"resourceload already disposed {ab}");
                return;
            }

            if (self.LoadedResource.Contains(ab))
            {
                return;
            }

            self.LoadedResource.Add(ab);
            await ResourcesComponent.Instance.LoadBundleAsync(ab);
        }
    }
    
    [ComponentOf(typeof(Scene))]
    public class ResourcesLoaderComponent: Entity, IAwake, IDestroy
    {
        public HashSet<string> LoadedResource = new HashSet<string>();
    }
}