using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.Http.Json;
using Newtonsoft.Json;
using Xunit;
using FluentAssertions;
using BertServer;
using BertServer.Controllers;
using static System.Net.Mime.MediaTypeNames;
using System.Web;
using Microsoft.AspNetCore.WebUtilities;

public class BertControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    readonly WebApplicationFactory<Program> application;

    public BertControllerTests(WebApplicationFactory<Program> application)
    {
        this.application = application;
    }

    [Fact]
    public async Task POSTManyTexts()
    {
        var client = application.CreateClient();
        var postResponse0 = await client.PostAsJsonAsync("https://localhost:7064/BertAnalyzer", "Gibberish 0");
        postResponse0.EnsureSuccessStatusCode();
        var postResponse1 = await client.PostAsJsonAsync("https://localhost:7064/BertAnalyzer", "Gibberish 1");
        postResponse1.EnsureSuccessStatusCode();
        var postResponse2 = await client.PostAsJsonAsync("https://localhost:7064/BertAnalyzer", "Gibberish 2");
        postResponse2.EnsureSuccessStatusCode();

        string Id0 = await postResponse0.Content.ReadAsStringAsync();
        string Id1 = await postResponse1.Content.ReadAsStringAsync();
        string Id2 = await postResponse2.Content.ReadAsStringAsync();
        Id0.Should().NotBeSameAs(Id1);
        Id0.Should().NotBeSameAs(Id2);
        Id1.Should().NotBeSameAs(Id2);
    }
    
    [Fact]
    public async Task POSTTextGetAnswer()
    {
        var client = application.CreateClient();
        string text = "In a hole in the ground there lived a hobbit. Not a nasty, dirty, wet hole, filled with the ends of worms and an oozy smell, nor yet a dry, bare, sandy hole with nothing in it to sit down on or to eat: it was a hobbit-hole, and that means comfort. It  had  a  perfectly  round  door  like  a  porthole,  painted  green,  with  a  shiny yellow  brass  knob  in  the  exact middle. The  door  opened  on  to  a  tube-shaped hall like  a  tunnel:  a  very  comfortable  tunnel without  smoke, with  panelled walls,  and floors  tiled  and  carpeted,  provided with  polished  chairs,  and  lots  and  lots of pegs for  hats  and  coats - the hobbit was fond of visitors. The tunnel wound on and on, going fairly but not quite straight into the  side  of  the  hill  -  The  Hill, as all the people for many miles round called  it - and many little round doors opened out of it,  first  on  one  side  and  then  on  another.  No  going  upstairs  for  the  hobbit: bedrooms,  bathrooms,  cellars,  pantries  (lots  of  these),  wardrobes  (he  had  whole rooms devoted to clothes), kitchens, dining-rooms, all were on the same floor, and indeed  on  the  same  passage. The  best  rooms were  all  on  the  left-hand side (going in),  for  these  were  the  only  ones  to  have  windows,  deep-set  round  windows looking over his garden and meadows beyond, sloping down to the river.";
        var postResponse0 = await client.PostAsJsonAsync("https://localhost:7064/BertAnalyzer", text);
        postResponse0.EnsureSuccessStatusCode();

        string Id0 = await postResponse0.Content.ReadAsStringAsync();
        Id0.Should().NotBeNull();

        var query = new Dictionary<string, string>()
        {
            ["textId"] = Id0,
            ["question"] = "Where did hobbit live?"
        };
        var uri = QueryHelpers.AddQueryString("https://localhost:7064/BertAnalyzer", query);

        var getResponse0 = await client.GetAsync(uri);
        getResponse0.EnsureSuccessStatusCode();
        var answer0 = await getResponse0.Content.ReadAsStringAsync();
        answer0.Should().NotBeNull();
    }
    [Fact]
    public async Task POSTEmptyText()
    {
        var client = application.CreateClient();
        string text = "";
        var postResponse0 = await client.PostAsJsonAsync("https://localhost:7064/BertAnalyzer", text);
        int responseCode = (int)postResponse0.StatusCode;
        responseCode.Should().Be(400);
        string responseText = await postResponse0.Content.ReadAsStringAsync();
        responseText.Should().Be("No text found in request.");
    }
    [Fact]
    public async Task GETNoTextId()
    {
        var client = application.CreateClient();

        var query = new Dictionary<string, string>()
        {
            ["textId"] = " ",
            ["question"] = "Where did hobbit live?"
        };
        var uri = QueryHelpers.AddQueryString("https://localhost:7064/BertAnalyzer", query);

        var getResponse0 = await client.GetAsync(uri);
        int responseCode = (int)getResponse0.StatusCode;
        responseCode.Should().Be(400);
        string responseText = await getResponse0.Content.ReadAsStringAsync();
    }
    [Fact]
    public async Task GETNonExistentTextId()
    {
        var client = application.CreateClient();

        var query = new Dictionary<string, string>()
        {
            ["textId"] = int.MaxValue.ToString(),
            ["question"] = "Where did hobbit live?"
        };
        var uri = QueryHelpers.AddQueryString("https://localhost:7064/BertAnalyzer", query);

        var getResponse0 = await client.GetAsync(uri);
        int responseCode = (int)getResponse0.StatusCode;
        responseCode.Should().Be(400);
        string responseText = await getResponse0.Content.ReadAsStringAsync();
    }
    [Fact]
    public async Task GETNoQuestion()
    {
        var client = application.CreateClient();
        string text = "In a hole in the ground there lived a hobbit. Not a nasty, dirty, wet hole, filled with the ends of worms and an oozy smell, nor yet a dry, bare, sandy hole with nothing in it to sit down on or to eat: it was a hobbit-hole, and that means comfort. It  had  a  perfectly  round  door  like  a  porthole,  painted  green,  with  a  shiny yellow  brass  knob  in  the  exact middle. The  door  opened  on  to  a  tube-shaped hall like  a  tunnel:  a  very  comfortable  tunnel without  smoke, with  panelled walls,  and floors  tiled  and  carpeted,  provided with  polished  chairs,  and  lots  and  lots of pegs for  hats  and  coats - the hobbit was fond of visitors. The tunnel wound on and on, going fairly but not quite straight into the  side  of  the  hill  -  The  Hill, as all the people for many miles round called  it - and many little round doors opened out of it,  first  on  one  side  and  then  on  another.  No  going  upstairs  for  the  hobbit: bedrooms,  bathrooms,  cellars,  pantries  (lots  of  these),  wardrobes  (he  had  whole rooms devoted to clothes), kitchens, dining-rooms, all were on the same floor, and indeed  on  the  same  passage. The  best  rooms were  all  on  the  left-hand side (going in),  for  these  were  the  only  ones  to  have  windows,  deep-set  round  windows looking over his garden and meadows beyond, sloping down to the river.";
        var postResponse0 = await client.PostAsJsonAsync("https://localhost:7064/BertAnalyzer", text);
        postResponse0.EnsureSuccessStatusCode();

        string Id0 = await postResponse0.Content.ReadAsStringAsync();
        Id0.Should().NotBeNull();

        var query = new Dictionary<string, string>()
        {
            ["textId"] = Id0,
            ["question"] = ""
        };
        var uri = QueryHelpers.AddQueryString("https://localhost:7064/BertAnalyzer", query);

        var getResponse0 = await client.GetAsync(uri);
        int responseCode = (int)getResponse0.StatusCode;
        responseCode.Should().Be(400);
    }

}
