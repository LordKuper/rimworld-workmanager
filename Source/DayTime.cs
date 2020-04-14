namespace WorkManager
{
    public class DayTime
    {
        public DayTime(int day, float hour)
        {
            Day = day;
            Hour = hour;
        }

        public int Day { get; set; }
        public float Hour { get; set; }
    }
}