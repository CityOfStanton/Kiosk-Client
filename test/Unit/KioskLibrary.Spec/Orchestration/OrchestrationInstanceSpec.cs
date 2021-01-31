﻿using Action = KioskLibrary.Actions.Action;
using KioskLibrary.Common;
using KioskLibrary.Orchestration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using KioskLibrary.Actions;
using static CommonTestLibrary.TestUtils;

namespace KioskLibrary.Spec.Orchestration
{
    [TestClass]
    public class OrchestrationInstanceSpec
    {
        public static IEnumerable<object[]> GetConstructorTestData()
        {
            var orchestrationSourceOptions = Enum.GetValues(typeof(OrchestrationSource));
            var lifecycleBehaviorOptions = Enum.GetValues(typeof(LifecycleBehavior));
            var orderingOptions = Enum.GetValues(typeof(Ordering));
            var r = new Random();

            yield return new object[] {
                new List<Action>() 
                {
                    new ImageAction(),
                    new WebsiteAction()
                },
                CreateRandomNumber(),
                (OrchestrationSource)orchestrationSourceOptions.GetValue(r.Next(orchestrationSourceOptions.Length)),
                (LifecycleBehavior)lifecycleBehaviorOptions.GetValue(r.Next(lifecycleBehaviorOptions.Length)),
                (Ordering)orderingOptions.GetValue(r.Next(orderingOptions.Length))
            };

            yield return new object[] {
                null,
                0,
                (OrchestrationSource)orchestrationSourceOptions.GetValue(r.Next(orchestrationSourceOptions.Length)),
                (LifecycleBehavior)lifecycleBehaviorOptions.GetValue(r.Next(lifecycleBehaviorOptions.Length)),
                (Ordering)orderingOptions.GetValue(r.Next(orderingOptions.Length))
            };
        }

        [DataTestMethod]
        [DynamicData(nameof(GetConstructorTestData), DynamicDataSourceType.Method)]
        public void ConstructorTest(List<Action> actions, int pollingInterval, OrchestrationSource orchestrationSource, LifecycleBehavior lifecycle, Ordering order)
        {
            var orchestrationInstance = new OrchestrationInstance(actions, pollingInterval, orchestrationSource, lifecycle, order);

            Assert.AreEqual(actions, orchestrationInstance.Actions);
            Assert.AreEqual(pollingInterval, orchestrationInstance.PollingIntervalMinutes);
            Assert.AreEqual(orchestrationSource, orchestrationInstance.OrchestrationSource);
            Assert.AreEqual(lifecycle, orchestrationInstance.Lifecycle);
            Assert.AreEqual(order, orchestrationInstance.Order);
        }

        [TestMethod]
        public void DefaultContructorTest()
        {
            var orchestrationInstance = new OrchestrationInstance();
            Assert.IsNotNull(orchestrationInstance.Actions);
        }
    }
}