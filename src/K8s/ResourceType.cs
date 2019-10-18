using System;
using System.Collections.Generic;
using System.Linq;

namespace AzSignalR.Monitor.Storage
{
    public enum ResourceType
    {
        VM,
        Service,
        Deployment,
        Pod,
        SignalR
    }

    public static class ResourceTypeExtensions
    {
        public static bool IsPerACS(this ResourceType resType)
        {
            switch (resType)
            {
                case ResourceType.VM:
                case ResourceType.Service:
                case ResourceType.Deployment:
                case ResourceType.Pod:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPerRegion(this ResourceType resType)
        {
            switch (resType)
            {
                case ResourceType.SignalR:
                    return true;
                default:
                    return false;
            }
        }
    }

    public static class ResourceTypeUtil
    {
        public static IReadOnlyList<ResourceType> All = Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().ToList();
    }
}
