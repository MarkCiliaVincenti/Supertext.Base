﻿using System;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core;
using Supertext.Base.Common;
using Supertext.Base.Configuration;

namespace Supertext.Base.NetFramework.Configuration
{
    public static class ConfigurationExtension
    {
        public static void RegisterConfigurationsWithAppConfigValues(this ContainerBuilder builder, params Assembly[] assemblies)
        {
            Validate.NotNull(assemblies, nameof(assemblies));

            RegisterAndInitializeConfigTypes(builder, assemblies);
        }

        private static void RegisterAndInitializeConfigTypes(ContainerBuilder builder, params Assembly[] assemblies)
        {
            builder.RegisterAssemblyTypes(assemblies)
                .Where(t => t.GetTypeInfo().IsAssignableTo<IConfiguration>())
                .AsSelf()
                .OnActivating(SetupValues);
        }

        private static void SetupValues(IActivatingEventArgs<object> args)
        {
            var configInstance = args.Instance;
            var properties = configInstance.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                var settingKey = propertyInfo.GetCustomAttributes<SettingsKeyAttribute>().SingleOrDefault();
                if (settingKey != null)
                {
                    SetValueIfSome(propertyInfo, configInstance, settingKey.AppSettingsKey);
                    continue;
                }

                if (SettingConnectionStringWhenAvailable(propertyInfo, configInstance))
                {
                    continue;
                }

                SetValueIfSome(propertyInfo, configInstance, propertyInfo.Name);
            }
        }

        private static bool SettingConnectionStringWhenAvailable(PropertyInfo propertyInfo, object configInstance)
        {
            var connectionStringAttribute = propertyInfo.GetCustomAttributes<ConnectionStringNameAttribute>().SingleOrDefault();
            if (connectionStringAttribute != null)
            {
                var connectionStringOption = GetConnectionStringValue(connectionStringAttribute.ConnectionStringName);
                if (connectionStringOption.IsSome)
                {
                    propertyInfo.SetValue(configInstance, Convert(connectionStringOption.Value, propertyInfo.PropertyType));
                    return true;
                }
            }

            return false;
        }

        private static void SetValueIfSome(PropertyInfo propertyInfo, object configInstance,
            string appsettingsKey)
        {
            var valueOption = GetSettingsValue(appsettingsKey);
            if (valueOption.IsSome)
            {
                propertyInfo.SetValue(configInstance, Convert(valueOption.Value, propertyInfo.PropertyType));
            }
        }

        private static object Convert(object value, Type targetType)
        {
            var tc = TypeDescriptor.GetConverter(targetType);
            return tc.ConvertFrom(value);
        }

        private static Option<object> GetSettingsValue(string settingsKey)
        {
            if (ConfigurationManager.AppSettings.AllKeys.Any(key => key == settingsKey))
            {
                var value = ConfigurationManager.AppSettings[settingsKey];
                return Option<object>.Some(value);
            }

            Console.WriteLine($"Key {settingsKey} not available");
            return Option<object>.None();
        }

        private static Option<object> GetConnectionStringValue(string connectionStringKey)
        {
            foreach (ConnectionStringSettings connectionStringSettings in ConfigurationManager.ConnectionStrings)
            {
                if (connectionStringSettings.Name.ToLowerInvariant() == connectionStringKey.ToLowerInvariant())
                {
                    return Option<object>.Some(ConfigurationManager.ConnectionStrings[connectionStringSettings.Name]);
                }
            }

            Console.WriteLine($"Connection string with name {connectionStringKey} not available");
            return Option<object>.None();
        }
    }
}