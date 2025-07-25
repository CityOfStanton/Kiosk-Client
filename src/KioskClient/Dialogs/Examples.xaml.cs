/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Actions;
using KioskLibrary.Common;
using KioskLibrary.Helpers;
using KioskLibrary.Orchestrations;
using System;
using System.Collections.Generic;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KioskClient.Dialogs
{
    public sealed partial class Examples : ContentDialog
    {
        public List<string> Logs { get; set; }
        public Examples()
        {
            this.InitializeComponent();
            Logs = new List<string>();
        }

        private Orchestration ComposeExampleOrchestration(string fileFormat)
        {
            Orchestration orchestration = new Orchestration
            {
                Name = $"{Constants.Application.OrchestrationFileExample.Name}{fileFormat}",
                PollingIntervalMinutes = 15,
                Order = Ordering.Sequential,
                Lifecycle = LifecycleBehavior.ContinuousLoop
            };

            orchestration.Actions.Add(new ImageAction(
                Constants.Application.OrchestrationFileExample.ImageActionExample.Name,
                5,
                Constants.Application.OrchestrationFileExample.ImageActionExample.Path,
                Microsoft.UI.Xaml.Media.Stretch.Uniform));

            orchestration.Actions.Add(new WebsiteAction(
                Constants.Application.OrchestrationFileExample.WebsiteExample.Name,
                20,
                Constants.Application.OrchestrationFileExample.WebsiteExample.Path,
                true,
                15,
                5,
                5));

            return orchestration;
        }

        private async void ButtonJSON_Click(object _, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("JSON Files", new List<string>() { ".json" });
            savePicker.SuggestedFileName = "Settings.json";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                Orchestration orchestration = ComposeExampleOrchestration("JSON");
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                var fileText = SerializationHelper.JSONSerialize(orchestration);
                await Windows.Storage.FileIO.WriteTextAsync(file, fileText);
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                Logs.Add($"Saved \"{orchestration.Name}\" to the following location: {file.Path}");
            }
        }

        private async void ButtonXML_Click(object _, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("XML Files", new List<string>() { ".xml" });
            savePicker.SuggestedFileName = "Settings.xml";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                Orchestration orchestration = ComposeExampleOrchestration("XML");
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                var serializedString = SerializationHelper.XMLSerialize<Orchestration>(orchestration);
                await Windows.Storage.FileIO.WriteTextAsync(file, serializedString);
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                Logs.Add($"Saved \"{orchestration.Name}\" to the following location: {file.Path}");
            }
        }

        private void ContentDialog_Examples_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
                Hide();
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
