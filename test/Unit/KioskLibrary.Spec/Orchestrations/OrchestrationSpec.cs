using Action = KioskLibrary.Actions.Action;
using KioskLibrary.Common;
using KioskLibrary.Orchestrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using KioskLibrary.Actions;
using static CommonTestLibrary.TestUtils;
using System.Threading.Tasks;
using KioskLibrary.Helpers;
using Moq;
using Windows.Web.Http;
using Microsoft.UI.Xaml.Media;
using System.Linq;

namespace KioskLibrary.Spec.Orchestrations
{
    [TestClass]
    public class OrchestrationSpec
    {
        public static IEnumerable<object[]> ConstructorTestData()
        {
            var orchestration = CreateRandomOrchestration();
            yield return new object[] {
                orchestration.Name,
                orchestration.Actions,
                orchestration.PollingIntervalMinutes,
                orchestration.OrchestrationSource,
                orchestration.Lifecycle,
                orchestration.Order
            };

            yield return new object[] {
                null,
                null,
                0,
                orchestration.OrchestrationSource,
                orchestration.Lifecycle,
                orchestration.Order
            };
        }

        public static IEnumerable<object[]> ConvertStringToOrchestrationTestData()
        {
            var orchestration = CreateRandomOrchestration();
            yield return new object[] {
                orchestration,
                SerializationHelper.JSONSerialize(orchestration)
            };

            yield return new object[] {
                orchestration,
                SerializationHelper.XMLSerialize(orchestration)
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
            var invalidMessage1 = new ValidationResult(CreateRandomString(), false, CreateRandomString());
            var invalidMessage2 = new ValidationResult(CreateRandomString(), false, CreateRandomString());

            mockHttpHelper
                .Setup(x => x.ValidateURI(It.Is<string>(u => u == validPath), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok), It.IsAny<string>()))
                .Returns(Task.FromResult(new ValidationResult(CreateRandomString(), true)));

            mockHttpHelper
                .Setup(x => x.ValidateURI(It.Is<string>(u => u == invalidPath1), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok), It.IsAny<string>()))
                .Returns(Task.FromResult(invalidMessage1));

            mockHttpHelper
                .Setup(x => x.ValidateURI(It.Is<string>(u => u == invalidPath2), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok), It.IsAny<string>()))
                .Returns(Task.FromResult(invalidMessage2));

            var validImageAction = new ImageAction(CreateRandomString(), CreateRandomNumber(1), validPath, (Stretch)stretchOptions.GetValue(r.Next(stretchOptions.Length)), mockHttpHelper.Object);
            var validWebsiteAction = new WebsiteAction(CreateRandomString(), CreateRandomNumber(1), validPath, true, CreateRandomNumber(1), CreateRandomNumber(0), CreateRandomNumber(1), mockHttpHelper.Object);

            var orchestration = CreateRandomOrchestration();
            orchestration.Actions.Clear();
            orchestration.Actions.Add(validImageAction);
            orchestration.Actions.Add(validWebsiteAction);
            orchestration.PollingIntervalMinutes = 30;
            orchestration.HttpHelper = mockHttpHelper.Object;

            var orchestrationWithInvalidPollingInterval = CreateRandomOrchestration();
            orchestrationWithInvalidPollingInterval.Actions.Clear();
            orchestrationWithInvalidPollingInterval.Actions.Add(validImageAction);
            orchestrationWithInvalidPollingInterval.Actions.Add(validWebsiteAction);
            orchestrationWithInvalidPollingInterval.PollingIntervalMinutes = 5;
            orchestrationWithInvalidPollingInterval.HttpHelper = mockHttpHelper.Object;

            var invalidImageAction = new ImageAction(CreateRandomString(), -1, invalidPath1, (Stretch)stretchOptions.GetValue(r.Next(stretchOptions.Length)), mockHttpHelper.Object);
            var invalidWebsiteAction = new WebsiteAction(CreateRandomString(), -1, invalidPath2, true, -1, -1, -1, mockHttpHelper.Object);

            var orchestrationWithInvalidActions = CreateRandomOrchestration();
            orchestrationWithInvalidActions.Actions.Clear();
            orchestrationWithInvalidActions.Actions.Add(invalidImageAction);
            orchestrationWithInvalidActions.Actions.Add(invalidWebsiteAction);
            orchestrationWithInvalidActions.PollingIntervalMinutes = 30;
            orchestrationWithInvalidActions.HttpHelper = mockHttpHelper.Object;

            var orchestrationWithInvalidPollingIntervalAndActions = CreateRandomOrchestration();
            orchestrationWithInvalidPollingIntervalAndActions.Actions.Clear();
            orchestrationWithInvalidPollingIntervalAndActions.Actions.Add(invalidImageAction);
            orchestrationWithInvalidPollingIntervalAndActions.Actions.Add(invalidWebsiteAction);
            orchestrationWithInvalidPollingIntervalAndActions.PollingIntervalMinutes = 5;
            orchestrationWithInvalidPollingIntervalAndActions.HttpHelper = mockHttpHelper.Object;

            yield return new object[] {
                orchestration,
                true,
                6,
                new List<string>()
                {
                    $"PollingIntervalMinutes: {Constants.Validation.Actions.Valid}",
                    $"Duration: {Constants.Validation.Actions.Valid}",
                    $"ScrollingTime: {Constants.Validation.Actions.Valid}",
                    $"ScrollingResetDelay: {Constants.Validation.Actions.Valid}",
                    $"SettingsDisplayTime: {Constants.Validation.Actions.Valid}"
                }
            };

