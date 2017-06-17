using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BankSeeker.Lib;
using System.IO;

namespace BankSeeker
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var teller = new Teller();;
            var account = new Account
            {
                BankType = Bank.Type.KB,
                Number = "957502-01-427751",
                UserId = "YEO7311",
                Password = "1524",
                From = DateTime.Today.AddDays(-1)
            };
            teller.SetAccount(account).SetTimerInterval(15).SetTimerEnabled(true);
            teller.AttachHandler(data => Console.WriteLine(data));
            teller.AttachLogger(log => Console.WriteLine(log));

            Task.Run(async () => await teller.Fetch());
        }
    }
}
