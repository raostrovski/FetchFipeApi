using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FetchFipeApi.Classes;
using Newtonsoft.Json;
using OfficeOpenXml;

namespace FetchFipeApi
{
    class Program
    {

        private static readonly HttpClient Client = new HttpClient();
        private static DirectoryInfo _diretorio;
        private static DirectoryInfo Diretorio
        {
            get => _diretorio;
            set
            {
                _diretorio = value;
                if (!_diretorio.Exists)
                    _diretorio.Create();
            }
        }
        private static string TipoVeiculo { get; set; }

        #region GetApi
        private static async Task<T> GetApi<T>(string url)
        {
            HttpResponseMessage response = await Client.GetAsync(url);
            T t = default;

            try
            {
                response.EnsureSuccessStatusCode();
                t = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ocorreu um erro na thread: {Thread.CurrentThread.ManagedThreadId}");
            }
            return t;
        }

        private static async Task<IEnumerable<Marca>> GetMarca() =>
            await GetApi<IEnumerable<Marca>>($"{TipoVeiculo}/marcas");

        private static async Task<ModelosApi> GetModelos(Marca marca) =>
            await GetApi<ModelosApi>($"{TipoVeiculo}/marcas/{marca.codigo}/modelos");


        private static async Task<IEnumerable<Ano>> GetAnos(Marca marca, Modelo modelo) =>
            await GetApi<IEnumerable<Ano>>($"{TipoVeiculo}/marcas/{marca.codigo}/modelos/{modelo.codigo}/anos");


        private static async Task<DadosVeiculo> GetDadoVeiculo(Marca marca, Modelo modelo, Ano ano) =>
            await GetApi<DadosVeiculo>($"{TipoVeiculo}/marcas/{marca.codigo}/modelos/{modelo.codigo}/anos/{ano.codigo}");


        #endregion
        private static async Task AcessarFipeApi()
        {
            IEnumerable<Marca> marcas = await GetMarca();
            if (marcas.Any())
                Task.WaitAll(marcas.Select(PercorrerModelos).ToArray());
        }

        private static async Task PercorrerModelos(Marca marca)
        {
            PrintNotificação($"Iniciando busca de modelos: {marca.nome} em Thread: {Thread.CurrentThread.ManagedThreadId}");
            ModelosApi modelos = await GetModelos(marca);

            if (modelos == null) return;

            foreach (Modelo modelo in modelos.modelos)
            {
                if (modelo is null) continue;
                await PercorrerAnos(marca, modelo);
            }
            PrintNotificação($"Saindo Thread: {Thread.CurrentThread.ManagedThreadId}...");
        }

        private static async Task PercorrerAnos(Marca marca, Modelo modelo)
        {
            PrintNotificação($"Iniciando busca de anos: {marca.nome} ano {modelo.nome} em Thread: {Thread.CurrentThread.ManagedThreadId}");
            IEnumerable<Ano> anos = await GetAnos(marca, modelo);
            List<DadosVeiculo> dadosVeiculos = new List<DadosVeiculo>();

            if (anos is null || !anos.Any()) return;

            foreach (Ano ano in anos)
            {
                if (ano is null) continue;
                DadosVeiculo dado = await GetDadoVeiculo(marca, modelo, ano);
                dadosVeiculos.Add(dado);
                //PrintDadoVeiculo(dado);
            }
            GravarExcel(dadosVeiculos);
        }

