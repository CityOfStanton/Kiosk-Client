/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

namespace KioskClient.Pages.PageArguments
{
    public class MainPageArguments
    {
        public bool ShowSetupInformation { get; set; }

        public MainPageArguments(bool showSetupInformation = false) => ShowSetupInformation = showSetupInformation;
    }
}
