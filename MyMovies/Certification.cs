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
                    return "Unrated";

                case 1:
                    return "G";

                case 3:
                    return "PG";

                case 4:
                    return "M";
            
                case 5:
                    return "MA";
            
                case 6:
                    return "R";

                case 7:
                    return "X";

                default:
                    return "Unknown";
            }
        }

        static private string UKRatings(int rating)
        {
            switch (rating)
            {
                case 4:
                    return "12 (UK)";

                case 5:
                    return "15 (UK)";

                case 6:
                    return "18 (UK)";

                case 7:
                    return "R18 (UK)";

                default:
                    return AustralianRatings(rating);
            }
        }

        static private string USARatings(int rating)
        {
            switch (rating)
            {
                case 4:
                    return "PG-13 (USA)";

                case 5:
                    return "R (USA)";

                case 6:
                    return "NC-17 (USA)";

                default:
                    return AustralianRatings(rating);
            }
        }

        static private string CanadianRatings(int rating)
        {
            switch (rating)
            {
                case 4:
                    return "14 (Canada)";

                case 5:
                    return "18 (Canada)";

                case 6:
                    return "XXX (Canada)";

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
                    return CanadianRatings(certification);

                default:    // Australia.
                    return AustralianRatings(certification);
            }
        }
    }
}
