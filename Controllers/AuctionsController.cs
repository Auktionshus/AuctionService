using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Auktionshus.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : ControllerBase
    {
        // Placeholder for the auction data storage
        private static readonly List<Auction> Auctions = new List<Auction>();

        // Image storage path
        private readonly string _imagePath = "Images";

        [HttpPost("create")]
        public async Task<IActionResult> CreateAuction(Auction auction)
        {
            if (auction == null)
            {
                return BadRequest("Auction object is null");
            }

            auction.Id = Guid.NewGuid();
            auction.BidHistory = new List<Bid>();
            auction.ImageHistory = new List<ImageRecord>();

            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
            await collection.InsertOneAsync(auction);
            return Ok(auction);
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListAuctions()
        {
            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
            var auctions = await collection.Find(_ => true).ToListAsync();
            return Ok(auctions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuction(Guid id)
        {
            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
            Auction auction = await collection.Find(a => a.Id == id).FirstOrDefaultAsync();

            if (auction == null)
            {
                return NotFound($"Auction with Id {id} not found.");
            }
            return Ok(auction);
        }

        [HttpGet("{id}/listImages")]
        public IActionResult ListImages(Guid id)
        {
            Auction auction = Auctions.FirstOrDefault(a => a.Id == id);
            if (auction == null)
            {
                return NotFound($"Auction with Id {id} not found.");
            }
            return Ok(auction.ImageHistory);
        }

        [HttpPost("uploadImage/{id}"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadImage(Guid id)
        {
            if (!Directory.Exists(_imagePath))
            {
                Directory.CreateDirectory(_imagePath);
            }

            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
            var filter = Builders<Auction>.Filter.Eq(a => a.Id, id);
            Auction auction = await collection.Find(filter).FirstOrDefaultAsync();

            if (auction == null)
            {
                return NotFound($"Auction with Id {id} not found.");
            }

            if (auction.ImageHistory == null)
            {
                auction.ImageHistory = new List<ImageRecord>();
            }

            try
            {
                foreach (var formFile in Request.Form.Files)
                {
                    // Validate file type and size
                    if (formFile.ContentType != "image/jpeg" && formFile.ContentType != "image/png")
                    {
                        return BadRequest(
                            $"Invalid file type for file {formFile.FileName}. Only JPEG and PNG files are allowed."
                        );
                    }
                    if (formFile.Length > 1048576) // 1MB
                    {
                        return BadRequest(
                            $"File {formFile.FileName} is too large. Maximum file size is 1MB."
                        );
                    }
                    if (formFile.Length > 0)
                    {
                        var fileName = "image-" + Guid.NewGuid().ToString() + ".jpg";
                        var fullPath = _imagePath + Path.DirectorySeparatorChar + fileName;

                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            formFile.CopyTo(stream);
                        }

                        var imageURI = new Uri(fileName, UriKind.RelativeOrAbsolute);
                        var imageRecord = new ImageRecord
                        {
                            Id = Guid.NewGuid(),
                            Location = imageURI,
                            Date = DateTime.UtcNow,
                            // Add other properties like Description and AddedBy as needed
                        };

                        auction.ImageHistory.Add(imageRecord);
                        var update = Builders<Auction>.Update.Push(
                            a => a.ImageHistory,
                            imageRecord
                        );
                        await collection.UpdateOneAsync(filter, update);
                    }
                    else
                    {
                        return BadRequest("Empty file submitted.");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }

            return Ok("Image(s) uploaded successfully.");
        }

        [HttpPost("{id}/placeBid")]
        public async Task<IActionResult> PlaceBid(Guid id, [FromBody] Bid bid)
        {
            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
            Auction auction = await collection.Find(a => a.Id == id).FirstOrDefaultAsync();

            if (auction == null)
            {
                return NotFound($"Auction with Id {id} not found.");
            }

            if (auction.BidHistory == null)
            {
                auction.BidHistory = new List<Bid>();
            }

            if (bid.Amount <= auction.CurrentPrice)
            {
                return BadRequest(
                    $"Bid amount must be higher than {auction.CurrentPrice} the current price."
                );
            }

            bid.Id = Guid.NewGuid();
            bid.Date = DateTime.UtcNow;
            auction.BidHistory.Add(bid);
            auction.CurrentPrice = bid.Amount;

            var update = Builders<Auction>.Update
                .Set(a => a.CurrentPrice, bid.Amount)
                .Push(a => a.BidHistory, bid);

            await collection.UpdateOneAsync(a => a.Id == id, update);

            return CreatedAtAction(nameof(GetAuction), new { id = id }, auction);
        }

        // ... (alle dine eksisterende metoder)

        [HttpPost("filter")]
        public async Task<IActionResult> FilteredAuctions([FromBody] FilterModel filter)
        {
            MongoClient dbClient = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var collection = dbClient.GetDatabase("auction").GetCollection<Auction>("auctions");
            var auctions = await collection.Find(_ => true).ToListAsync();

            if (!string.IsNullOrEmpty(filter.Category))
            {
                auctions = auctions.Where(a => a.Category == filter.Category).ToList();
            }

            if (!string.IsNullOrEmpty(filter.Location))
            {
                auctions = auctions.Where(a => a.Location == filter.Location).ToList();
            }

            if (filter.MinPrice.HasValue)
            {
                auctions = auctions.Where(a => a.CurrentPrice >= filter.MinPrice.Value).ToList();
            }

            if (filter.MaxPrice.HasValue)
            {
                auctions = auctions.Where(a => a.CurrentPrice <= filter.MaxPrice.Value).ToList();
            }

            if (filter.DateFrom.HasValue)
            {
                auctions = auctions.Where(a => a.StartTime >= filter.DateFrom.Value).ToList();
            }

            if (filter.DateTo.HasValue)
            {
                auctions = auctions.Where(a => a.EndTime <= filter.DateTo.Value).ToList();
            }

            return Ok(auctions);
        }
    }
}
