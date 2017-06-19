using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankSeeker.Lib.Seekers
{
    class SeekerNH : Seeker
    {
        protected override List<Packet> FetchPackets(Account account)
        {
            Teller.Log($"[{account.Name}] 농협은 아직 지원되지 않습니다. (BankSeeker.Lib.Seekers.SeekerNH Class)");
            return null;
        }
        public override void Dispose()
        {
        }
    }
}