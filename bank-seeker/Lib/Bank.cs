using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BankSeeker.Lib
{
    // <summary>은행 타입 및 이름, <cref="Seeker">해당 파서의 타입</cref></summary>
    using BankTypeInfo = Dictionary<Bank.Type, Tuple<string, System.Type>>;
    public static class Bank
    {
        public enum Type
        {
            KB,
            NH, // not implemented
            WR,  // not implemented
        }

        private static BankTypeInfo info = new BankTypeInfo()
        {
            {Type.KB, Tuple.Create("국민은행", typeof(Seekers.SeekerKB))},
            {Type.NH, Tuple.Create("농협중앙회", typeof(Seekers.NotImplemented))},
            {Type.WR, Tuple.Create("우리은행", typeof(Seekers.NotImplemented))},
        };

        public static string GetName(Bank.Type? t)
        {
            if (t == null) return null;
            return info.ContainsKey((Type)t) ? info[(Type)t].Item1 : null;
        }

        internal static System.Type GetSeekerType(Bank.Type? t)
        {
            if (t == null) return null;
            return info.ContainsKey((Type)t) ? info[(Type)t].Item2 : null;
        }
    }
}
