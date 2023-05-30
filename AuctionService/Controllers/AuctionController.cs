using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;

namespace AuctionService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionController : ControllerBase
    {
        private readonly ILogger<AuctionController> _logger;
        private readonly string _hostName;
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _mongoDbConnectionString;

        public AuctionController(ILogger<AuctionController> logger, IConfiguration config)
        {
            _mongoDbConnectionString = config["MongoDbConnectionString"];
            _hostName = config["HostnameRabbit"];
            _secret = config["Secret"];
            _issuer = config["Issuer"];

            _logger = logger;
            _logger.LogInformation($"Connection: {_hostName}");
        }

        // Placeholder for the auction data storage
        private static readonly List<Auction> Auctions = new List<Auction>();

        // Image storage path
        private readonly string _imagePath = "Images";

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAuth()
        {
            return Ok("You're authorized");
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAuction(Auction auction)
        {
            if (auction != null)
            {
                try
                {
                    // Opretter forbindelse til RabbitMQ
                    var factory = new ConnectionFactory { HostName = _hostName };

                    using var connection = factory.CreateConnection();
                    using var channel = connection.CreateModel();

                    channel.ExchangeDeclare(exchange: "topic_fleet", type: ExchangeType.Topic);

                    // Serialiseres til JSON
                    string message = JsonSerializer.Serialize(auction);

                    // Konverteres til byte-array
                    var body = Encoding.UTF8.GetBytes(message);

                    // Sendes til kø
                    channel.BasicPublish(
                        exchange: "topic_fleet",
                        routingKey: "auctions.create",
                        basicProperties: null,
                        body: body
                    );

                    _logger.LogInformation("Auction created and sent to RabbitMQ");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return StatusCode(500);
                }
                return Ok(auction);
            }
            else
            {
                return BadRequest("Auction object is null");
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListAuctions()
        {
            MongoClient dbClient = new MongoClient(_mongoDbConnectionString);
            var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
            var auctions = await collection.Find(_ => true).ToListAsync();
            return Ok(auctions);
        }

        [HttpGet("Auction/{id}")]
        public async Task<IActionResult> GetAuction(Guid id)
        {
            MongoClient dbClient = new MongoClient(_mongoDbConnectionString);
            var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
            Auction auction = await collection.Find(a => a.Id == id).FirstOrDefaultAsync();

            if (auction == null)
            {
                return NotFound($"Auction with Id {id} not found.");
            }
            return Ok(auction);
        }
        [HttpGet("version")]
        public IEnumerable<string> Get()
        {
            var properties = new List<string>();
            var assembly = typeof(Program).Assembly;
            foreach (var attribute in assembly.GetCustomAttributesData())
            {
                _logger.LogInformation("Tilføjer " + attribute.AttributeType.Name);
                properties.Add($"{attribute.AttributeType.Name} - {attribute.ToString()}");
            }
            return properties;

        }
    }
}