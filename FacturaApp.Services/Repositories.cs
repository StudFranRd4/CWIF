using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FacturaApp.Core.Models;
using FacturaApp.Data;

namespace FacturaApp.Services
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetAsync(Guid id);
        Task<T> CreateAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(Guid id);
    }

    public class EfRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly BillingDbContext _db;
        private readonly DbSet<T> _set;
        private readonly ILogger<EfRepository<T>> _logger;

        public EfRepository(BillingDbContext db, ILogger<EfRepository<T>> logger)
        {
            _db = db;
            _set = db.Set<T>();
            _logger = logger;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                return await _set.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAllAsync para {Type}", typeof(T).Name);
                throw;
            }
        }

        public async Task<T?> GetAsync(Guid id)
        {
            try
            {
                return await _set.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAsync para {Type} con Id {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public async Task<T> CreateAsync(T entity)
        {
            try
            {
                if (entity.Id == Guid.Empty)
                    entity.Id = Guid.NewGuid();

                _set.Add(entity);
                await _db.SaveChangesAsync();
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CreateAsync para {Type}", typeof(T).Name);
                throw;
            }
        }

        public async Task<T> UpdateAsync(T entity)
        {
            try
            {
                var existingEntity = await _set.FindAsync(entity.Id);
                if (existingEntity == null)
                    throw new ArgumentException($"Entidad con Id {entity.Id} no encontrada");

                _db.Entry(existingEntity).CurrentValues.SetValues(entity);
                await _db.SaveChangesAsync();
                return existingEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UpdateAsync para {Type} con Id {Id}", typeof(T).Name, entity.Id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var entity = await _set.FindAsync(id);
                if (entity != null)
                {
                    _set.Remove(entity);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DeleteAsync para {Type} con Id {Id}", typeof(T).Name, id);
                throw;
            }
        }
    }
}
