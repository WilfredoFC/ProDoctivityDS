namespace ProDoctivityDS.Application.Interfaces
{
    public interface IBaseService<TEntity, TDto>
     where TEntity : class
     where TDto : class
    {
        Task<List<TDto>> GetAllListDto();
        Task<TDto?> GetDtoById(int id);
        Task<TDto?> UpdateDtoAsync(TDto dtoUpdate, int id);
        Task<TDto?> SaveDtoAsync(TDto dtoSave);
        Task<List<TDto>> GetWithInclude(List<string> properties);
        Task<bool> DeleteHardDtoAsync(int dtoDelete);


    }
}
