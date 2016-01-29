using System.Diagnostics;

namespace LightRail
{
    public class TraceSource
    {
        public static TraceSource FromClass()
        {
            var frame = new StackFrame(1);
            var method = frame.GetMethod();
            return new TraceSource(method.DeclaringType.Namespace + "." + method.DeclaringType.Name);
        }

        public static TraceSource FromNamespace()
        {
            var frame = new StackFrame(1);
            var method = frame.GetMethod();
            return new TraceSource(method.DeclaringType.Namespace);
        }

        public static TraceSource From(string name)
        {
            return new TraceSource(name);
        }

        public TraceSource(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}
