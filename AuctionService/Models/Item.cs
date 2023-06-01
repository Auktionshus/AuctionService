using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Item
{
    public Guid Id { get; set; }
    public User Seller { get; set; }
    public string Title { get; set; }
    public string Brand { get; set; }
    public string Description { get; set; }
    public string ImageFileId { get; set; }
    public Category Category { get; set; }
    public string Location { get; set; }
}
