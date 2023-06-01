using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.Commons;

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

        public AuctionController(
            ILogger<AuctionController> logger,
            Environment secrets,
            IConfiguration config
        )
        {
            try
            {
                _hostName = config["HostnameRabbit"];
                _secret = secrets.dictionary["Secret"];
                _issuer = secrets.dictionary["Issuer"];
                _mongoDbConnectionString = secrets.dictionary["ConnectionString"];

                _logger = logger;
                _logger.LogInformation($"Secret: {_secret}");
                _logger.LogInformation($"Issuer: {_issuer}");
                _logger.LogInformation($"MongoDbConnectionString: {_mongoDbConnectionString}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error getting environment variables{e.Message}");
            }
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

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateAuction(AuctionDTO auction)
        {
            if (auction != null)
            {
                try
                {
                    // Connect to RabbitMQ
                    var factory = new ConnectionFactory { HostName = _hostName };

                    using var connection = factory.CreateConnection();
                    using var channel = connection.CreateModel();

                    channel.ExchangeDeclare(exchange: "topic_fleet", type: ExchangeType.Topic);

                    // Serialize to JSON
                    string message = JsonSerializer.Serialize(auction);

                    // Convert to byte-array
                    var body = Encoding.UTF8.GetBytes(message);

                    // Send to RabbitMQ
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
