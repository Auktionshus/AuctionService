using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RabbitMQ.Client;
using MongoDB.Driver;

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

        private MongoClient dbClient;

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

                // Connect to MongoDB
                dbClient = new MongoClient(_mongoDbConnectionString);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error getting environment variables{e.Message}");
            }
        }

        /// <summary>
        /// Creates an auction
        /// </summary>
        /// <param name="auction">AuctionDTO</param>
        /// <returns>The created auction</returns>
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateAuction(AuctionDTO auction)
        {
            if (auction != null)
            {
                // Check if Item exists
                Item item = null;
                try
                {
                    var itemCollection = dbClient.GetDatabase("Items").GetCollection<Item>("Item");
                    item = itemCollection.Find(i => i.Id == auction.Item).FirstOrDefault();
                    _logger.LogInformation($" [x] Received item with id: {item.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while querying the item collection: {ex}");
                }

                if (item == null)
                {
                    return NotFound($"Item with Id {auction.Item} not found.");
                }
                else
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
                }
                return Ok(auction);
            }
            else
            {
                return BadRequest("Auction object is null");
            }
        }

        /// <summary>
        /// Lists all auctions
        /// </summary>
        /// <returns>A list of auctions</returns>
        [HttpGet("list")]
        public async Task<IActionResult> ListAuctions()
        {
            try
            {
                _logger.LogInformation("Listing all auctions");
                var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
                var auctions = await collection.Find(_ => true).ToListAsync();
                return Ok(auctions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Gets auction from id
        /// </summary>
        /// <param name="id">Auction id</param>
        /// <returns>An auction</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuction(Guid id)
        {
            try
            {
                _logger.LogInformation($"Getting auction with id: {id}");
                var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
                Auction auction = await collection.Find(a => a.Id == id).FirstOrDefaultAsync();

                if (auction == null)
                {
                    return NotFound($"Auction with Id {id} not found.");
                }
                return Ok(auction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Gets the version information of the service
        /// </summary>
        /// <returns>A list of version information</returns>
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
