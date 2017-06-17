using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BankSeeker.Lib
{

    // <summary>계좌 및 타이머를 설정하고, <see cref="Seeker">은행별 파서</see>를 생성 및 실행하고 이벤트 발행/구독 인터페이스 제공</summary>
    class Teller
    {
        // 계좌 설정 및 은행별 파서 생성
        private Account account = null;
        private Seeker seeker = null;
        public Teller SetAccount(Account account)
        {
            this.account = account;
            account.Validate();

            System.Type BankSeekerType = Bank.GetSeekerType(account.BankType);
            seeker = (Seeker)Activator.CreateInstance(BankSeekerType);
            seeker.setTeller(this);
            seeker.SetTimeoutSeconds((uint)(timer.Interval / 1000));
            return this;
        }

        // 계좌 데이터 가져오기 및 이벤트 발행/구독 (데이터 갱신 여부는 체크하지 않음)
        public delegate void TellerPacketEventHandler(List<Seeker.Packet> packets);
        private event TellerPacketEventHandler TellerPacketEvent;
        public async Task Fetch()
        {
            account.Validate();
            var packets = await seeker.Fetch(account);
            if (packets != null && packets.Count > 0) TellerPacketEvent(packets);
        }
        public Teller AttachHandler(TellerPacketEventHandler handler)
        {
            TellerPacketEvent += handler;
            return this;
        }
        public Teller DetachHandler(TellerPacketEventHandler handler)
        {
            TellerPacketEvent -= handler;
            return this;
        }
        private void InitHandler() // 생성자에서 콜
        {
            TellerPacketEvent += packets => Log(@"{packets.Count}개의 거래 조회 완료...");
        }

        // 자동 반복 타이머 설정
        private Timer timer = new Timer();
        public Teller SetTimerInterval(uint sec)
        {
            timer.Interval = (double)(sec * 1000);
            if (seeker != null)
            {
                seeker.SetTimeoutSeconds((uint)(timer.Interval / 1000));
            }
            return this;
        }
        public Teller SetTimerEnabled(bool enabled)
        {
            timer.Enabled = enabled;
            return this;
        }
        private void InitTimer() // 생성자에서 콜
        {
            SetTimerInterval(60); // 기본 1분
            SetTimerEnabled(false);
            timer.Elapsed += async (object sender, ElapsedEventArgs e) => await Fetch();
        }

        // 로거
        public delegate void TellerLogEventHandler(string log);
        private event TellerLogEventHandler TellerLogEvent;
        public Teller AttachLogger(TellerLogEventHandler handler)
        {
            TellerLogEvent += handler;
            return this;
        }
        public void Log(string log)
        {
            if (TellerLogEvent != null)
            {
                TellerLogEvent(log);
            } else {
                Console.WriteLine(log);
            }
        }

        // 초기화
        public Teller()
        {
            InitHandler();
            InitTimer();
        }
        public Teller(Account account) : this()
        {
            SetAccount(account);
        }
    }
}
