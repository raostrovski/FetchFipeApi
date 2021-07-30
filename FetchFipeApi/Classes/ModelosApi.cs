using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FetchFipeApi.Classes
{
    class ModelosApi
    {
        public ICollection<Modelo> modelos { get; set; }
        public ICollection<Ano> anos { get; set; }
    }
}
