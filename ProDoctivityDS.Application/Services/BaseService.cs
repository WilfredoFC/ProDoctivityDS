using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Application.Services
{
    public class BaseServices<TEntity, Tdto> : IBaseService<TEntity, Tdto>
       where TEntity : class
       where Tdto : class
    {
        private readonly IMapper _mapper;
        private readonly IBaseRepository<TEntity> _repository;

        public BaseServices(IMapper mapper, IBaseRepository<TEntity> _baseRepository)
        {
            _mapper = mapper;
            _repository = _baseRepository;
        }

        public async Task<bool> DeleteHardDtoAsync(int dtoDelete)
        {
            try
            {
                await _repository.RemoveAsync(dtoDelete);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Tdto>> GetAllListDto()
        {
            try
            {
                var listEntity = await _repository.GetAllListAsync();
                var listDto = _mapper.Map<List<Tdto>>(listEntity);
                return listDto;
            }
            catch (Exception)
            {
                return [];
            }
        }

        public async Task<Tdto?> GetDtoById(int id)
        {
            try
            {
                var entity = await _repository.GetEntityByIdAsync(id);
                if (entity == null)
                {
                    return null;
                }

                Tdto? dto = _mapper.Map<Tdto>(entity);
                return dto;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Tdto>> GetWithInclude(List<string> properties)
        {
            try
            {
                var query = _repository.GetAllQueryWithInclude(properties);

                var result = await query.ProjectTo<Tdto>(_mapper.ConfigurationProvider).ToListAsync();

                return result;
            }
            catch
            {
                return [];
            }
        }


        public virtual async Task<Tdto?> SaveDtoAsync(Tdto dtoSave)
        {
            try
            {
                TEntity entity = _mapper.Map<TEntity>(dtoSave);
                TEntity? returnEntity = await _repository.SaveEntityAsync(entity);

                if (returnEntity == null)
                {
                    return null;
                }

                return _mapper.Map<Tdto>(returnEntity);
            }
            catch
            {
                return null;
            }
        }

        public async Task<Tdto?> UpdateDtoAsync(Tdto dtoUpdate, int id)
        {
            try
            {
                TEntity entity = _mapper.Map<TEntity>(dtoUpdate);
                TEntity? returnEntity = await _repository.UpdateEntityAsync(id, entity);
                if (returnEntity == null)
                {
                    return null;
                }

                return _mapper.Map<Tdto>(returnEntity);
            }
            catch
            {
                return null;
            }
        }
    }
}
