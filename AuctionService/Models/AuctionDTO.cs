using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class AuctionDTO
{
    public Guid Item { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
}
