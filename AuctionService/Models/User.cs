using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class User
{
    public Guid Id { get; set; }
    public String FirstName { get; set; }
    public String LastName { get; set; }
    public String Email { get; set; }
    public Int32 MobilNummer { get; set; }
    public List<Bid> BidHistory { get; set; }
    public List<ImageRecord> ImageHistory { get; set; }

    public string Category { get; set; }
    public string Location { get; set; }


}