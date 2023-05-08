using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

public class AuctionMvcController : Controller
{
    private readonly HttpClient _httpClient;

    public AuctionMvcController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IActionResult> ActiveAuctions()
    {
        var activeAuctions = await _httpClient.GetFromJsonAsync<List<Auction>>("api/auctions/active");
        return View(activeAuctions);
    }
}