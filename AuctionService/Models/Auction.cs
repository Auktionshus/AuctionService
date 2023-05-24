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
    public string ImageFileId { get; set; }

    public string Category { get; set; }
    public string Location { get; set; }
}
