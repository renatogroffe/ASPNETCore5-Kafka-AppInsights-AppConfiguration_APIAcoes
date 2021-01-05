using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Confluent.Kafka;
using APIAcoes.Models;

namespace APIAcoes.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AcoesController : ControllerBase
    {
        private readonly ILogger<AcoesController> _logger;
        private readonly IConfiguration _configuration;

        public AcoesController(ILogger<AcoesController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        [ProducesResponseType(typeof(Resultado), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<Resultado> Post(Acao acao)
        {
            var cotacaoAcao = new CotacaoAcao()
            {
                Codigo = acao.Codigo,
                Valor = acao.Valor,
                CodCorretora = _configuration["Corretora:Codigo"],
                NomeCorretora = _configuration["Corretora:Nome"]
            };
            var conteudoAcao = JsonSerializer.Serialize(cotacaoAcao);
            _logger.LogInformation($"Dados: {conteudoAcao}");

            string topic = _configuration["ApacheKafka:Topic"];
            var configKafka = new ProducerConfig
            {
                BootstrapServers = _configuration["ApacheKafka:Broker"]
            };

            using (var producer = new ProducerBuilder<Null, string>(configKafka).Build())
            {
                var result = await producer.ProduceAsync(
                    topic,
                    new Message<Null, string>
                    { Value = conteudoAcao });

                _logger.LogInformation(
                    $"Apache Kafka - Envio para o tópico {topic} concluído | " +
                    $"{conteudoAcao} | Status: { result.Status.ToString()}");
            }

            return new Resultado()
            {
                Mensagem = "Informações de ação enviadas com sucesso!"
            };
        }        
    }
}