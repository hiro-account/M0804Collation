using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M0804Collation
{
    class Receptacle
    {
        /// <summary>
        /// データを照合用の形式に整えます
        /// </summary>
        /// <param name="pYYYYMMDD">年月日</param>
        /// <param name="pAmount">金額</param>
        /// <param name="pShop">店舗名</param>
        /// <param name="pReturns">返品したか否か</param>
        /// <param name="pMatched">照合したか否か</param>
        public Receptacle(string pYYYYMMDD, int pAmount, string pShop, bool pReturns, bool pMatched)
        {
            YYYYMMDD = pYYYYMMDD; Amount = pAmount; Shop = pShop; Returns = pReturns; Matched = pMatched;
        }

        public string YYYYMMDD { get; set; }
        public int Amount { get; set; }
        public string Shop { get; set; }
        public bool Returns { get; set; }
        public bool Matched { get; set; }
        public string YYMM
        {
            get
            {
                return YYYYMMDD.Substring(2, 5);
            }
        }
    }
}
