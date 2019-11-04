using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M0804Collation
{
    class Collation
    {
        private const string AllMatched = "未照合分なし";

        /// <summary>
        /// クレジットカードの請求書のデータとレシートのデータを照合します
        /// 照合分を f3_matched フォルダに、未照合分を f2_unmatched フォルダにそれぞれ出力します
        /// </summary>
        /// <param name="pPrematchPath">当月分のデータの位置</param>
        /// <param name="pUnmatchedPath">前月までの未照合分のデータの位置</param>
        /// <returns></returns>
        public bool Execute(string pPrematchPath, string pUnmatchedPath)
        {
            //================================================================================
            // ファイルの内容を読み込む
            //================================================================================

            // prematch（当月分） ----------------------------------------
            IEnumerable<string> prematchFiles = Directory.EnumerateFiles(pPrematchPath).OrderBy(i => i);

            string firstPath = prematchFiles.First();
            string firstFile = firstPath.Substring(firstPath.LastIndexOf("\\") + 1, 4);

            string lastPath = prematchFiles.Last();
            string lastFile = lastPath.Substring(lastPath.LastIndexOf("\\") + 1, 4);

            if (firstFile == lastFile) return false;

            string billFile = prematchFiles.Where(i => i.Contains(firstFile)).OrderByDescending(i => i).First();
            IEnumerable<string> prematchBill = File.ReadAllLines(billFile, Encoding.Default);

            string receiptFile = prematchFiles.Where(i => i.Contains(lastFile)).OrderByDescending(i => i).First();
            IEnumerable<string> prematchReceipt = File.ReadAllLines(receiptFile, Encoding.UTF8);

            // unmatched（前月までの未照合分） ----------------------------------------
            string lastPathOfUnmatched = Directory.EnumerateFiles(pUnmatchedPath).OrderByDescending(i => i).First();
            int lastIndex = lastPathOfUnmatched.LastIndexOf("\\");
            string lastFileOfUnmatched = lastPathOfUnmatched.Substring(lastPathOfUnmatched.LastIndexOf("\\") + 1, 4);

            IEnumerable<string[]> unmatched = Directory.EnumerateFiles(pUnmatchedPath).Where(i => i.Contains(lastFileOfUnmatched)).OrderBy(i => i)
                .Select(i => File.ReadAllLines(i, Encoding.UTF8));


            //================================================================================
            // 当月分と前月までの未照合分を合わせる
            //================================================================================

            // bill（請求書側） ----------------------------------------
            IEnumerable<Receptacle> billFormatted = FormatBill(prematchBill);
            IEnumerable<Receptacle> unmatchedBill = unmatched.First().Where(i => !i.Contains(AllMatched)).Where(i => !i.Contains("checked")).ToReceptacles();
            
            List<Receptacle> billFormattedListed = billFormatted.ToList();
            billFormattedListed.AddRange(unmatchedBill);
            
            IEnumerable<Receptacle> billAdded = billFormattedListed.OrderBy(i => i.YYYYMMDD).ThenBy(i => i.Amount);

            // receipt（レシート側） ----------------------------------------
            IEnumerable<Receptacle> receiptFormatted = FormatReceipt(prematchReceipt);
            IEnumerable<Receptacle> unmatchedReceipt = unmatched.Last().Where(i => !i.Contains(AllMatched)).Where(i => !i.Contains("checked")).ToReceptacles();

            List<Receptacle> listedFormattedReceipt = receiptFormatted.ToList();
            listedFormattedReceipt.AddRange(unmatchedReceipt);
            
            IEnumerable<Receptacle> AddedReceipt = listedFormattedReceipt.OrderBy(i => i.YYYYMMDD).ThenBy(i => i.Amount);


            //================================================================================
            // 請求書側とレシート側を照合する
            //================================================================================

            IEnumerable<IEnumerable<Receptacle>> checkedBandR = MatchBillAndReceipt(billAdded, AddedReceipt);

            Console.WriteLine("Bill Matched----------------------------------------");
            OutputConsole(checkedBandR.First(), true);

            Console.WriteLine("Bill Unmatched----------------------------------------");
            OutputConsole(checkedBandR.First(), false);

            Console.WriteLine("Receipt Matched----------------------------------------");
            OutputConsole(checkedBandR.ElementAt(1), true);

            Console.WriteLine("Receipt Unmatched----------------------------------------");
            OutputConsole(checkedBandR.ElementAt(1), false);

            OutputCsv(checkedBandR);

            return true;
        }

        /// <summary>
        /// 請求書のデータを照合用の形式に整えます
        /// </summary>
        /// <param name="pReadBill">CSVファイルから読み込んだデータ</param>
        /// <returns>照合用の形式に整えたデータ</returns>
        private IEnumerable<Receptacle> FormatBill(IEnumerable<string> pReadBill)
        {
            var headlines = pReadBill.First().Split(',').Select((t, n) => new { Item = t, Index = n });
            int dayIndex = headlines.First(i => i.Item.Contains("日")).Index;
            int amountIndex = headlines.First(i => i.Item.Contains("金額")).Index;
            int shopIndex = headlines.First(i => i.Item.Contains("店")).Index;

            return pReadBill.Skip(1).Select(i => i.Split(',')).Select(i => new
            {
                D = i[dayIndex].Replace("\"", "").Replace(".", "/"),
                A = i[amountIndex].Replace("\"", ""),
                S = i[shopIndex].Replace("\"", "")
            }).Where(i => i.D.Length > 0 && i.A.Length > 0).Select(i => new Receptacle(i.D, int.Parse(i.A), i.S.Length == 0 ? null : i.S, false, false));
        }

        /// <summary>
        /// レシートのデータを照合用の形式に整えます
        /// </summary>
        /// <param name="pReadReceipt">CSVファイルから読み込んだデータ</param>
        /// <returns>照合用の形式に整えたデータ</returns>
        private IEnumerable<Receptacle> FormatReceipt(IEnumerable<string> pReadReceipt)
        {
            return pReadReceipt.Select(i => i.Split(',')).Select(i =>
            {
                Receptacle rcptcl = new Receptacle(i[0], int.Parse(i[1]), null, false, false);

                switch (i.Length)
                {
                    case 3:
                        if (i[2] == "returns")
                        {
                            rcptcl.Returns = true;
                        }
                        else
                        {
                            rcptcl.Shop = i[2];
                        }

                        break;
                    case 4:
                        rcptcl.Shop = i[2];

                        if (i[3] == "returns")
                        {
                            rcptcl.Returns = true;
                        }

                        break;
                    default:
                        break;
                }

                return rcptcl;
            });
        }

        /// <summary>
        /// 請求書側のデータとレシート側のデータを照合します
        /// </summary>
        /// <param name="pBill">請求書側のデータ</param>
        /// <param name="pReceipt">レシート側のデータ</param>
        /// <returns>照合した結果のデータ</returns>
        private IEnumerable<IEnumerable<Receptacle>> MatchBillAndReceipt(IEnumerable<Receptacle> pBill, IEnumerable<Receptacle> pReceipt)
        {
            List<Receptacle> checkedBill = pBill.ToList();
            List<Receptacle> checkedReceipt = pReceipt.ToList();

            for (int i = 0; i < checkedBill.Count(); i++)
            {
                for (int j = 0; j < checkedReceipt.Count(); j++)
                {
                    if (checkedReceipt[j].Matched)
                    {
                        continue;
                    }

                    if (checkedBill[i].YYYYMMDD == checkedReceipt[j].YYYYMMDD && checkedBill[i].Amount == checkedReceipt[j].Amount)
                    {
                        checkedBill[i].Matched = true;
                        checkedReceipt[j].Matched = true;

                        if (checkedReceipt[j].Returns)
                        {
                            checkedBill[i].Returns = true;
                        }

                        break;
                    }
                }
            }

            List<IEnumerable<Receptacle>> checkedBandR = new List<IEnumerable<Receptacle>>();
            checkedBandR.Add(checkedBill);
            checkedBandR.Add(checkedReceipt);

            return checkedBandR;
        }

        /// <summary>
        /// 照合した結果をコンソールに表示します
        /// </summary>
        /// <param name="pCheckedBandR">照合した結果のデータ</param>
        /// <param name="pMatched">trueで照合したデータを、falseで未照合のデータをそれぞれ表示する</param>
        private void OutputConsole(IEnumerable<Receptacle> pCheckedBandR, bool pMatched)
        {
            pCheckedBandR.Where(i => i.Matched == pMatched)
                .Select(i => new { DandA = i.YYYYMMDD + i.Amount.ToString().PadLeft(10, ' '), R = i.Returns ? "returns" : "", M = i.Matched ? "照合" : "未照合", S = i.Shop })
                .ToList().ForEach(i => Console.WriteLine(i.DandA + " " + i.R + "                    " + i.M + " " + i.S));
            Console.WriteLine();
        }

        /// <summary>
        /// 照合した結果をCSVファイルとして出力します
        /// </summary>
        /// <param name="pCheckedBandR">照合した結果のデータ</param>
        private void OutputCsv(IEnumerable<IEnumerable<Receptacle>> pCheckedBandR)
        {
            IEnumerable<Receptacle> matchedBill = pCheckedBandR.First().Where(i => i.Matched);

            string yymm = matchedBill.GroupBy(i => i.YYMM).OrderByDescending(i => i.Count()).Select(i => i.Key).First().Replace("/", "");

            File.WriteAllLines(@"csvs\f3_matched\" + yymm + "_matched.csv", matchedBill.ToCsvs(), Encoding.UTF8);

            string[] allMatchedArray = new string[] { AllMatched };

            IEnumerable<string> unmatchedBill = pCheckedBandR.First().Where(i => !i.Matched).ToCsvs();

            File.WriteAllLines(@"csvs\f2_unmatched\" + yymm + "_bill_result.csv", unmatchedBill.Count() == 0 ? allMatchedArray : unmatchedBill, Encoding.UTF8);

            IEnumerable<Receptacle> matchedReceipt = pCheckedBandR.ElementAt(1).Where(i => i.Matched);

            IEnumerable<string> unmatchedReceipt = pCheckedBandR.ElementAt(1).Where(i => !i.Matched).ToCsvs();

            File.WriteAllLines(@"csvs\f2_unmatched\" + yymm + "_receipt_result.csv"
                , unmatchedReceipt.Count() == 0 ? allMatchedArray : unmatchedReceipt, Encoding.UTF8);
        }
    }
}
