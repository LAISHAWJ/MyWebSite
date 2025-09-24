using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using MyWebsite.Core.Interfaces;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace MyWebsite.Infrastructure.Repositories
{
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly string _connectionString; 

        protected BaseRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public virtual IEnumerable<T> GetAll()
        {
            using (var conn = new SqlConnection(_connectionString)) 
            {
                conn.Open();
                return conn.Query<T>($"SELECT * FROM {typeof(T).Name}"); 
            }
        }
    }
}
