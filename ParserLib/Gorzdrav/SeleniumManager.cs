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

        options.AddArgument("--headless=new");
        
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);
        
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        
        options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
        
        options.AddArgument("--window-size=1920,1080");

        options.AddArguments($@"--user-agent={UserAgentRotator.GetRandom()}");
        // options.AddArguments($"--proxy-server={ProxyRotator.GetRandomString()}"); // ERROR: не работает что-то
        
        _driver = new ChromeDriver(options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        
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
            _driver.Navigate().GoToUrl(baseUrl);
            int page = 1;

            await WaitForPaginationToLoad(cancellationToken);

            int totalPages = GetTotalPages();

            Console.WriteLine($"Начало парсинга. Всего страниц: {totalPages}");

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"Обработка страницы {page}...");
                          
                if (!await WaitForCardsToLoad(cancellationToken))
                {
                    Console.WriteLine($"Не удалось загрузить карточки на странице {page}");
                    break;
                }
                
                await SimulateHumanBehaviorAsync(cancellationToken);
                
                await ProcessCurrentPageAsync(page, cancellationToken);
                
                if (page < totalPages)
                {
                    await Task.Delay(Random.Shared.Next(2000, 5000), cancellationToken);
                    bool moved = await NavigateNextPage(page, cancellationToken);
                    if (!moved) break;
                }
                
                page++;

            }
            while (page <= totalPages);

            Console.WriteLine("Парсинг завершён");
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

    /// <summary>
    /// Получение общего количества страниц. Т.е. просто номер последней страницы
    /// </summary>
    /// <returns></returns>
    private int GetTotalPages()
    {
        try
        {
            var paginationLinks = _driver.FindElements(By.CssSelector("ul.ui-table-pagination__pages-list a"));
            if (paginationLinks.Any())
            {
                var lastLink = paginationLinks.Last();
                string lastPageText = lastLink.Text.Trim();
                
                if (int.TryParse(lastPageText, out int lastPage))
                {
                    Console.WriteLine($"Найдено страниц: {lastPage}");
                    return lastPage;
                }
            }

            var nextBtn = _driver.FindElements(By.CssSelector("#__nuxt > div.layout > div.catalog.catalog--theme--gz > div.block-container.block-container--with-paddings.block-container--desktop.block-container--theme--gz > div > div.catalog-page__content > div.ui-table-pagination.ui-table-pagination--theme--gz.catalog-list__pagination.catalog-list__pagination--theme--gz > ul > li > div"));
            if (nextBtn.Any())
            {
                Console.WriteLine("Пагинация найдена, но количество страниц неизвестно - будет парситься до конца");
                return int.MaxValue;
            }

            Console.WriteLine("Пагинация не найдена - парсим только первую страницу");
            return 1; // NOTE: default 1 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при определении количества страниц: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Переход на конкретную страницу пагинации
    /// </summary>
    /// <param name="pageNumber">Номер страницы для перехода</param>
    public async Task<bool> NavigateNextPage(int? pageNumber = null, CancellationToken cancellationToken = default)
    {
        try
        {            
            if (pageNumber != null)
            {
                var pageLinks = _driver.FindElements(By.CssSelector("ul.ui-table-pagination__pages-list a"));
                foreach (var link in pageLinks)
                {
                    string linkText = link.Text.Trim();
                    
                    if (string.IsNullOrEmpty(linkText) || linkText == "...") 
                        continue;
                    
                    if (int.TryParse(linkText, out int linkPage) && linkPage == pageNumber + 1)
                    {
                        ((IJavaScriptExecutor)_driver).ExecuteScript(
                            "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", link);
                        
                        await Task.Delay(500, cancellationToken);
                        
                        link.Click();
                                            
                        Console.WriteLine($"Перешли на страницу {pageNumber + 1}");
                        return true;
                    }
                }
            }

            var nextBtn = _driver.FindElements(By.CssSelector("#__nuxt > div.layout > div.catalog.catalog--theme--gz > div.block-container.block-container--with-paddings.block-container--desktop.block-container--theme--gz > div > div.catalog-page__content > div.ui-table-pagination.ui-table-pagination--theme--gz.catalog-list__pagination.catalog-list__pagination--theme--gz > ul > li > div"));
            if (nextBtn.Any())
            {
                nextBtn.Last().Click();
                return true;
            }

            Console.WriteLine("Больше страниц нет или совсем ничего не нашлось");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при переходе на страницу {pageNumber}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ожидание загрузки/отрисовки пагинации
    /// </summary>
    private async Task<bool> WaitForPaginationToLoad(CancellationToken cancellationToken)
    {
        try
        {
            var paginationSelectors = new By[]
            {
                By.CssSelector("ul.ui-table-pagination__pages-list"),
                By.CssSelector("[class*='pagination']"),
                By.CssSelector("ul.pagination"),
                By.CssSelector("div.pagination-wrapper")
            };

            bool anyPaginationFound = false;
            foreach (var selector in paginationSelectors)
            {
                try
                {
                    await Task.Run(() => 
                    {
                        return _wait.Until(driver => 
                            driver.FindElements(selector).Count > 0
                        );
                    }, cancellationToken);
                    
                    anyPaginationFound = true;
                    Console.WriteLine($"Пагинация загрузилась");
                    break;
                }
                catch (WebDriverTimeoutException)
                {
                    continue;
                }
            }

            if (!anyPaginationFound)
            {
                Console.WriteLine("Пагинация не загрузилась");
                return false;
            }
                       
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при ожидании пагинации: {ex.Message}");
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
            var cardSelectors = new By[]
            {
                By.CssSelector("div.product-card.product-card--grid.product-card--theme--gz"),
                By.CssSelector(".product-card"),
                By.CssSelector("[class*='product-card']")
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

            await Task.Delay(2000, cancellationToken); // NOTE: не приятно но ладно
            
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
            
            var scrollHeight = (long)(jsExecutor.ExecuteScript("return document.body.scrollHeight") ?? throw new NullReferenceException());
            int scrollSteps = 3;
            
            for (int i = 1; i <= scrollSteps; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                int scrollPosition = (int)(scrollHeight * i / scrollSteps);
                jsExecutor.ExecuteScript($"window.scrollTo(0, {scrollPosition})");
                
                // NOTE: Случайная задержка между скроллами
                await Task.Delay(Random.Shared.Next(800, 1500), cancellationToken);
            }
            
            // NOTE: Небольшая пауза перед обработкой
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