# bank-seeker

## 1. 사용 방법
[계좌 설정]에서 계좌를 추가한 후 정보를 입력합니다. [거래 내역 조회]를 클릭하면 조회된 거래 내역이 [실행 내역]에 추가됩니다. 이때 [반복 간격]을 0분으로 설정하면 1회만 실행되며, 이외에는 프로그램이 종료될 때까지 반복 실행됩니다.
지원 은행을 추가하거나, 기존 은행의 로직을 변경하는 경우 등 유지보수에 있어서는 BankSeeker.Lib.Seeker를 상속 받는 BankSeeker.Lib.Seekers.CALSS를 작성하고 BankSeeker.Lib.Banks에 등록하면 충분합니다.

## 2. 연동 및 보안
```json
{  
   "Hash":"거래내역 고유 문자",
   "HashSignature":"거래내역 위조 방지 문자",
   "Account":{  
      "BankCode":"은행코드",
      "Number":"계좌번호"
   },
   "Packet":{  
      "Date":UNIX_TIMESTAMP,
      "Note":"적요",
      "OutName":”출금자명",
      "OutAmount":출금액,
      "InName":"입금자명",
      "InAmount":입금액,
      "Balance":잔액,
      "Bank":"거래영업소",
      "Type":null
   }
}
```

[콜백 설정]에서 조회 내역을 웹 서비스와 연동시킬 수 있습니다. [콜백 URL]을 설정하고 [자동 호출]을 설정하면 새로운 거래 내역이 조회될 때마다 [콜백 URL]로 HTTP POST 요청을 보냅니다. 이때 첨부되는 데이터 포맷은 application/json 입니다. [실행 내역]에서 직접 거래별로 [호출] 버튼을 눌러 [콜백 URL]로 요청을 보낼 수도 있습니다.
이때 [콜백 URL]이 HTTPS 프로토콜인 경우에는 [암호키]나 데이터의 [HashSignature]를 이용 할 필요가 없습니다. 하지만 HTTP로 통신하는 경우엔 최소한의 보안을 통해 위조 요청을 방지 할 수 있습니다
 각 데이터의 [HashSignature] 값은  [MD5(Hash + 암호키)]를 통해서 계산된 문자열입니다. [콜백 URL]의 웹 서버는 전송된 데이터의 [Hash]와 미리 설정된 [암호키]로부터 [HashSignature*]를 계산하고, 전송된 데이터의 [HashSignature]와 비교하여 위조 여부를 판단 할 수 있습니다.

***17/06/19 kim@benzen.io***
