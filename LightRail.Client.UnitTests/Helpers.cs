using System.Runtime.Serialization;

namespace LightRail.Client
{
    public static class Helpers
    {
        public static T GetUninitializedObject<T>()
        {
            return (T)FormatterServices.GetSafeUninitializedObject(typeof(T));
        }
    }
}
