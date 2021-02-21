﻿/*
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
using System.Linq;
using Windows.UI.Xaml.Media;
using KioskLibrary.Common;

namespace KioskLibrary.Spec.Actions
{
    [TestClass]
    public class ImageActionSpec
    {
        public static IEnumerable<object[]> GetConstructorTestData()
        {
            var options = Enum.GetValues(typeof(Stretch));
            var r = new Random();

            yield return new object[] {
                CreateRandomString(),
                CreateRandomNumber(0),
                CreateRandomString(),
                (Stretch)options.GetValue(r.Next(options.Length))
            };
            yield return new object[] {
                null,
                null,
                null,
                (Stretch)options.GetValue(r.Next(options.Length))
            };
        }

        [DataTestMethod]
        [DynamicData(nameof(GetConstructorTestData), DynamicDataSourceType.Method)]
        public void ConstructorTest(string name, int? duration, string path, Stretch stretch)
        {
            var action = new ImageAction(name, duration, path, stretch);

            Assert.AreEqual(name, action.Name);
            Assert.AreEqual(duration, action.Duration);
            Assert.AreEqual(path, action.Path);
            Assert.AreEqual(stretch, action.Stretch);
        }

        [DataTestMethod]
        [DataRow(true, 0, null)]
        [DataRow(false, 1, "ERROR, ERROR, ERROR")]
        public async Task ValidateFailedAsyncTest(bool expectedIsValid, int errorCount, string errorMessage)
        {
            var randomName = CreateRandomString();
            var randomPath = $"http://{CreateRandomString()}";

            Mock<IHttpHelper> mockHttpClient = new Mock<IHttpHelper>();

            var action = new ImageAction(
                randomName,
                CreateRandomNumber(0),
                randomPath,
                Stretch.None,
                mockHttpClient.Object);

            mockHttpClient
                .Setup(x => x.ValidateURI(It.Is<string>(p => p == randomPath), It.Is<HttpStatusCode>(h => h == HttpStatusCode.Ok), It.Is<string>(n => n == action.Name)))
                .Returns(Task.FromResult(new ValidationResult(nameof(ImageAction.Path), expectedIsValid, errorMessage)));

            var validationResult = await action.ValidateAsync();

            Assert.AreEqual(expectedIsValid, validationResult.IsValid, $"The result is {expectedIsValid}.");
            Assert.AreEqual(errorCount, validationResult.Children.Count, $"The error count is {errorCount}");
            Assert.AreEqual(errorMessage, validationResult.Children.FirstOrDefault(), $"The error message is {errorMessage}");
        }
    }
}