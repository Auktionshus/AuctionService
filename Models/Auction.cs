using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Auction
{
    public Guid Id { get; set; }

    public string Title { get; set; }
    public string Brand { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public List<Bid> BidHistory { get; set; }
    public List<ImageRecord> ImageHistory { get; set; }

    public string Category { get; set; }
    public string Location { get; set; }

    public Auction(
        Guid id,
        string title,
        string brand,
        string description,
        DateTime startTime,
        DateTime endTime,
        decimal startingPrice,
        decimal currentPrice,
        List<Bid> bidHistory,
        List<ImageRecord> imageHistory,
        string category,
        string location
    )
    {
        this.Id = id;
        this.Title = title;
        this.Brand = brand;
        this.Description = description;
        this.StartTime = startTime;
        this.EndTime = endTime;
        this.StartingPrice = startingPrice;
        this.CurrentPrice = currentPrice;
        this.BidHistory = bidHistory;
        this.ImageHistory = imageHistory;
        this.Category = category;
        this.Location = location;
    }
}
