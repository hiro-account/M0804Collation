using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M0804Collation
{
    static class ExtensionMethods
    {
        /// <summary>
        /// データをCSV形式から照合用の形式に変換します
        /// </summary>
        /// <param name="pSource">CSV形式のデータ</param>
        /// <returns>照合用の形式に変換したデータ</returns>
        public static IEnumerable<Receptacle> ToReceptacles(this IEnumerable<string> pSource)
        {
            return pSource.Select(i => i.Split(',')).Select(i => new Receptacle(i[0], int.Parse(i[1]), i[2], i[3] == "" ? false : true, false));
        }

        /// <summary>
        /// データを照合用の形式からCSV形式に変換します
        /// </summary>
        /// <param name="pSource">照合用の形式のデータ</param>
        /// <returns>CSV形式に変換したデータ</returns>
        public static IEnumerable<string> ToCsvs(this IEnumerable<Receptacle> pSource)
        {
            return pSource.Select(i => i.YYYYMMDD + "," + i.Amount + "," + i.Shop + "," + (i.Returns ? "returns" : ""));
        }
    }
}
