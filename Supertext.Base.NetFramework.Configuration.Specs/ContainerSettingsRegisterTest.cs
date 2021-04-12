﻿using Autofac;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Supertext.Base.NetFramework.Configuration.Specs
{
    [TestClass]
    public class ContainerSettingsRegisterTest
    {
        private ContainerBuilder _builder;

        [TestInitialize]
        public void Setup()
        {
            _builder = new ContainerBuilder();
        }

        [TestMethod]
        public void RegisterConfigurationsWithAppConfigValues_SettingsAvailable_RegisteredWithConfiguredValues()
        {
            _builder.RegisterConfigurationsWithAppConfigValues(GetType().Assembly);

            var container = _builder.Build();
            var config = container.Resolve<DummyConfig>();

            config.Value.Should().Be("any Value");
            config.AnotherValue.Should().Be("another value");
            config.SomeInt.Should().Be(4711);
            config.AnotherInt.Should().Be(9712);
            config.DoubleValue.Should().Be(0);
            config.ConnectionString.Should().Be("bla");
            config.SuperSecret.Should().Be("very secret value");
        }
    }
}