using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FetchFipeApi.Classes
{
    public class Veiculo
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string FipeName { get; set; }
        public string Name { get; set; }
        public ICollection<Modelo> Modelos { get; set; }
    }
}
