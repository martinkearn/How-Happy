namespace HowHappy_Web.Models
{
    using System.Collections.Generic;

    public class WeekdaySummary
    {
        public System.DayOfWeek DayOfWeek { get; set; }
        /// <summary>
        /// For each happiness score (0-100) (the key) we keep a tally of how many
        /// times that has occurred (the value).
        /// For example, dictionary key 11 would be happiness value 11 and if the number
        /// 3 is in there, then 3 pictures taken on this day will have had a happiness
        /// score of 3.
        /// </summary>
        public Dictionary<int,int> HappinessScores { get; set; } = new Dictionary<int, int>();   
    }
}
