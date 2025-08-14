
namespace AddCorrectTable.Services
{
    public interface IMaterialService
    {
        Task<List<AggregatedMaterial>> GetAggregatedMaterialsAsync(DateTime startDate, DateTime endDate);
        Task<int> SaveCorrectedMaterialsAsync(IEnumerable<AggregatedMaterial> materials, DateTime startDate, DateTime endDate, string? userName);
    }
}