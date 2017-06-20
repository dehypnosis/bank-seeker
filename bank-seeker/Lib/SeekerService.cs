//using Driver = OpenQA.Selenium.Chrome.ChromeDriver;
//using DriverService = OpenQA.Selenium.Chrome.ChromeDriverService;
using Driver = OpenQA.Selenium.PhantomJS.PhantomJSDriver;
using DriverService = OpenQA.Selenium.PhantomJS.PhantomJSDriverService;


namespace BankSeeker.Lib
{

    // <summary>은행별 파서를 위한 공용 서비스</summary>
    public class SeekerService
    {
        DriverService driverService;

        public SeekerService()
        {
            // webdriver
            driverService = DriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            
        }

        ~SeekerService()
        {
            if (driverService != null)
                driverService.Dispose();
        }

        public Driver GetDriver()
        {
            return new Driver(driverService);
        }
    }
}
