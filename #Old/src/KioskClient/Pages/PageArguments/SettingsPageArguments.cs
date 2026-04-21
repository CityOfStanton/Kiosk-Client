/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Orchestrations;
using System.Collections.Generic;

namespace KioskClient.Pages.PageArguments
{
    public class SettingsPageArguments
    {
        public List<string> Log { get; set; }

        public Orchestration Orchestration { get; set; }

        public SettingPageTabs? SelectedSettingPageTabs { get; set; } = null;

        public SettingsPageArguments(List<string> log, Orchestration orchestration = null, SettingPageTabs? selectedSettingPageTabs = null)
        {
            Log = log;
            Orchestration = orchestration;
            SelectedSettingPageTabs = selectedSettingPageTabs;
        }
    }
}
