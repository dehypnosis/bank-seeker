using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankSeeker.Lib
{
    // <summary>은행별 파서의 인터페이스 및 데이터 구조, Fetch 호출시 서비스의 도움을 받아서 비동기로 데이터를 파싱하고 포맷에 맞게 변환</summary>
    abstract public class Seeker : IDisposable
    {
        protected class NeedToRefetchError : Exception { }
        protected abstract List<Packet> FetchPackets(Account account);

        // 파서별로 조회된 내역을 암호화 및 중복 체크 로직으로 래핑
        public List<Package> Fetch(Account account)
        {
            try
            {
                var packets = FetchPackets(account);
                if (packets == null) return null;

                var packages = packets.ConvertAll<Package>(new Converter<Packet, Package>(packet =>
                {
                    return new Package
                    {
                        Packet = packet,
                        Account = account,
                        Hash = "" + (uint)(packet.Date + "" + packet.Balance).GetHashCode()
                    };
                }));

                return packages;
            } catch (NeedToRefetchError)
            {
                return Fetch(account);
            }
        }

        public abstract void Dispose();
    }
}
