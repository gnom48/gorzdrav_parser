using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Text;

namespace ParserLib.Gorzdrav;

public class SeleniumManager : IDisposable
{
    private IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public delegate Task ParsingDelegate(Stream stream, CancellationToken cancellationToken);
    public event ParsingDelegate? OnNewPage;

    public SeleniumManager()
    {
        var options = new ChromeOptions();
        
        // Скрываем автоматизацию
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);
        
        // Оптимизация производительности
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        
        // Отключаем загрузку изображений для экономии трафика
        options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
        
        // Реальные размеры окна
        options.AddArgument("--window-size=1920,1080");
        
        _driver = new ChromeDriver(options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        
        // Убираем метки автоматизации через JS
        RemoveAutomationTraces();
    }

    private void RemoveAutomationTraces()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
    }

    /// <summary>
    /// Основной метод для навигации по страницам
    /// </summary>
    /// <param name="baseUrl">Базовый URL категории</param>
    /// <param name="startPage">Начальная страница</param>
    /// <param name="endPage">Конечная страница</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task NavigatePagesAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run(() => _driver.Navigate().GoToUrl(baseUrl), cancellationToken);
            int page = 1;
            int totalPages = GetTotalPages();

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                          
                if (!await WaitForCardsToLoad(cancellationToken))
                {
                    Console.WriteLine($"Не удалось загрузить карточки на странице {page}");
                    continue;
                }
                
                await SimulateHumanBehaviorAsync(cancellationToken);
                
                await ProcessCurrentPageAsync(page, cancellationToken);
                
                if (page <= totalPages)
                {
                    await Task.Delay(Random.Shared.Next(2000, 5000), cancellationToken);
                    await NavigateNextPage(page);
                }

            }
            while (page <= totalPages);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Операция отменена");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в SeleniumManager: {ex.Message}");
        }
    }

    private int GetTotalPages()
    {
        var paginationLinks = _driver.FindElements(By.CssSelector("ul.ui-table-pagination__pages-list > a"));
        if (paginationLinks.Any())
        {
            var lastLink = paginationLinks.Last();
            string lastPageText = lastLink.Text.Trim();
            
            if (int.TryParse(lastPageText, out int lastPage))
            {
                return lastPage;
            }
        }

        return -1;
    }

    /// <summary>
    /// Переход на конкретную страницу пагинации
    /// </summary>
    /// <param name="pageNumber">Номер страницы для перехода</param>
    public async Task<bool> NavigateNextPage(int pageNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var pageLinks = _driver.FindElements(By.CssSelector("ul.ui-table-pagination__pages-list a[href*='/category/sredstva-ot-diabeta/']"));
            
            foreach (var link in pageLinks)
            {
                string linkText = link.Text.Trim();
                
                if (string.IsNullOrEmpty(linkText) || linkText == "...") 
                    continue;
                
                if (int.TryParse(linkText, out int linkPage) && linkPage == pageNumber)
                {
                    ((IJavaScriptExecutor)_driver).ExecuteScript(
                        "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", link);
                    
                    await Task.Delay(500, cancellationToken);
                    
                    link.Click();
                                        
                    return true;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при переходе на страницу {pageNumber}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ожидание загрузки карточек товаров
    /// </summary>
    private async Task<bool> WaitForCardsToLoad(CancellationToken cancellationToken)
    {
        try
        {
            var cardSelectors = new[]
            {
                By.ClassName(".product-card.product-card--grid.product-card--theme--gz")
            };

            bool anyCardFound = false;
            foreach (var selector in cardSelectors)
            {
                try
                {
                    await Task.Run(() => 
                    {
                        return _wait.Until(driver => 
                            driver.FindElements(selector).Count > 0
                        );
                    }, cancellationToken);
                    
                    anyCardFound = true;
                    Console.WriteLine($"Карточки найдены по селектору: {selector}");
                    break;
                }
                catch (WebDriverTimeoutException)
                {
                    continue;
                }
            }

            if (!anyCardFound)
            {
                Console.WriteLine("Карточки не найдены ни по одному селектору");
                return false;
            }

            // Дополнительно ждем появления хотя бы 5 карточек (убеждаемся что загрузились все)
            await Task.Delay(2000, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при ожидании карточек: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Имитация поведения человека (скролл, движение мыши)
    /// </summary>
    private async Task SimulateHumanBehaviorAsync(CancellationToken cancellationToken)
    {
        try
        {
            var jsExecutor = (IJavaScriptExecutor)_driver;
            
            // Плавный скролл вниз
            var scrollHeight = (long)jsExecutor.ExecuteScript("return document.body.scrollHeight");
            int scrollSteps = 3;
            
            for (int i = 1; i <= scrollSteps; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                int scrollPosition = (int)(scrollHeight * i / scrollSteps);
                jsExecutor.ExecuteScript($"window.scrollTo(0, {scrollPosition})");
                
                // Случайная задержка между скроллами
                await Task.Delay(Random.Shared.Next(800, 1500), cancellationToken);
            }
            
            // Небольшая пауза перед обработкой
            await Task.Delay(Random.Shared.Next(1000, 2000), cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при имитации поведения: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработка текущей страницы - получение HTML и передача через Stream
    /// </summary>
    private async Task ProcessCurrentPageAsync(int pageNumber, CancellationToken cancellationToken)
    {
        try
        {
            string pageSource = _driver.PageSource;
            
            byte[] htmlBytes = Encoding.UTF8.GetBytes(pageSource);
            using var memoryStream = new MemoryStream(htmlBytes, writable: false);
            
            if(OnNewPage != null) 
                await OnNewPage.Invoke(memoryStream, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке страницы {pageNumber}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
            
        GC.SuppressFinalize(this);
    }
}