using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace AuctionService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionController : ControllerBase
    {
        private IGridFSBucket gridFS;
        private readonly ILogger<AuctionController> _logger;
        private readonly string _hostName;
        private readonly string _mongoDbConnectionString;

        public AuctionController(ILogger<AuctionController> logger, IConfiguration config)
        {
            _logger = logger;
            _mongoDbConnectionString = config["MongoDbConnectionString"];
            _hostName = config["HostnameRabbit"];
            _logger.LogInformation($"Connection: {_hostName}");
        }

        // Placeholder for the auction data storage
        private static readonly List<Auction> Auctions = new List<Auction>();

        // Image storage path
        private readonly string _imagePath = "Images";

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

        [HttpGet("{id}")]
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

        [HttpPost("uploadImage/{id}"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
        {
            MongoClient dbClient = new MongoClient(_mongoDbConnectionString);
            var database = dbClient.GetDatabase("auction");
            var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
            gridFS = new GridFSBucket(database);

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var fileName = id.ToString() + Path.GetExtension(file.FileName);

            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument { { "itemId", id.ToString() } }
            };

            var imageStream = file.OpenReadStream();
            var fileId = await gridFS.UploadFromStreamAsync(fileName, imageStream, options);
            imageStream.Close();

            var filter = Builders<Auction>.Filter.Eq(auction => auction.Id, id);
            var update = Builders<Auction>.Update.Set(
                auction => auction.ImageFileId,
                fileId.ToString()
            );

            await collection.UpdateOneAsync(filter, update);
            return Ok();
        }

        [HttpGet("image/{fileId}")]
        public async Task<IActionResult> GetImage(string fileId)
        {
            var fileStream = await gridFS.OpenDownloadStreamAsync(new ObjectId(fileId));

            if (fileStream == null)
            {
                return NotFound();
            }

            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return new FileContentResult(memoryStream.ToArray(), "image/jpeg");
        }
    }
}
