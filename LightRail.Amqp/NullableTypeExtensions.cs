namespace LightRail.Amqp
{
    public static class NullableTypeExtensions
    {
        public static bool IsTrue(this bool? value)
        {
            return value.HasValue && value.Value;
        }
    }
}
