using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace AddCorrectTable.Pages
{
    public partial class Counter
    {
        private List<AggregatedMaterial> Materials = new();
        private DateTime StartDate = DateTime.Today;
        private DateTime EndDate = DateTime.Today;
        private DotNetObjectReference<Counter> dotNetHelper;            // Ссылка на наш компонент
        private bool isSaving = false;
        private bool isLoading = false;

        /// <summary>
        /// ВЫЧИСЛЯЕМОЕ СВОЙСТВО ДЛЯ ЗАГОЛОВКА
        /// </summary>
        private string PageTitle
        {
            get
            {
                if (StartDate.Date == EndDate.Date)
                {
                    return $"Материалы за {StartDate:yyyy-MM-dd}";
                }
                else
                {
                    return $"Материалы за период с {StartDate:yyyy-MM-dd} по {EndDate:yyyy-MM-dd}";
                }
            }
        }

        /// <summary>
        /// Standard method for component initialization.
        /// </summary>
        /// <param name="firstRender"></param>
        /// <returns></returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                dotNetHelper = DotNetObjectReference.Create(this);  // Создаем ссылку на этот компонент, чтобы передать ее в JS
                await JSRuntime.InvokeVoidAsync("initializeTableNavigation", dotNetHelper); // Инициализируем наш JS-обработчик
            }
        }

        /// <summary>
        /// Load a list of aggregated materials for the specified date range.
        /// </summary>
        /// <returns></returns>
        private async Task LoadData()
        {
            isLoading = true;
            Task.Delay(12000);
            try
            {
                Materials = await MaterialService.GetAggregatedMaterialsAsync(StartDate, EndDate);
            }
            catch (Exception ex)
            {
                // logget.log(ex) -> TODO
            }
            finally 
            {
                isLoading = false;
            }
        }

        /// <summary>
        /// Save the corrected materials to the database.
        /// </summary>
        /// <returns></returns>
        private async Task Save()
        {
            if (!Materials.Any()) return;
                var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userName = user.Identity?.IsAuthenticated == true ? user.Identity.Name : "Anonymous";

            // Отправляем на сохранение только измененные объекты
            //var Materials = Materials.Where(m => m.IsModified).ToList();

            isSaving = true;
            try
            {
                var count = await MaterialService.SaveCorrectedMaterialsAsync(Materials, StartDate, EndDate, userName);
                Console.WriteLine($"Сохранено: {count} записей");

                // После успешного сохранения сбрасываем флаги у сохраненных объектов
                foreach (var mat in Materials)
                {
                    mat.ResetModifiedState();
                }

                await LoadData();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                // logget.log(ex) -> TODO
            }
            finally 
            {
                isSaving = false;
            }
        }

        /// <summary>
        /// Move an arrow up and down through the input fields.
        /// </summary>
        /// <param name="currentId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [JSInvokable]
        public string? GetNextFocusElementId(string currentId, string key)
        {
            // "mass-input-5" -> 5
            var currentIndex = int.Parse(currentId.Split('-').Last());

            var nextIndex = -1;
            if (key == "ArrowDown" && currentIndex < Materials.Count - 1)
            {
                nextIndex = currentIndex + 1;
            }
            else if (key == "ArrowUp" && currentIndex > 0)
            {
                nextIndex = currentIndex - 1;
            }

            return nextIndex != -1 ? $"mass-input-{nextIndex}" : null;
        }

        public void Dispose()
        {
            dotNetHelper?.Dispose(); 
        }

    }
}