using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWebsite.Core.Interfaces
{
    public interface IGeneratorService
    {
        void GenerateWebsite(string outputDir);
    }
}
