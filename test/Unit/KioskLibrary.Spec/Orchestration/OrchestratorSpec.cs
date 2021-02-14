using KioskLibrary.Actions;
using KioskLibrary.Common;
using KioskLibrary.Helpers;
using KioskLibrary.Orchestration;
using KioskLibrary.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using Windows.Web.Http;
using static CommonTestLibrary.TestUtils;

namespace KioskLibrary.Spec.Orchestration
{
    [TestClass]
    public class OrchestratorSpec
    {
        [TestMethod]
        public async Task GetNextOrchestrationTest()
        {
            var currentOrchestrationPath = $"http://{CreateRandomString()}";
            var testInstance = CreateRandomOrchestrationInstance();
            var testInstanceAsString = SerializationHelper.JSONSerialize(testInstance);
            var mockApplicationStorage = new Mock<IApplicationStorage>();
            var mockhttphelper = new Mock<IHttpHelper>();

            var responseMessage = new HttpResponseMessage(HttpStatusCode.Ok)
            {
                Content = new HttpStringContent(testInstanceAsString)
            };

            mockApplicationStorage
                .Setup(x => x.GetSettingFromStorage<string>(It.Is<string>(s => s == Constants.ApplicationStorage.Settings.DefaultOrchestrationURI)))
                .Returns(currentOrchestrationPath)
                .Verifiable();

            mockApplicationStorage
                .Setup(x => x.SaveFileToStorageAsync(It.Is<string>(s => s == Constants.ApplicationStorage.Files.NextOrchestration), It.Is<OrchestrationInstance>(t => SerializationHelper.JSONSerialize(t) == SerializationHelper.JSONSerialize(testInstance))))
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
            var testInstance = CreateRandomOrchestrationInstance();
            var testInstanceAsString = SerializationHelper.JSONSerialize(testInstance);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.Ok)
            {
                Content = new HttpStringContent(testInstanceAsString)
            };
            testInstance.HttpHelper = mockhttphelper.Object;

            mockApplicationStorage
                .Setup(x => x.GetSettingFromStorage<OrchestrationSource>(It.Is<string>(s => s == Constants.ApplicationStorage.Settings.DefaultOrchestrationSource)))
                .Returns(orchestrationSource);

            mockApplicationStorage
                .Setup(x => x.GetFileFromStorageAsync<OrchestrationInstance>(It.Is<string>(s => s == Constants.ApplicationStorage.Files.DefaultOrchestration)))
                .Returns(Task.FromResult(testInstance));

            mockApplicationStorage
                .Setup(x => x.GetSettingFromStorage<string>(It.Is<string>(s => s == Constants.ApplicationStorage.Settings.DefaultOrchestrationURI)))
                .Returns(currentOrchestrationPath);

            mockhttphelper
                .Setup(x => x.GetAsync(It.Is<Uri>(p => p.OriginalString == currentOrchestrationPath)))
                .Returns(Task.FromResult(responseMessage));

            mockhttphelper
                .Setup(x => x.ValidateURI(It.Is<string>(p => p == (testInstance.Actions[0] as ImageAction).Path), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok)))
                .Returns(Task.FromResult((true, null as string)));

            mockhttphelper
                .Setup(x => x.ValidateURI(It.Is<string>(p => p == (testInstance.Actions[1] as WebsiteAction).Path), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok)))
                .Returns(Task.FromResult((true, null as string)));

            var orchestrator = new Orchestrator(mockhttphelper.Object, mockApplicationStorage.Object, mockTimeHelper.Object);
            await orchestrator.StartOrchestration();
        }
    }
}
