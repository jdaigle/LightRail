using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public abstract class DispatchInfo
    {
        protected DispatchInfo(params Type[] parameterTypes)
        {
            this.ParameterTypes = parameterTypes ?? new Type[0];
        }

        public virtual void Invoke(object arg0)
        {
            throw new NotImplementedException();
        }

        public bool IsMatchByParameterType(params Type[] args)
        {
            if (args.Length != ParameterTypes.Length)
            {
                return false;
            }
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                if (!ParameterTypes[i].IsAssignableFrom(args[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public Type[] ParameterTypes { get; private set; }
    }

    public class GenericDispatchInfo<TArg0> : DispatchInfo
    {
        public GenericDispatchInfo(Action<TArg0> method)
            : base (typeof(TArg0))
        {
            this.method = method;
        }

        private Action<TArg0> method;

        public override void Invoke(object arg0)
        {
            method((TArg0)arg0);
        }
    }
}
