using System;
using System.Collections.Generic;
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
            return this;
        }

        // 계좌 데이터 가져오기 및 이벤트 발행/구독 (데이터 갱신 여부 체크는 Teller가 하지 않음)
        public delegate void TellerEventHandler(List<Seeker.Packet> packets);
        private event TellerEventHandler TellerEvent;
        public async Task Fetch()
        {
            account.Validate();
            var packets = await seeker.Fetch(account, (uint)(timer.Interval/1000));
            if (packets != null && packets.Count > 0) TellerEvent(packets);
        }
        public Teller AttachHandler(TellerEventHandler handler)
        {
            TellerEvent += handler;
            return this;
        }
        public Teller DetachHandler(TellerEventHandler handler)
        {
            TellerEvent -= handler;
            return this;
        }
        private void InitHandler() // 생성자에서 콜
        {
            TellerEvent += data => Console.WriteLine("Data delivered");
        }

        // 자동 반복 타이머 설정
        private Timer timer = new Timer();
        public Teller SetTimerInterval(uint sec)
        {
            timer.Interval = (double)(sec * 1000);
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
