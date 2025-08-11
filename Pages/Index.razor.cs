using Microsoft.JSInterop;

namespace AddCorrectTable.Pages
{
    public partial class Index
    {
        private List<AggregatedMaterial> Materials = new();
        private List<MaterialAggregatedCorrected> CorrectedMaterials = new();
        private DateTime SelectedDate = DateTime.Today;
        private DotNetObjectReference<Index> dotNetHelper; // Ссылка на наш компонент
        private bool showCorrectedTable = false; // По умолчанию таблица будет скрыта

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                dotNetHelper = DotNetObjectReference.Create(this);  // Создаем ссылку на этот компонент, чтобы передать ее в JS
                await JSRuntime.InvokeVoidAsync("initializeTableNavigation", dotNetHelper); // Инициализируем наш JS-обработчик
            }
        }

        private async Task LoadData()
        {
            Materials = await MaterialService.GetAggregatedMaterialsAsync(SelectedDate);
            CorrectedMaterials.Clear(); // Очищаем вторую таблицу при загрузке новых данных для правки

            await LoadCorrectedData(); // После успешного сохранения СРАЗУ ЖЕ обновляем вторую таблицу
        }

        // Новый метод для загрузки данных во вторую таблицу
        private async Task LoadCorrectedData()
        {
            CorrectedMaterials = await MaterialService.GetCorrectedMaterialsAsync(SelectedDate);
            await InvokeAsync(() => StateHasChanged());
        }

        private async Task Save()
        {
            // Отправляем на сохранение только измененные объекты
            var materialsToSave = Materials.Where(m => m.IsModified).ToList();

            if (materialsToSave.Any())
            {
                foreach (var mat in materialsToSave)
                {
                    mat.Date = SelectedDate;
                }

                var count = await MaterialService.SaveCorrectedMaterialsAsync(materialsToSave);
                Console.WriteLine($"Сохранено: {count} записей");

                // После успешного сохранения сбрасываем флаги у сохраненных объектов
                foreach (var mat in materialsToSave)
                {
                    mat.ResetModifiedState();
                }
                await InvokeAsync(() => StateHasChanged());
            }
        }

        // Этот метод будет вызываться из JavaScript
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
            dotNetHelper?.Dispose(); // Обязательно освобождаем ссылку при уничтожении компонента
        }
    }
}