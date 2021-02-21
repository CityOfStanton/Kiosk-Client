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
using System.Linq;

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
                CreateRandomNumber(1),
                CreateRandomNumber(0),
                CreateRandomNumber(1)
            };
            yield return new object[] {
                null,
                null,
                null,
                Convert.ToBoolean(CreateRandomNumber(0, 1)),
                null,
                null,
                CreateRandomNumber(1)
            };
        }

        [DataTestMethod]
        [DynamicData(nameof(GetConstructorTestData), DynamicDataSourceType.Method)]
        public void ConstructorTest(string name, int? duration, string path, bool autoScroll, int? scrollingTime, int? scrollingResetDelay, int settingsDisplayTime)
        {
            var action = new WebsiteAction(name, duration, path, autoScroll, scrollingTime, scrollingResetDelay, settingsDisplayTime);

            Assert.AreEqual(name, action.Name);
            Assert.AreEqual(duration, action.Duration);
            Assert.AreEqual(path, action.Path);
            Assert.AreEqual(autoScroll, action.AutoScroll);
            Assert.AreEqual(scrollingTime, action.ScrollingTime);
            Assert.AreEqual(scrollingResetDelay, action.ScrollingResetDelay);
            Assert.AreEqual(settingsDisplayTime, action.SettingsDisplayTime);
        }

        [TestMethod]
        public async Task ValidatePassedAsyncTest()
        {
            var randomName = CreateRandomString();
            var randomPath = $"http://{CreateRandomString()}";

            Mock<IHttpHelper> mockHttpClient = new Mock<IHttpHelper>();

            var action = new WebsiteAction(
                randomName,
                CreateRandomNumber(0),
                randomPath,
                false,
                CreateRandomNumber(1),
                CreateRandomNumber(0),
                CreateRandomNumber(1),
                mockHttpClient.Object);

            mockHttpClient
                .Setup(
                    x => x.ValidateURI(It.Is<string>(p => p == randomPath), 
                    It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok),
                    It.Is<string>(n => n == nameof(WebsiteAction.Path))))
                .Returns(Task.FromResult(new ValidationResult(CreateRandomString(), true)));

            var validationResult = await action.ValidateAsync();

            Assert.IsTrue(validationResult.IsValid, "The validation result is True.");
            Assert.AreEqual(5, validationResult.Children.Count, "There are 5 validation results.");
        }

        [TestMethod]
        public async Task ValidateFailedAsyncTest()
        {
            var randomName = CreateRandomString();
            var randomPath = $"http://{CreateRandomString()}";

            Mock<IHttpHelper> mockHttpClient = new Mock<IHttpHelper>();

            var invalidActionWithSmallScrollingTime = new WebsiteAction(
                randomName,
                -1,
                randomPath,
                false,
                -1,
                -1,
                -1,
                mockHttpClient.Object);

            var uriValidationMessage = new ValidationResult(invalidActionWithSmallScrollingTime.Name, true);
            uriValidationMessage.Children.Add(new ValidationResult(nameof(WebsiteAction.ScrollingTime), false, Constants.Validation.Actions.WebsiteAction.InvalidScrollingTime));

            mockHttpClient
                .Setup(
                    x => x.ValidateURI(It.Is<string>(p => p == randomPath), 
                    It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok), 
                    It.Is<string>(n => n == nameof(WebsiteAction.Path))))
                .Returns(Task.FromResult(uriValidationMessage));

            var validationResult = await invalidActionWithSmallScrollingTime.ValidateAsync();

            Assert.IsFalse(validationResult.IsValid, "The result is False.");
            Assert.AreEqual(5, validationResult.Children.Count, "There are 5 validation results.");
            Assert.IsTrue(validationResult.Children.Contains(uriValidationMessage));
            Assert.IsTrue(validationResult.Children.Any(x => x.Message == Constants.Validation.Actions.InvalidDuration));
            Assert.IsTrue(validationResult.Children.Any(x => x.Message == Constants.Validation.Actions.WebsiteAction.InvalidScrollingTime));
            Assert.IsTrue(validationResult.Children.Any(x => x.Message == Constants.Validation.Actions.WebsiteAction.InvalidScrollingResetDelay));
            Assert.IsTrue(validationResult.Children.Any(x => x.Message == Constants.Validation.Actions.WebsiteAction.InvalidSettingsDisplayTime));
        }
    }
}