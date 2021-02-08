/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using KioskLibrary.Actions;
using static CommonTestLibrary.TestUtils;
using System.Collections.Generic;
using Moq;
using Windows.Web.Http;
using System.Threading.Tasks;
using KioskLibrary.Helpers;
using KioskLibrary.Common;

namespace KioskLibrary.Spec.Actions
{
    [TestClass]
    public class WebsiteActionSpec
    {
        public static IEnumerable<object[]> GetConstructorTestData()
        {
            yield return new object[] {
                CreateRandomString(),
                CreateRandomNumber(0),
                CreateRandomString(),
                Convert.ToBoolean(CreateRandomNumber(0, 1)),
                CreateRandomNumber(0),
                (double?)CreateRandomNumber(0),
                CreateRandomNumber(0),
                CreateRandomNumber(0)
            };
            yield return new object[] {
                null,
                null,
                null,
                Convert.ToBoolean(CreateRandomNumber(0, 1)),
                null,
                null,
                null,
                null
            };
        }

        [DataTestMethod]
        [DynamicData(nameof(GetConstructorTestData), DynamicDataSourceType.Method)]
        public void ConstructorTest(string name, int? duration, string path, bool autoScroll, int? scrollDuration, double? scrollInterval, int? scrollResetDelay, int? settingsDisplayTime)
        {
            var action = new WebsiteAction(name, duration, path, autoScroll, scrollDuration, scrollInterval, scrollResetDelay, settingsDisplayTime);

            Assert.AreEqual(name, action.Name);
            Assert.AreEqual(duration, action.Duration);
            Assert.AreEqual(path, action.Path);
            Assert.AreEqual(autoScroll, action.AutoScroll);
            Assert.AreEqual(scrollDuration, action.ScrollDuration);
            Assert.AreEqual(scrollInterval, action.ScrollInterval);
            Assert.AreEqual(scrollResetDelay, action.ScrollResetDelay);
            Assert.AreEqual(settingsDisplayTime, action.SettingsDisplayTime);

        }

        [TestMethod]
        public async Task ValidatePassedAsyncTest()
        {
            var randomName = CreateRandomString();
            var randomPath = $"http://{CreateRandomString()}";

            Mock<IHttpHelper> mockHttpClient = new Mock<IHttpHelper>();
            mockHttpClient
                .Setup(x => x.ValidateURI(It.Is<string>(p => p == randomPath), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok)))
                .Returns(Task.FromResult((true, null as string)));

            var action = new WebsiteAction(
                randomName,
                CreateRandomNumber(0),
                randomPath,
                false,
                CreateRandomNumber(0),
                (double)CreateRandomNumber(0),
                CreateRandomNumber(0),
                CreateRandomNumber(0),
                mockHttpClient.Object);

            var (IsValid, Name, Errors) = await action.ValidateAsync();

            Assert.IsTrue(IsValid, "The validation result is True.");
            Assert.AreEqual(randomName, Name, "The name is correct.");
            Assert.AreEqual(0, Errors.Count, "The error count is 0");
        }

        [TestMethod]
        public async Task ValidateFailedAsyncTest()
        {
            var randomName = CreateRandomString();
            var randomPath = $"http://{CreateRandomString()}";
            var uriValidationErrorMessage = $"The error message is {CreateRandomString()}";

            Mock<IHttpHelper> mockHttpClient = new Mock<IHttpHelper>();
            mockHttpClient
                .Setup(x => x.ValidateURI(It.Is<string>(p => p == randomPath), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok)))
                .Returns(Task.FromResult((false, uriValidationErrorMessage)));

            var action = new WebsiteAction(
                randomName,
                -1,
                randomPath,
                false,
                -1,
                (double)-1,
                -1,
                -1,
                mockHttpClient.Object);

            var (IsValid, Name, Errors) = await action.ValidateAsync();

            Assert.IsFalse(IsValid, "The result is False.");
            Assert.AreEqual(randomName, Name, "The name is correct.");
            Assert.AreEqual(6, Errors.Count, "The error count is 6");
            Assert.IsTrue(Errors.Contains(uriValidationErrorMessage));
            Assert.IsTrue(Errors.Contains(Constants.ValidationMessages.ActionValidationErrors.Duration));
            Assert.IsTrue(Errors.Contains(Constants.ValidationMessages.WebsiteActionValidationErrors.ScrollDuration));
            Assert.IsTrue(Errors.Contains(Constants.ValidationMessages.WebsiteActionValidationErrors.ScrollInterval));
            Assert.IsTrue(Errors.Contains(Constants.ValidationMessages.WebsiteActionValidationErrors.ScrollResetDelay));
            Assert.IsTrue(Errors.Contains(Constants.ValidationMessages.WebsiteActionValidationErrors.SettingsDisplayTime));
        }
    }
}