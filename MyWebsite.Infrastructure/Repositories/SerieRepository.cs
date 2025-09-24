using MyWebsite.Core.Entidades;
using MyWebsite.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWebsite.Infrastructure.Repositories
{
    public class SerieRepository : BaseRepository<Series>, ISerieRepository
    {
        public SerieRepository(string connectionString) : base(connectionString) { }
    }
}
