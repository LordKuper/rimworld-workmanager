namespace WorkManager
{
    public class RimworldTime
    {
        public RimworldTime(int year, int day, float hour)
        {
            Year = year;
            Day = day;
            Hour = hour;
        }

        public int Day { get; set; }
        public float Hour { get; set; }
        public int Year { get; set; }
    }
}