        private static void GravarExcel(IEnumerable<DadosVeiculo> dadosVeiculos)
        {
            DadosVeiculo ex = dadosVeiculos.First(x => x != null);
            string modelo = CorrigirStringParaNomeArquivo(ex.Modelo);
            PrintNotificação($"Iniciando criação do Excel do arquivo: {ex.Marca} {modelo} em thread {Thread.CurrentThread.ManagedThreadId}");

            string fileName = $"{ex.CodigoFipe} - {ex.Marca} - {modelo}.xlsx";
            FileInfo f = GetFileInfo(fileName);
            using ExcelPackage package = new ExcelPackage();
            ExcelWorksheet worksheet;

            if (f.Exists)
            {
                worksheet = package.Workbook.Worksheets[0];
            }
            else
            {
                worksheet = package.Workbook.Worksheets.Add("Caminhões");
                worksheet.Cells[1, 1].Value = "CodigoFipe";
                worksheet.Cells[1, 2].Value = "Marca";
                worksheet.Cells[1, 3].Value = "Modelo";
                worksheet.Cells[1, 4].Value = "AnoModelo";
                worksheet.Cells[1, 5].Value = "Valor";
                worksheet.Cells[1, 6].Value = "Combustivel";
                worksheet.Cells[1, 7].Value = "MesReferencia";
                worksheet.Cells[1, 8].Value = "TipoVeiculo";
                worksheet.Cells[1, 9].Value = "SiglaCombustivel";
            }

            int row = 2;
            foreach (DadosVeiculo dado in dadosVeiculos)
            {
                if (dado is null) continue;
                worksheet.Cells[row, 1].Value = dado.CodigoFipe;
                worksheet.Cells[row, 2].Value = dado.Marca;
                worksheet.Cells[row, 3].Value = dado.Modelo;
                worksheet.Cells[row, 4].Value = dado.AnoModelo;
                worksheet.Cells[row, 5].Value = dado.Valor;
                worksheet.Cells[row, 6].Value = dado.Combustivel;
                worksheet.Cells[row, 7].Value = dado.MesReferencia;
                worksheet.Cells[row, 8].Value = dado.TipoVeiculo;
                worksheet.Cells[row, 9].Value = dado.SiglaCombustivel;
                ++row;
            }

            package.SaveAs(f);
            PrintNotificação($"Finalizado excel em thread {Thread.CurrentThread.ManagedThreadId}");
        }

        #region Diretorio
        public static DirectoryInfo GetRootDirectory()
        {
            string currentDir = AppContext.BaseDirectory;
            DirectoryInfo dir = new DirectoryInfo(currentDir);
#if DEBUG
            while (!currentDir.EndsWith("bin"))
            {
                currentDir = Directory.GetParent(currentDir).FullName.TrimEnd('\\');
            }
            dir = new DirectoryInfo(currentDir).Parent;
#endif
            return dir;
        }

        private static FileInfo GetFileInfo(string file)
        {
            return new FileInfo(Path.Combine(Diretorio.FullName, file));
        }
        #endregion

        private static async Task Main(string[] args)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            Diretorio = new DirectoryInfo($"{GetRootDirectory().FullName}\\Excel");
            Client.BaseAddress = new Uri("https://parallelum.com.br/fipe/api/v1/");

            PrintNotificação("Valores possíveis para tipo veiculos: carros, motos ou caminhoes");
            TipoVeiculo = Console.ReadLine();
            await AcessarFipeApi();
        }

        private static void PrintNotificação(string notificacao) =>
            Console.WriteLine(notificacao);

        private static string CorrigirStringParaNomeArquivo(string arq) =>
            arq.Replace('<', ' ').Replace('>', ' ').Replace((char)92, ' ')
                .Replace('/', ' ').Replace('|', ' ').Replace('?', ' ').Replace('*', ' ')
                .Replace(':', ' ').Replace('"', ' ');

        private static void PrintDadoVeiculo(DadosVeiculo dado) =>
            PrintNotificação($"Valor: {dado.Valor}\n" +
                              $"Marca: {dado.Marca}\n" +
                              $"Modelo: {dado.Modelo}\n" +
                              $"AnoModelo: {dado.AnoModelo}\n" +
                              $"Combustivel: {dado.Combustivel}\n" +
                              $"CodigoFipe: {dado.CodigoFipe}\n" +
                              $"MesReferencia: {dado.MesReferencia}\n" +
                              $"TipoVeiculo: {dado.TipoVeiculo}\n" +
                              $"SiglaCombustivel: {dado.SiglaCombustivel}\n");
    }
}