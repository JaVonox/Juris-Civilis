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
            { timeSettings.Fast,0.07f },
            { timeSettings.VeryFast,0.03f }
        };
    
        public static string SetDate(int increment, ref int year, ref int month, ref int day)
        {
            //Increment the year month and day where appropriate
            string newDate = "";
            string monthStr = ((Months)month).ToString();

            int comparisonDate = monthSizes[monthStr]; //size of month to compare against
            if (comparisonDate == -1)
            {
                if (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) //leap year rules
                {
                    comparisonDate = 29;
                }
                else
                {
                    comparisonDate = 28;
                }
            }

            if (day + increment > comparisonDate)
            {
                day = ((day+increment) - comparisonDate);
                month++;
            }
            else
            {
                day += increment;
            }

            if (month > 12)
            {
                month = 1;
                year++;
            }

            newDate += day.ToString() + "/";
            newDate += ((Months)month).ToString() + "/";
            newDate += year;

            return newDate;
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
}
