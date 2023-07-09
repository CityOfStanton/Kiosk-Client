using KioskLibrary.Actions;
using KioskLibrary.Common;
using KioskLibrary.Helpers;
using KioskLibrary.Orchestrations;
using KioskLibrary.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using Windows.Web.Http;
using static CommonTestLibrary.TestUtils;

namespace KioskLibrary.Spec.Orchestrations
{
    [TestClass]
    public class OrchestratorSpec
    {
        [TestMethod]
        public async Task GetNextOrchestrationTest()
        {
            var currentOrchestrationPath = $"http://{CreateRandomString()}";
            var testOrchestration = CreateRandomOrchestration();
            var testOrchestrationAsString = SerializationHelper.JSONSerialize(testOrchestration);
            var mockApplicationStorage = new Mock<IApplicationStorage>();
            var mockhttphelper = new Mock<IHttpHelper>();

            var responseMessage = new HttpResponseMessage(HttpStatusCode.Ok)
            {
                Content = new HttpStringContent(testOrchestrationAsString)
            };

            mockApplicationStorage
                .Setup(x => x.GetSettingFromStorage<string>(It.Is<string>(s => s == Constants.ApplicationStorage.Settings.DefaultOrchestrationURI)))
                .Returns(currentOrchestrationPath)
                .Verifiable();

            mockApplicationStorage
                .Setup(x => x.SaveFileToStorageAsync(It.Is<string>(s => s == Constants.ApplicationStorage.Files.NextOrchestration), It.Is<Orchestration>(t => SerializationHelper.JSONSerialize(t) == SerializationHelper.JSONSerialize(testOrchestration))))
                .Verifiable();

            mockhttphelper
                .Setup(x => x.GetAsync(It.Is<Uri>(p => p.OriginalString == currentOrchestrationPath)))
                .Returns(Task.FromResult(responseMessage))
                .Verifiable();

            await Orchestrator.GetNextOrchestration(mockhttphelper.Object, mockApplicationStorage.Object);

            mockApplicationStorage.VerifyAll();
            mockhttphelper.VerifyAll();
        }

        [TestMethod]
        public async Task GetNextOrchestrationFailsTest()
        {
            var mockApplicationStorage = new Mock<IApplicationStorage>();
            var mockhttphelper = new Mock<IHttpHelper>();
            var mockTimeHelper = new Mock<ITimeHelper>();

            mockApplicationStorage
                .Setup(x => x.GetSettingFromStorage<string>(It.Is<string>(s => s == Constants.ApplicationStorage.Settings.DefaultOrchestrationURI)))
                .Verifiable();

            await Orchestrator.GetNextOrchestration(mockhttphelper.Object, mockApplicationStorage.Object);

            mockApplicationStorage.VerifyAll();
        }

        [TestMethod]
        public async Task StartOrchestrationWithNoValidOrchestrationStoredTest()
        {
            var mockApplicationStorage = new Mock<IApplicationStorage>();
            var mockhttphelper = new Mock<IHttpHelper>();
            var mockTimeHelper = new Mock<ITimeHelper>();

            mockApplicationStorage
                .Setup(x => x.GetSettingFromStorage<OrchestrationSource>(It.Is<string>(s => s == Constants.ApplicationStorage.Settings.DefaultOrchestrationSource)))
                .Verifiable();

            var orchestrator = new Orchestrator(mockhttphelper.Object, mockApplicationStorage.Object, mockTimeHelper.Object);
            await orchestrator.StartOrchestration();

            mockApplicationStorage.VerifyAll();
        }

        [DataTestMethod]
        [DataRow(OrchestrationSource.File)]
        [DataRow(OrchestrationSource.URL)]
        public async Task StartOrchestrationTest(OrchestrationSource orchestrationSource)
        {
            var mockApplicationStorage = new Mock<IApplicationStorage>();
            var mockhttphelper = new Mock<IHttpHelper>();
            var mockTimeHelper = new Mock<ITimeHelper>();

            var currentOrchestrationPath = $"http://{CreateRandomString()}";
            var testOrchestration = CreateRandomOrchestration();
            var testOrchestrationAsString = SerializationHelper.JSONSerialize(testOrchestration);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.Ok)
            {
                Content = new HttpStringContent(testOrchestrationAsString)
            };
            testOrchestration.HttpHelper = mockhttphelper.Object;

            mockApplicationStorage
                .Setup(x => x.GetSettingFromStorage<OrchestrationSource>(It.Is<string>(s => s == Constants.ApplicationStorage.Settings.DefaultOrchestrationSource)))
                .Returns(orchestrationSource);

            mockApplicationStorage
                .Setup(x => x.GetFileFromStorageAsync<Orchestration>(It.Is<string>(s => s == Constants.ApplicationStorage.Files.DefaultOrchestration)))
                .Returns(Task.FromResult(testOrchestration));

            mockApplicationStorage
                .Setup(x => x.GetSettingFromStorage<string>(It.Is<string>(s => s == Constants.ApplicationStorage.Settings.DefaultOrchestrationURI)))
                .Returns(currentOrchestrationPath);

            mockhttphelper
                .Setup(x => x.GetAsync(It.Is<Uri>(p => p.OriginalString == currentOrchestrationPath)))
                .Returns(Task.FromResult(responseMessage));

            mockhttphelper
                .Setup(x => x.ValidateURI(It.Is<string>(p => p == (testOrchestration.Actions[0] as ImageAction).Path), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok), It.IsAny<string>()))
                .Returns(Task.FromResult(new ValidationResult(CreateRandomString(), true)));

            mockhttphelper
                .Setup(x => x.ValidateURI(It.Is<string>(p => p == (testOrchestration.Actions[1] as WebsiteAction).Path), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok), It.IsAny<string>()))
                .Returns(Task.FromResult(new ValidationResult(CreateRandomString(), true)));

            var orchestrator = new Orchestrator(mockhttphelper.Object, mockApplicationStorage.Object, mockTimeHelper.Object);
            await orchestrator.StartOrchestration();
        }
    }
}
