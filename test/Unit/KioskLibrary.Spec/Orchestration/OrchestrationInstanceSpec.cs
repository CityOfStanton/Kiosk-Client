using Action = KioskLibrary.Actions.Action;
using KioskLibrary.Common;
using KioskLibrary.Orchestration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using KioskLibrary.Actions;
using static CommonTestLibrary.TestUtils;
using System.Threading.Tasks;
using KioskLibrary.Helpers;
using Moq;
using Windows.Web.Http;

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

        public static IEnumerable<object[]> ConvertStringToOrchestrationInstanceTestData()
        {
            var orchestrationSourceOptions = Enum.GetValues(typeof(OrchestrationSource));
            var lifecycleBehaviorOptions = Enum.GetValues(typeof(LifecycleBehavior));
            var orderingOptions = Enum.GetValues(typeof(Ordering));
            var r = new Random();
            var orchestrationInstance = new OrchestrationInstance(
                new List<Action>()
                {
                    new ImageAction(),
                    new WebsiteAction()
                },
                CreateRandomNumber(),
                (OrchestrationSource)orchestrationSourceOptions.GetValue(r.Next(orchestrationSourceOptions.Length)),
                (LifecycleBehavior)lifecycleBehaviorOptions.GetValue(r.Next(lifecycleBehaviorOptions.Length)),
                (Ordering)orderingOptions.GetValue(r.Next(orderingOptions.Length)));

            yield return new object[] {
                orchestrationInstance,
                SerializationHelper.JSONSerialize(orchestrationInstance)
            };

            yield return new object[] {
                orchestrationInstance,
                SerializationHelper.XMLSerialize(orchestrationInstance)
            };

            yield return new object[] {
                null,
                ""
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

        [TestMethod]
        public async Task GetOrchestrationInstanceTest()
        {
            var path = new Uri($"http://{CreateRandomString()}");
            var testInstance = new OrchestrationInstance(
                new List<Action>()
                {
                    new ImageAction(),
                    new WebsiteAction()
                },
                CreateRandomNumber(),
                OrchestrationSource.URL,
                LifecycleBehavior.SingleRun,
                Ordering.Sequential
            );
            var testInstanceAsString = SerializationHelper.JSONSerialize(testInstance);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.Ok)
            {
                Content = new HttpStringContent(testInstanceAsString)
            };

            var mockHttpClient = new Mock<IHttpHelper>();
            mockHttpClient
                .Setup(x => x.GetAsync(It.Is<Uri>(p => p == path)))
                .Returns(Task.FromResult(responseMessage));

            var result = await OrchestrationInstance.GetOrchestrationInstance(path, mockHttpClient.Object);

            TestPropertiesForEquality(testInstance, result);
        }

        [TestMethod]
        public async Task GetOrchestrationInstanceAfterExceptionTest()
        {
            var mockHttpClient = new Mock<IHttpHelper>();
            mockHttpClient
                .Setup(x => x.GetAsync(It.IsAny<Uri>()))
                .ThrowsAsync(new Exception());

            var result = await OrchestrationInstance.GetOrchestrationInstance(new Uri($"http://{CreateRandomString()}"), mockHttpClient.Object);

            Assert.IsNull(result);
        }

        [DataTestMethod]
        [DynamicData(nameof(ConvertStringToOrchestrationInstanceTestData), DynamicDataSourceType.Method)]
        public void ConvertStringToOrchestrationInstanceTest(OrchestrationInstance expectedInstance, string serializedInstance)
        {
            var result = OrchestrationInstance.ConvertStringToOrchestrationInstance(serializedInstance);
            TestPropertiesForEquality(expectedInstance, result);
        }
    }
}
