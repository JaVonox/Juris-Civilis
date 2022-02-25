using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Calendar
{
    public static class Calendar
    {
        public enum timeSettings
        {
            Pause = 0,
            Normal = 1,
            Fast = 2,
            VeryFast = 3,
        }

        public enum Months
        {
            Null = 0,
            January = 1,
            February = 2,
            March = 3,
            April = 4,
            May = 5,
            June = 6,
            July = 7,
            August = 8,
            September = 9,
            October = 10,
            November = 11,
            December = 12,
        }

        public static Dictionary<string, int> monthSizes = new Dictionary<string, int>()
        {
            {"January",31 },
            {"February",-1 }, //Special case
            {"March",31 },
            {"April",30 },
            {"May",31 },
            {"June",30 },
            {"July",31 },
            {"August",31 },
            {"September",30 },
            {"October",31 },
            {"November",30 },
            {"December",31 }
        };

        public static Dictionary<timeSettings, float> runSpeed = new Dictionary<timeSettings, float>()
        {
            { timeSettings.Pause,0 },
            { timeSettings.Normal,0.10f },
            { timeSettings.Fast,0.03f },
            { timeSettings.VeryFast,0.005f }
        };

        public static string SetDate(int increment, ref Date date)
        {
            //Increment the year month and day where appropriate
            string newDate = "";
            string monthStr = ((Months)date.month).ToString();

            int comparisonDate = monthSizes[monthStr]; //size of month to compare against
            if (comparisonDate == -1)
            {
                if (date.year % 4 == 0 && (date.year % 100 != 0 || date.year % 400 == 0)) //leap year rules
                {
                    comparisonDate = 29;
                }
                else
                {
                    comparisonDate = 28;
                }
            }

            if (date.day + increment > comparisonDate)
            {
                date.day = ((date.day + increment) - comparisonDate);
                date.month++;
            }
            else
            {
                date.day += increment;
            }

            if (date.month > 12)
            {
                date.month = 1;
                date.year++;
            }

            newDate += date.day.ToString() + "/";
            newDate += ((Months)date.month).ToString() + "/";
            newDate += date.year;

            return newDate;
        }

        public static Date ReturnDate(int increment, ref Date r)
        {
            Date tmpDate = new Date();
            tmpDate.day = r.day;
            tmpDate.month = r.month;
            tmpDate.year = r.year;

            for (int i = 1; i <= increment; i++)
            {
                SetDate(1, ref tmpDate);
            }
            return tmpDate;
        }
        public static bool IsAfterDate(Date curDate, Date comparitorDate)
        {
            if(curDate.year > comparitorDate.year) { return true; }
            if(curDate.year == comparitorDate.year)
            {
                if(curDate.month > comparitorDate.month) { return true; }
                if(curDate.month == comparitorDate.month)
                {
                    if(curDate.day >= comparitorDate.day) { return true; }
                    else { return false; }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static void PauseTime(ref timeSettings simSpeed, ref Button pause, ref Button normal, ref Button fast, ref Button veryFast)
        {
            simSpeed = timeSettings.Pause;
            pause.interactable = false;
            normal.interactable = true;
            fast.interactable = true;
            veryFast.interactable = true;
        }
        public static void NormalTime(ref timeSettings simSpeed, ref Button pause, ref Button normal, ref Button fast, ref Button veryFast)
        {
            simSpeed = timeSettings.Normal;
            pause.interactable = true;
            normal.interactable = false;
            fast.interactable = true;
            veryFast.interactable = true;
        }
        public static void FastTime(ref timeSettings simSpeed, ref Button pause, ref Button normal, ref Button fast, ref Button veryFast)
        {
            simSpeed = timeSettings.Fast;
            pause.interactable = true;
            normal.interactable = true;
            fast.interactable = false;
            veryFast.interactable = true;
        }

        public static void VeryFastTime(ref timeSettings simSpeed, ref Button pause, ref Button normal, ref Button fast, ref Button veryFast)
        {
            simSpeed = timeSettings.VeryFast;
            pause.interactable = true;
            normal.interactable = true;
            fast.interactable = true;
            veryFast.interactable = false;
        }
    }

    public class Date
    {
        public int day;
        public int month;
        public int year;
    }
}
