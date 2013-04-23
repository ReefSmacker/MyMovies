using System;
using System.Collections.Generic;
using System.Text;

namespace MyMovies
{
    static public class Certification
    {
        /// <summary>
        /// Certification ratings
        /// </summary>
        static private string AustralianRatings(int rating)
        {
            switch(rating)
            {
                case 0:
                    return "Unrated";         // Unrated

                case 1:
                    return "AU-G";

                case 3:
                    return "AU-PG";

                case 4:
                    return "AU-M";
            
                case 5:
                    return "AU-MA";
            
                case 6:
                    return "AU-R";

                case 7:
                    return "AU-X";

                default:
                    return "Unknown";
            }
        }

        static private string UKRatings(int rating)
        {
            switch (rating)
            {
                case 0:
                    return "UK-Uc";

                case 1:
                    return "UK-U";

                case 2:
                    return "UK-PG";

                case 3:
                    return "UK-12A";

                case 4:
                    return "UK-12";

                case 5:
                    return "UK-15";

                case 6:
                    return "UK-18";

                case 7:
                    return "UK-R18";

                default:
                    return AustralianRatings(rating);
            }
        }

        static private string USARatings(int rating)
        {
            switch (rating)
            {
                case 1:
                    return "G";

                case 3:
                    return "PG";

                case 4:
                    return "PG-13";

                case 5:
                    return "R";

                case 6:
                    return "NC-17";

                default:
                    return AustralianRatings(rating);
            }
        }

        /// <summary>
        /// no longer used to align with delivered MediaPortal icons.
        /// </summary>
        /// <param name="rating"></param>
        /// <returns></returns>
        static private string CanadianRatings(int rating)
        {
            switch (rating)
            {
                case 4:
                    return "14";

                case 5:
                    return "18";

                case 6:
                    return "XXX";

                default:
                    return AustralianRatings(rating);
            }
        }

        static public string Rating(int numericCertification)
        {
            return Rating(numericCertification, 4 /* Australia */ );
        }

        static public string Rating(int certification, int country)
        {
            switch (country)
            {
                case 76:    // UK
                    return UKRatings(certification);

                case 77:    // USA
                    return USARatings(certification);

                case 12:    // Canada
                    return USARatings(certification);

                default:    // Australia.
                    return AustralianRatings(certification);
            }
        }
    }
}
