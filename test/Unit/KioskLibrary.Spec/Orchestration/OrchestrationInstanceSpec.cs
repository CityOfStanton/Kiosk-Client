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
using Windows.UI.Xaml.Media;

namespace KioskLibrary.Spec.Orchestration
{
    [TestClass]
    public class OrchestrationInstanceSpec
    {
        public static IEnumerable<object[]> ConstructorTestData()
        {
            var orchestrationInstance = CreateRandomOrchestrationInstance();
            yield return new object[] {
                orchestrationInstance.Actions,
                orchestrationInstance.PollingIntervalMinutes,
                orchestrationInstance.OrchestrationSource,
                orchestrationInstance.Lifecycle,
                orchestrationInstance.Order
            };

            yield return new object[] {
                null,
                0,
                orchestrationInstance.OrchestrationSource,
                orchestrationInstance.Lifecycle,
                orchestrationInstance.Order
            };
        }

        public static IEnumerable<object[]> ConvertStringToOrchestrationInstanceTestData()
        {
            var orchestrationInstance = CreateRandomOrchestrationInstance();
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

        public static IEnumerable<object[]> ValidateAsyncTestData()
        {
            var stretchOptions = Enum.GetValues(typeof(Stretch));
            var r = new Random();
            var mockHttpHelper = new Mock<IHttpHelper>();
            var validPath = $"http://{CreateRandomString()}";
            var invalidPath1 = $"http://{CreateRandomString()}";
            var invalidPath2 = $"http://{CreateRandomString()}";
            var invalidMessage1 = CreateRandomString();
            var invalidMessage2 = CreateRandomString();

            mockHttpHelper
                .Setup(x => x.ValidateURI(It.Is<string>(u => u == validPath), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok)))
                .Returns(Task.FromResult((true, "")));

            mockHttpHelper
                .Setup(x => x.ValidateURI(It.Is<string>(u => u == invalidPath1), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok)))
                .Returns(Task.FromResult((false, invalidMessage1)));

            mockHttpHelper
                .Setup(x => x.ValidateURI(It.Is<string>(u => u == invalidPath2), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok)))
                .Returns(Task.FromResult((false, invalidMessage2)));

            var validImageAction = new ImageAction(CreateRandomString(), CreateRandomNumber(), validPath, (Stretch)stretchOptions.GetValue(r.Next(stretchOptions.Length)), mockHttpHelper.Object);
            var validWebsiteAction = new WebsiteAction(CreateRandomString(), CreateRandomNumber(), validPath, true, CreateRandomNumber(), CreateRandomNumber(), CreateRandomNumber(), mockHttpHelper.Object);

            var orchestrationInstance = CreateRandomOrchestrationInstance();
            orchestrationInstance.Actions.Clear();
            orchestrationInstance.Actions.Add(validImageAction);
            orchestrationInstance.Actions.Add(validWebsiteAction);
            orchestrationInstance.PollingIntervalMinutes = 30;

            var orchestrationInstanceWithInvalidPollingInterval = CreateRandomOrchestrationInstance();
            orchestrationInstanceWithInvalidPollingInterval.Actions.Clear();
            orchestrationInstanceWithInvalidPollingInterval.Actions.Add(validImageAction);
            orchestrationInstanceWithInvalidPollingInterval.Actions.Add(validWebsiteAction);
            orchestrationInstanceWithInvalidPollingInterval.PollingIntervalMinutes = 5;

            var invalidImageAction = new ImageAction(CreateRandomString(), CreateRandomNumber(), invalidPath1, (Stretch)stretchOptions.GetValue(r.Next(stretchOptions.Length)), mockHttpHelper.Object);
            var invalidWebsiteAction = new WebsiteAction(CreateRandomString(), CreateRandomNumber(), invalidPath2, true, CreateRandomNumber(), CreateRandomNumber(), CreateRandomNumber(), mockHttpHelper.Object);

            var orchestrationInstanceWithInvalidActions = CreateRandomOrchestrationInstance();
            orchestrationInstanceWithInvalidActions.Actions.Clear();
            orchestrationInstanceWithInvalidActions.Actions.Add(invalidImageAction);
            orchestrationInstanceWithInvalidActions.Actions.Add(invalidWebsiteAction);
            orchestrationInstanceWithInvalidActions.PollingIntervalMinutes = 30;

            var orchestrationInstanceWithInvalidPollingIntervalAndActions = CreateRandomOrchestrationInstance();
            orchestrationInstanceWithInvalidPollingIntervalAndActions.Actions.Clear();
            orchestrationInstanceWithInvalidPollingIntervalAndActions.Actions.Add(invalidImageAction);
            orchestrationInstanceWithInvalidPollingIntervalAndActions.Actions.Add(invalidWebsiteAction);
            orchestrationInstanceWithInvalidPollingIntervalAndActions.PollingIntervalMinutes = 5;

            yield return new object[] {
                orchestrationInstance,
                true,
                new List<string>()
            };

            yield return new object[] {
                orchestrationInstanceWithInvalidPollingInterval,
                false,
                new List<string>() { Constants.ValidationMessages.InvalidPollingMessage }
            };

            yield return new object[] {
                orchestrationInstanceWithInvalidActions,
                false,
                new List<string>()
                {
                    $"{orchestrationInstanceWithInvalidActions.Actions[0].Name}: {invalidMessage1}",
                    $"{orchestrationInstanceWithInvalidActions.Actions[1].Name}: {invalidMessage2}"
                }
            };

            yield return new object[] {
                orchestrationInstanceWithInvalidPollingIntervalAndActions,
                false,
                new List<string>()
                {
                    Constants.ValidationMessages.InvalidPollingMessage,
                    $"{orchestrationInstanceWithInvalidPollingIntervalAndActions.Actions[0].Name}: {invalidMessage1}",
                    $"{orchestrationInstanceWithInvalidPollingIntervalAndActions.Actions[1].Name}: {invalidMessage2}"}
            };
        }

        [DataTestMethod]
        [DynamicData(nameof(ConstructorTestData), DynamicDataSourceType.Method)]
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
            var testInstance = CreateRandomOrchestrationInstance();
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

        [DataTestMethod]
        [DynamicData(nameof(ValidateAsyncTestData), DynamicDataSourceType.Method)]
        public async Task ValidateAsyncTest(OrchestrationInstance orchestrationInstance, bool isValid, List<string> errors)
        {
            var (IsValid, Errors) = await orchestrationInstance.ValidateAsync();

            Assert.AreEqual(isValid, IsValid, $"The validity of the instance is {isValid}");
            Assert.AreEqual(errors.Count, Errors.Count, $"The number of errors is {errors.Count}");
            foreach (var error in errors)
                Assert.IsTrue(Errors.Contains(error), $"The validation result contains '{error}'");
        }
    }
}
