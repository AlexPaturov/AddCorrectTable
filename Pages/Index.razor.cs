using Microsoft.JSInterop;

namespace AddCorrectTable.Pages
{
    public partial class Index
    {
        private List<AggregatedMaterial> Materials = new();
        private List<MaterialAggregatedCorrected> CorrectedMaterials = new();
        private DateTime SelectedDate = DateTime.Today;
        private DotNetObjectReference<Index> dotNetHelper; // ������ �� ��� ���������
        private bool showCorrectedTable = false; // �� ��������� ������� ����� ������

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                dotNetHelper = DotNetObjectReference.Create(this);  // ������� ������ �� ���� ���������, ����� �������� �� � JS
                await JSRuntime.InvokeVoidAsync("initializeTableNavigation", dotNetHelper); // �������������� ��� JS-����������
            }
        }

        private async Task LoadData()
        {
            Materials = await MaterialService.GetAggregatedMaterialsAsync(SelectedDate);
            CorrectedMaterials.Clear(); // ������� ������ ������� ��� �������� ����� ������ ��� ������

            await LoadCorrectedData(); // ����� ��������� ���������� ����� �� ��������� ������ �������
        }

        // ����� ����� ��� �������� ������ �� ������ �������
        private async Task LoadCorrectedData()
        {
            CorrectedMaterials = await MaterialService.GetCorrectedMaterialsAsync(SelectedDate);
            await InvokeAsync(() => StateHasChanged());
        }

        private async Task Save()
        {
            // ���������� �� ���������� ������ ���������� �������
            var materialsToSave = Materials.Where(m => m.IsModified).ToList();

            if (materialsToSave.Any())
            {
                foreach (var mat in materialsToSave)
                {
                    mat.Date = SelectedDate;
                }

                var count = await MaterialService.SaveCorrectedMaterialsAsync(materialsToSave);
                Console.WriteLine($"���������: {count} �������");

                // ����� ��������� ���������� ���������� ����� � ����������� ��������
                foreach (var mat in materialsToSave)
                {
                    mat.ResetModifiedState();
                }
                await InvokeAsync(() => StateHasChanged());
            }
        }

        // ���� ����� ����� ���������� �� JavaScript
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
            dotNetHelper?.Dispose(); // ����������� ����������� ������ ��� ����������� ����������
        }
    }
}