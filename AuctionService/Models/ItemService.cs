using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class ItemService 
{
    public Guid Id { get; set; }

    public string Title { get; set; }
    public string Brand { get; set; }
    public string Description { get; set; }
  
    public decimal StartingPrice { get; set; }
    
    public List<ImageRecord> ImageHistory { get; set; }

    public string Category { get; set; }
    public string Location { get; set; }
}

