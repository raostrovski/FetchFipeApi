namespace FetchFipeApi.Classes
{
    public class Marca
    {
        public string nome { get; set; }
        public string codigo { get; set; }

        public Marca()
        {

        }

        public Marca(string snome, string scodigo)
        {
            nome = snome;
            codigo = scodigo;
        }
    }
}
