/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

namespace KioskLibrary.Orchestration
{
    public class PollingInterval
    {
        public PollingInterval(string name, int seconds)
        {
            Name = name;
            Seconds = seconds;
        }

        public string Name { get; set; }
        public int Seconds { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
