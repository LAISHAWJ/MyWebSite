using MyWebsite.Core.Interfaces;
using MyWebsite.Core.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWebsite.Infrastructure.Repositories
{
    public class HobbyRepository : BaseRepository<Hobbies>, IHobbyRepository
    {
        public HobbyRepository(string connectionString) : base(connectionString) { }
    }
}
