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

            var responseMessage = new HttpResponseMessage(HttpStatusCode.Ok)
            {
                Content = new HttpStringContent(testInstanceAsString)
            };

            var mockApplicationStorage = new Mock<IApplicationStorage>();
            mockApplicationStorage
                .Setup(x => x.GetFromStorage<string>(It.Is<string>(s => s == Constants.ApplicationStorage.CurrentOrchestrationURI)))
                .Returns(currentOrchestrationPath)
                .Verifiable();

            mockApplicationStorage
                .Setup(x => x.SaveToStorage(It.Is<string>(s => s == Constants.ApplicationStorage.NextOrchestration), It.Is<OrchestrationInstance>(t => SerializationHelper.JSONSerialize(t) == SerializationHelper.JSONSerialize(testInstance))))
                .Verifiable();

            var mockhttphelper = new Mock<IHttpHelper>();
            mockhttphelper
                .Setup(x => x.GetAsync(It.Is<Uri>(p => p.OriginalString == currentOrchestrationPath)))
                .Returns(Task.FromResult(responseMessage))
                .Verifiable();

            await Orchestrator.GetNextOrchestration(mockhttphelper.Object, mockApplicationStorage.Object);

            mockApplicationStorage.VerifyAll();
            mockhttphelper.VerifyAll();
        }
    }
}