            yield return new object[] {
                orchestrationWithInvalidPollingInterval,
                false,
                6,
                new List<string>() {
                    $"PollingIntervalMinutes: {Constants.Validation.Orchestration.InvalidPollingInterval}",
                    $"Duration: {Constants.Validation.Actions.Valid}",
                    $"ScrollingTime: {Constants.Validation.Actions.Valid}",
                    $"ScrollingResetDelay: {Constants.Validation.Actions.Valid}",
                    $"SettingsDisplayTime: {Constants.Validation.Actions.Valid}"
                }
            };

            yield return new object[] {
                orchestrationWithInvalidActions,
                false,
                8,
                new List<string>()
                {
                    $"{invalidMessage1}",
                    $"{invalidMessage2}",
                    $"PollingIntervalMinutes: {Constants.Validation.Actions.Valid}",
                    $"Duration: {Constants.Validation.Actions.InvalidDuration}",
                    $"ScrollingTime: {Constants.Validation.Actions.WebsiteAction.InvalidScrollingTime}",
                    $"ScrollingResetDelay: {Constants.Validation.Actions.WebsiteAction.InvalidScrollingResetDelay}",
                    $"SettingsDisplayTime: {Constants.Validation.Actions.WebsiteAction.InvalidSettingsDisplayTime}"
                }
            };

            yield return new object[] {
                orchestrationWithInvalidPollingIntervalAndActions,
                false,
                8,
                new List<string>()
                {
                    $"{invalidMessage1}",
                    $"{invalidMessage2}",
                    $"PollingIntervalMinutes: {Constants.Validation.Orchestration.InvalidPollingInterval}",
                    $"Duration: {Constants.Validation.Actions.InvalidDuration}",
                    $"ScrollingTime: {Constants.Validation.Actions.WebsiteAction.InvalidScrollingTime}",
                    $"ScrollingResetDelay: {Constants.Validation.Actions.WebsiteAction.InvalidScrollingResetDelay}",
                    $"SettingsDisplayTime: {Constants.Validation.Actions.WebsiteAction.InvalidSettingsDisplayTime}"
                }
            };
        }

        [DataTestMethod]
        [DynamicData(nameof(ConstructorTestData), DynamicDataSourceType.Method)]
        public void ConstructorTest(string name, List<Action> actions, int pollingInterval, OrchestrationSource orchestrationSource, LifecycleBehavior lifecycle, Ordering order)
        {
            var orchestration = new Orchestration(name, actions, pollingInterval, orchestrationSource, lifecycle, order);

            Assert.AreEqual(name, orchestration.Name);
            Assert.AreEqual(actions, orchestration.Actions);
            Assert.AreEqual(pollingInterval, orchestration.PollingIntervalMinutes);
            Assert.AreEqual(orchestrationSource, orchestration.OrchestrationSource);
            Assert.AreEqual(lifecycle, orchestration.Lifecycle);
            Assert.AreEqual(order, orchestration.Order);
        }

        [TestMethod]
        public void DefaultContructorTest()
        {
            var orchestration = new Orchestration();
            Assert.IsNotNull(orchestration.Actions);
        }

        [TestMethod]
        public async Task GetOrchestrationTest()
        {
            var path = new Uri($"http://{CreateRandomString()}");
            var testOrchestration = CreateRandomOrchestration();
            var testOrchestrationAsString = SerializationHelper.JSONSerialize(testOrchestration);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.Ok)
            {
                Content = new HttpStringContent(testOrchestrationAsString)
            };

            var mockHttpClient = new Mock<IHttpHelper>();
            mockHttpClient
                .Setup(x => x.GetAsync(It.Is<Uri>(p => p == path)))
                .Returns(Task.FromResult(responseMessage));

            var result = await Orchestration.GetOrchestration(path, mockHttpClient.Object);

            TestPropertiesForEquality(testOrchestration, result);
        }

        [TestMethod]
        public async Task GetOrchestrationAfterExceptionTest()
        {
            var mockHttpClient = new Mock<IHttpHelper>();
            mockHttpClient
                .Setup(x => x.GetAsync(It.IsAny<Uri>()))
                .ThrowsAsync(new Exception());

            var result = await Orchestration.GetOrchestration(new Uri($"http://{CreateRandomString()}"), mockHttpClient.Object);

            Assert.IsNull(result);
        }

        [DataTestMethod]
        [DynamicData(nameof(ConvertStringToOrchestrationTestData), DynamicDataSourceType.Method)]
        public void ConvertStringToOrchestrationTest(Orchestration expectedOrchestration, string serializedOrchestration)
        {
            var result = Orchestration.ConvertStringToOrchestration(serializedOrchestration);
            TestPropertiesForEquality(expectedOrchestration, result);
        }

        [DataTestMethod]
        [DynamicData(nameof(ValidateAsyncTestData), DynamicDataSourceType.Method)]
        public async Task ValidateAsyncTest(Orchestration orchestration, bool isValid, int validationMessageCount, List<string> validationMessages)
        {
            await orchestration.ValidateAsync();

            Assert.AreEqual(isValid, orchestration.IsValid, $"The validity of the orchestration is {isValid}");

            var listOfMessages = new List<string>();
            foreach (var result in orchestration.ValidationResult)
                GetListOfValidationMessages(result, ref listOfMessages);

            Assert.AreEqual(validationMessageCount, listOfMessages.Count, $"The number of errors is {validationMessageCount}");

            foreach (var message in listOfMessages)
                Assert.IsTrue(validationMessages.Contains(message), $"The validation result contains '{message}'");
        }

        private void GetListOfValidationMessages(ValidationResult result, ref List<string> listOfMessages)
        {
            if (!string.IsNullOrEmpty(result.Message))
                listOfMessages.Add(result.ToString());

            foreach (var childResult in result.Children)
                GetListOfValidationMessages(childResult, ref listOfMessages);
        }
    }
}
