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

namespace KioskLibrary.Spec.Actions
{
    [TestClass]
    public class WebsiteActionSpec
    {
        public static IEnumerable<object[]> GetConstructorTestData()
        {
            yield return new object[] {
                CreateRandomString(), 
                CreateRandomNumber(), 
                CreateRandomString(), 
                Convert.ToBoolean(CreateRandomNumber(0, 1)),
                CreateRandomNumber(),
                (double?)CreateRandomNumber(),
                CreateRandomNumber(),
                CreateRandomNumber()
            };
            yield return new object[] {
                null,
                null,
                null,
                Convert.ToBoolean(CreateRandomNumber(0, 1)),
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

        [DataTestMethod]
        [DataRow(true, 0, null)]
        [DataRow(false, 6, "ERROR, ERROR, ERROR")]
        public async Task ValidateFailedAsyncTest(bool validationResult, int errorCount, string uriValidationErrorMessage)
        {
            var randomName= CreateRandomString();
            var randomPath = $"http://{CreateRandomString()}";

            Mock<IHttpHelper> mockHttpClient = new Mock<IHttpHelper>();
            mockHttpClient
                .Setup(x => x.ValidateURI(It.Is<string>(p => p == randomPath), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok)))
                .Returns(Task.FromResult((validationResult, uriValidationErrorMessage)));

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

            Assert.AreEqual(validationResult, IsValid, $"The result is {validationResult}.");
            Assert.AreEqual(randomName, Name, "The name is correct.");
            Assert.AreEqual(errorCount, Errors.Count, $"The error count is {errorCount}");
            Assert.IsTrue(Errors.Contains($"The error message is {uriValidationErrorMessage}"));
            Assert.IsTrue(Errors.Contains("Duration must be greater than 0."));
            Assert.IsTrue(Errors.Contains("ScrollDuration must be greater than 0."));
            Assert.IsTrue(Errors.Contains("ScrollInterval must be greater than 0."));
            Assert.IsTrue(Errors.Contains("ScrollResetDelay must be greater than 0."));
            Assert.IsTrue(Errors.Contains("SettingsDisplayTime must be greater than 0."));
        }
    }
}