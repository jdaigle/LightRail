using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LightRail.Client.Logging;

namespace LightRail.Client.FastServiceLocator
{
    public class FastContainer : IDisposable
    {
        public static bool SupperLogging = false;
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(FastContainer));
        private readonly IDictionary<Type, FastContainerRegistration> registrations = new Dictionary<Type, FastContainerRegistration>();

        public FastContainer() { }

        private FastContainer(FastContainer other)
        {
            this.registrations = new Dictionary<Type, FastContainerRegistration>();
            foreach (var otherRegistration in other.registrations)
            {
                this.registrations.Add(otherRegistration.Key, otherRegistration.Value.Clone());
            }
        }

        public virtual FastContainerRegistration Register<TService>(Func<FastContainer, TService> resolve)
            where TService : class
        {
            AssertNotDisposed();
            if (SupperLogging)
            {
                Logger.Debug("Registering Wireup Callback for {0}", typeof(TService));
            }
            var registration = new FastContainerRegistration(c => (object)resolve(c));
            this.registrations[typeof(TService)] = registration;
            return registration;
        }

        public virtual FastContainerRegistration Register(Type type, Func<FastContainer, object> resolve)
        {
            AssertNotDisposed();
            if (SupperLogging)
            {
                Logger.Debug("Registering Wireup Callback for {0}", type);
            }
            var registration = new FastContainerRegistration(c => (object)resolve(c));
            this.registrations[type] = registration;
            return registration;
        }

        public virtual FastContainerRegistration Register<TService>(TService instance)
        {
            AssertNotDisposed();
            return Register(typeof(TService), instance);
        }

        public virtual FastContainerRegistration Register(Type type, object instance)
        {
            AssertNotDisposed();
            if (Equals(instance, null))
                throw new ArgumentNullException("instance", "Instance Cannot Be Null");

            if (type.IsValueType && type.IsInterface)
                throw new ArgumentException("Type Must Be Interface", "instance");

            if (SupperLogging)
            {
                Logger.Debug("Registering Service Instance {0}", type);
            }
            var registration = new FastContainerRegistration(instance);
            this.registrations[type] = registration;
            return registration;
        }

        public virtual TService Resolve<TService>()
        {
            AssertNotDisposed();
            return (TService)Resolve(typeof(TService));
        }

        public virtual object Resolve(Type type)
        {
            AssertNotDisposed();
            if (SupperLogging)
            {
                Logger.Debug("Resolving Service {0}", type);
            }

            FastContainerRegistration registration;
            if (this.registrations.TryGetValue(type, out registration))
                return registration.Resolve(this);

            if (SupperLogging)
            {
                Logger.Debug("Unable To Resolve {0}", type);
            }
            return null;
        }

        private static object _FillPropertiesTargetCacheLock = new object();
        private static IDictionary<Type, IList<PropertyInfo>> FillPropertiesTargetPublicProperties = new Dictionary<Type, IList<PropertyInfo>>();

        private static IList<PropertyInfo> GetPropertiesForTarget(Type targetType)
        {
            if (FillPropertiesTargetPublicProperties.ContainsKey(targetType))
            {
                return FillPropertiesTargetPublicProperties[targetType];
            }

            lock (_FillPropertiesTargetCacheLock)
            {
                if (FillPropertiesTargetPublicProperties.ContainsKey(targetType))
                {
                    return FillPropertiesTargetPublicProperties[targetType];
                }

                var publicProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Where(x => x.GetSetMethod() != null).ToList();
                FillPropertiesTargetPublicProperties[targetType] = publicProperties;
                return publicProperties;
            }
        }

        public virtual void FillProperties(object target)
        {
            var properties = GetPropertiesForTarget(target.GetType());
            foreach (var prop in properties)
            {
                if (this.registrations.ContainsKey(prop.PropertyType))
                {
                    if (prop.GetValue(target, null) == null)
                    {
                        prop.SetValue(target, this.Resolve(prop.PropertyType), null);
                    }
                }
            }
        }

        public virtual FastContainer Clone()
        {
            AssertNotDisposed();
            return new FastContainer(this);
        }

        private void AssertNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("TinyContainerRegistration");
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                try
                {
                    foreach (var item in registrations)
                    {
                        item.Value.Dispose();
                    }
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}
