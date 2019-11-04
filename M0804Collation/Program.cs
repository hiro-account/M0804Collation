using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M0804Collation
{
    class Program
    {
        static void Main(string[] args)
        {
            new Collation().Execute(@"csvs\\f1_prematch", @"csvs\\f2_unmatched");
        }
    }
}
