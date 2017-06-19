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
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Threading;
using static BankSeeker.Lib.Seeker;
using System.Collections.Specialized;
using BankSeeker.Helper;

namespace BankSeeker
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>

    public partial class MainWindow : Window
    {
        private MainViewModel viewModel;
        public MainWindow()
        {
            // 뷰 모델 생성
            DataContext = new MainViewModel();
            viewModel = this.DataContext as MainViewModel;

            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            viewModel.SaveData();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.LoadData();

            // 로깅시 스크롤 내림
            Teller.TellerLogEvent += log => Dispatcher.Invoke(new Action(() => LogScrollViewer.ScrollToEnd()));

            // 계좌 날짜 컨트롤 제약 설정
            this.AccountTo.DisplayDateEnd = this.AccountFrom.DisplayDateEnd = DateTime.Today;
            this.AccountTo.SelectedDateChanged += AccountTo_SelectedDateChanged;
            this.AccountFrom.SelectedDateChanged += AccountFrom_SelectedDateChanged;

            // 도움말 파일 로드
            var textRange = new TextRange(HelpTextBox.Document.ContentStart, HelpTextBox.Document.ContentEnd);
            using (var fileStream = new System.IO.FileStream(ContentManager.getPath("readme.rtf"), System.IO.FileMode.Open))
                textRange.Load(fileStream, System.Windows.DataFormats.Rtf);
        }

        private void AccountFrom_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.AccountTo.SelectedDate < this.AccountFrom.SelectedDate)
            {
                AccountTo.SelectedDate = this.AccountFrom.SelectedDate;
            }
        }
        private void AccountTo_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.AccountTo.SelectedDate < this.AccountFrom.SelectedDate)
            {
                AccountFrom.SelectedDate = this.AccountTo.SelectedDate;
            }
        }

        private void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddAccount();
        }

        private void RemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            if (Confirm("정말 삭제하시겠습니까?"))
                viewModel.RemoveAccount();
        }

        private void Fetch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.Fetch();
            }
            catch (Account.AccountError)
            {
                System.Windows.MessageBox.Show("계좌 정보를 입력하세요.", "오류");
            }
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Stop();
        }

        private void PackageCallback_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Callback((Package)((Button)sender).Tag);
        }

        private void ClearPackages_Click(object sender, RoutedEventArgs e)
        {
            if (Confirm("정말 삭제하시겠습니까?"))
                viewModel.ClearPackages();
        }

        private bool Confirm(string message)
        {
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(message, "확인", MessageBoxButton.OKCancel);
            return messageBoxResult == MessageBoxResult.OK;
        }
    }
}