using System;

namespace Akka.Persistence.CosmosDB
{
    /// <summary>
    /// Datetime serialization object, since we need precision on the datetime for queries
    /// CosmosDB doesnt support datetime object with the required precision so this class 
    /// splits the datetime into two parts one the Date in numberformat of yyyymmdd and then the ticks starting from the day
    /// </summary>
    public class DateTimeJsonObject
    {
        public int Date { get; set; }
        public long Ticks { get; set; }
        public string TotalTicks { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeJsonObject"/> class.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        public DateTimeJsonObject(DateTime dateTime)
        {
            var year = dateTime.Year.ToString().PadLeft(4, '0');
            var month = dateTime.Month.ToString().PadLeft(2, '0');
            var date = dateTime.Day.ToString().PadLeft(2, '0');

            Date = Convert.ToInt32($"{year}{month}{date}");
            Ticks = dateTime.Subtract(dateTime.Date).Ticks;
            //Since we are calculating Ticks only since start of the date it would be less than 2^53 which is json stored
            TotalTicks = dateTime.Ticks.ToString();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeJsonObject"/> class.
        /// </summary>
        public DateTimeJsonObject()
        {

        }

        /// <summary>
        /// To the date time.
        /// </summary>
        /// <returns></returns>
        public DateTime ToDateTime()
        {
            return new DateTime(long.Parse(TotalTicks));
        }
    }
}
