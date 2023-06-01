//using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class User
{
    public Guid Id { get; set; }
    public String FirstName { get; set; }
    public String LastName { get; set; }
    public String Email { get; set; }
    public byte[] PasswordHash { get; set; }
    public byte[] PasswordSalt { get; set; }
}
