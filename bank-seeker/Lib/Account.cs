using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BankSeeker.Lib
{
    // <summary>파싱 할 계좌 정보</summary>
    public class Account : IXmlSerializable
    {
        public string Name { get; set; } = "이름 없음";
        public uint IntervalSeconds { get; set; } = 0;
        public uint IntervalMins {
            get
            {
                return IntervalSeconds / 60;
            }
            set
            {
                IntervalSeconds = value * 60;
            }
        }
        public Bank Bank { get; set; } = Bank.ByCode("NH");
        public string Number { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Password { get; set; } = "";
        public DateTime From { get; set; } = DateTime.Today;
        public DateTime To { get; set; } = DateTime.Today;

        // validate
        public void Validate()
        {
            if (Number.Trim().Equals("")
                || UserId.Trim().Equals("")
                || Password.Trim().Equals(""))
            {
                throw new AccountError();
            }
        }

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Name = reader.GetAttribute("Name");
            IntervalMins = Convert.ToUInt16(reader.GetAttribute("IntervalMins"));
            Bank = Bank.ByCode(reader.GetAttribute("Bank.Code"));
            Number = reader.GetAttribute("Number");
            UserId = reader.GetAttribute("UserId");
            Password = reader.GetAttribute("Password");
            From = DateTime.Today.Subtract(TimeSpan.FromDays(Convert.ToDouble(reader.GetAttribute("DateRangeDays"))));
            To = DateTime.Today;

            reader.Read();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("IntervalMins", IntervalMins + "");
            writer.WriteAttributeString("Bank.Code", Bank.Code);
            writer.WriteAttributeString("Number", Number);
            writer.WriteAttributeString("UserId", UserId);
            writer.WriteAttributeString("Password", Password);
            writer.WriteAttributeString("DateRangeDays", To.Subtract(From).TotalDays.ToString());
        }

        public class AccountError : Exception {};
    }
}