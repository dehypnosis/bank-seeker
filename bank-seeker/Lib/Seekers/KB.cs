using System;
using System.Threading.Tasks;
using System.Drawing;

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

//using Driver = OpenQA.Selenium.Chrome.ChromeDriver;
//using DriverService = OpenQA.Selenium.Chrome.ChromeDriverService;
using Driver = OpenQA.Selenium.PhantomJS.PhantomJSDriver;
using DriverService = OpenQA.Selenium.PhantomJS.PhantomJSDriverService;

using Shipwreck.Phash;
using System.Collections.Generic;
using OpenQA.Selenium.Interactions;

namespace BankSeeker.Lib.Seekers
{

    // <summary>국민은행 파서</summary>
    class SeekerKB : Seeker
    {
        // headless browser
        private DriverService driverService = null;
        private Driver driver = null;

        public SeekerKB()
        {
            driverService = DriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;

        }
        ~SeekerKB()
        {
            driverService.Dispose();
        }

        public override async Task<List<Packet>> Fetch(Account account, uint timeoutSec)
        {
            return await Task.Run(() =>
            {
                if (driver != null) return null; // to be a Singleton becauseof race for BMP files

                try
                {
                    // open page
                    driver = new Driver(driverService);
                    driver.Manage().Window.Size = new Size(765, 1000);
                    driver.Navigate().GoToUrl("https://obank1.kbstar.com/quics?page=C025255&cc=b028364:b028702");

                    // fill the form
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSec));
                    var loading = By.Id("loading");
                    wait.Until(ExpectedConditions.InvisibilityOfElementLocated(loading));

                    var account_num = By.Id("account_num");
                    wait.Until(ExpectedConditions.ElementToBeClickable(account_num));
                    driver.FindElement(account_num).SendKeys(account.Number.Replace("-", ""));

                    var user_id = By.Id("user_id");
                    wait.Until(ExpectedConditions.ElementToBeClickable(user_id));
                    driver.FindElement(user_id).SendKeys(account.UserId);

                    // open password keypad
                    var password = By.Id("비밀번호");
                    wait.Until(ExpectedConditions.ElementToBeClickable(password));
                    driver.FindElement(password).Click();

                    // save keypad image
                    var keypadImagePath = @"./KB_keypad.bmp";
                    var keypad = By.CssSelector(".keypadWrap img");
                    wait.Until(ExpectedConditions.ElementToBeClickable(keypad));
                    driver.GetScreenshot().SaveAsFile(keypadImagePath, ScreenshotImageFormat.Bmp);
                    using (var bitmapKeypad = Bitmap.FromFile(keypadImagePath))
                    {
                        // analyze keypad image
                        var element = driver.FindElement(keypad);
                        var area = new
                        {
                            left = element.Location.X,
                            center = element.Location.X + element.Size.Width / 2,
                            right = element.Location.X + element.Size.Width,
                            width = element.Size.Width,
                            top = element.Location.Y,
                            height = element.Size.Height,
                        };
                        var btnLocations = new Dictionary<string, Point>() {
                            {"num1", new Point(area.left + 46, area.top + 70)},
                            {"num2", new Point(area.center, area.top + 70)},
                            {"num3", new Point(area.right - 46, area.top + 70)},
                            {"num4", new Point(area.left + 46, area.top + 130)},
                            {"xxx1", new Point(area.center, area.top + 130)},
                            {"num6", new Point(area.right - 46, area.top + 130)},
                            {"xxx2", new Point(area.left + 46, area.top + 188)},
                            {"xxx3", new Point(area.center, area.top + 188)},
                            {"xxx4", new Point(area.right - 46, area.top + 188)},
                            {"delOne", new Point(area.left + 46, area.top + 246)},
                            {"xxx5", new Point(area.center, area.top + 246)},
                            {"delAll", new Point(area.right - 46, area.top + 246)},
                            {"submit", new Point(area.center, area.top + 300)},
                        };

                        // SOLUTION IMAGES' digests
                        var solDigests = new Dictionary<string, Digest>() {
                            {"num5", ImagePhash.ComputeDigest(@"./KB_keypad_sol_5.bmp", 1, 1, 180) },
                            {"num7", ImagePhash.ComputeDigest(@"./KB_keypad_sol_7.bmp", 1, 1, 180) },
                            {"num8", ImagePhash.ComputeDigest(@"./KB_keypad_sol_8.bmp", 1, 1, 180) },
                            {"num9", ImagePhash.ComputeDigest(@"./KB_keypad_sol_9.bmp", 1, 1, 180) },
                            {"num0", ImagePhash.ComputeDigest(@"./KB_keypad_sol_0.bmp", 1, 1, 180) },
                        };

                        // find each digit of btns by correlation with solution image
                        var solBtnLocations = new Dictionary<string, Point>();
                        foreach (var btnLocation in btnLocations)
                        {
                            // compare only xxx btns (order shuffled btns 5,7,8,9,0)
                            if (!btnLocation.Key.StartsWith("xxx"))
                            {
                                solBtnLocations.Add(btnLocation.Key, btnLocation.Value);
                                continue;
                            }

                            // crop image
                            var btnImagePath = @"./KB_keypad_" + btnLocation.Key + ".bmp";
                            var rect = new Rectangle(btnLocation.Value.X - 22, btnLocation.Value.Y - 22, 44, 44);
                            using (var bitmap = new Bitmap(rect.Width, rect.Height))
                            using (var graphic = Graphics.FromImage(bitmap))
                            {
                                graphic.DrawImage(bitmapKeypad, 0, 0, rect, GraphicsUnit.Pixel);
                                bitmap.Save(btnImagePath);

                                // find each digit for btns
                                var btnDigest = ImagePhash.ComputeDigest(btnImagePath, 1, 1, 180);
                                string answerKey = null;
                                double answerCorr = 0;
                                foreach (var solDigest in solDigests)
                                {
                                    var corr = ImagePhash.GetCrossCorrelation(btnDigest, solDigest.Value);
                                    if (corr > answerCorr)
                                    {
                                        answerCorr = corr;
                                        answerKey = solDigest.Key;
                                    }
                                }

                                // add to solved btn locations
                                solBtnLocations.Add(answerKey, btnLocation.Value);
                                Console.WriteLine(btnLocation.Key + " => " + answerKey);
                            }
                        }
                        

                        // now tocuh the keypad as the solution
                        Point locationTo;
                        var zero = driver.FindElement(password);
                        var dx = -area.left + 90;
                        var dy = -area.top + 15;

//                        // helper to adjust dx,dy
//                        driver.ExecuteScript(@"
//window.onclick = function(e) {
//    var d = document.createElement('div');
//    d.style.width='2px'; d.style.height='2px';
//    d.style.position='absolute';
//    d.style.display='block';
//    d.style.top=e.clientY+'px';
//    d.style.left=e.clientX+'px';
//    d.style.background='red';
//    d.style.zIndex='10000000';
//    document.body.appendChild(d);
//};
//                        ");

                        foreach (var digit in account.Password.ToCharArray())
                        {
                            locationTo = solBtnLocations["num" + digit];
                            new Actions(driver).MoveToElement(zero).MoveByOffset(locationTo.X + dx, locationTo.Y + dy).Click().Perform();
                            Console.WriteLine(("[num" + digit + "] ") + (locationTo.X + dx) + "," + (locationTo.Y + dy));
                        }
                        locationTo = btnLocations["submit"]; // (373, 554).
                        new Actions(driver).MoveToElement(zero).MoveByOffset(locationTo.X + dx, locationTo.Y + dy).Click().Perform();
                        Console.WriteLine("[submit digits] " + (locationTo.X + dx) + "," + (locationTo.Y + dy));

                        // submit the form
                        wait.Until(ExpectedConditions.InvisibilityOfElementLocated(keypad));
                        var submit = By.CssSelector("input[type=submit]");
                        driver.FindElement(submit).Click();

                        // check page loaded
                        // driver.GetScreenshot().SaveAsFile(@"./KB_Result.bmp", ScreenshotImageFormat.Bmp);
                        var table = By.CssSelector(".tType01");
                        wait.Until(ExpectedConditions.ElementIsVisible(table));

                        // data processing
                        var rawPackets = driver.ExecuteScript(@"
if (!document.querySelectorAll) {
    document.querySelectorAll = function(selectors) {
        var style = document.createElement('style'), elements = [], element;
        document.documentElement.firstChild.appendChild(style);
        document._qsa = [];

        style.styleSheet.cssText = selectors + '{x-qsa:expression(document._qsa && document._qsa.push(this))}';
        window.scrollBy(0, 0);
        style.parentNode.removeChild(style);

        while (document._qsa.length) {
        element = document._qsa.shift();
        element.style.removeAttribute('x-qsa');
        elements.push(element);
        }
        document._qsa = null;
        return elements;
    };
}

return (function(results) {
    var trs = document.querySelectorAll('.tType01 tbody tr')
    for(var i=0; i<trs.length; i++) {
        var tr = trs[i];
        if (i % 2 == 0){
            var res = results[i / 2] = {
                datetime: tr.querySelector('td:nth-child(1)').textContent.trim(),
			    note: tr.querySelector('td:nth-child(2)').textContent.trim(),
			    myName: tr.querySelector('td:nth-child(3)').textContent.trim(),
			    outAmount: parseInt(tr.querySelector('td:nth-child(4)').textContent.trim().replace(/,/g, '')),
			    inAmount: parseInt(tr.querySelector('td:nth-child(5)').textContent.trim().replace(/,/g, '')),
			    balance: parseInt(tr.querySelector('td:nth-child(6)').textContent.trim().replace(/,/g, '')),
			    bank: tr.querySelector('td:nth-child(7)').textContent.trim(),
			    type: tr.querySelector('td:nth-child(8)').textContent.trim(),
            };
            res.datetime = new Date(res.datetime.substr(0, 10) + ' ' + res.datetime.substr(10));
        }
        else
        {
            results[(i - 1) / 2].yourName = tr.textContent.trim();
        }
    }
    return results;
})([]);
                        ");

                        // reprocessing as Packet def
                        List<Packet> packets = new List<Packet>();
                        var trs = driver.FindElementsByCssSelector(".tType01 tbody tr");
                        for (var i=0; i<trs.Count; i++)
                        {
                            var tr = trs[i];
                            if (i % 2 == 0)
                            {
                                var datetimeTemp = tr.FindElement(By.CssSelector("td:nth-child(1)")).GetAttribute("textContent").Trim();
                                datetimeTemp = datetimeTemp.Substring(0, 10) + " " + datetimeTemp.Substring(10);
                                var packet = new Packet
                                {
                                    datetime = Convert.ToDateTime(datetimeTemp),
                                    note = tr.FindElement(By.CssSelector("td:nth-child(2)")).GetAttribute("textContent").Trim(),
                                    myName = tr.FindElement(By.CssSelector("td:nth-child(3)")).GetAttribute("textContent").Trim(),
                                    outAmount = Convert.ToDecimal(tr.FindElement(By.CssSelector("td:nth-child(4)")).GetAttribute("textContent").Trim()),
                                    inAmount = Convert.ToDecimal(tr.FindElement(By.CssSelector("td:nth-child(5)")).GetAttribute("textContent").Trim()),
                                    balance = Convert.ToDecimal(tr.FindElement(By.CssSelector("td:nth-child(6)")).GetAttribute("textContent").Trim()),
                                    bank = tr.FindElement(By.CssSelector("td:nth-child(7)")).GetAttribute("textContent").Trim(),
                                    type = tr.FindElement(By.CssSelector("td:nth-child(8)")).GetAttribute("textContent").Trim(),
                                };
                                packets.Add(packet);
                            }
                            else
                            {
                                packets[(i - 1) / 2].yourName = tr.GetAttribute("textContent").Trim();
                            }
                        }

                        return packets;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return null;
                }
                finally
                {
                    if (driver != null) {
                        driver.Dispose();
                        driver = null;
                    }
                }
            });
        }
    }
}
