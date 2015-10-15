using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public interface IServiceLocator : IDisposable
    {
        void RegisterSingleton<T>(T instance) where T : class;

        T Resolve<T>();
        object Resolve(Type type);

        IServiceLocator CreateNestedContainer();
    }
}
