
namespace AddCorrectTable.Services
{
    public interface IMaterialService
    {
        Task<List<AggregatedMaterial>> GetAggregatedMaterialsAsync(DateTime date);
        Task<List<MaterialAggregatedCorrected>> GetCorrectedMaterialsAsync(DateTime date);
        Task<int> SaveCorrectedMaterialsAsync(IEnumerable<AggregatedMaterial> materials);
    }
}