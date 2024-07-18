// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using System.IO;
using TL;


Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: false);

IConfiguration config = builder.Build();

var apiId = config["Telegram:ApiId"];
var apiHash = config["Telegram:ApiHash"];
var phoneNumber = config["Telegram:PhoneNumber"];


using var client = new WTelegram.Client(apiID: int.Parse( apiId), apiHash:apiHash);
WTelegram.Helpers.Log = (lvl, str) => { };
await DoLogin(phoneNumber);

var contacts = await client.Contacts_GetContacts();
foreach ( var contact in contacts.contacts )
{
}

foreach (var (id, user) in contacts.users)
{
    if (string.IsNullOrEmpty(user.MainUsername))
        Console.WriteLine($"{user.first_name}\t{user.phone}\tTID:{id}");
    else
        Console.WriteLine($"@{user.MainUsername}\t{user.first_name}\t{user.phone}\tTID:{id}");
}

Console.WriteLine("                                                                                       ");
Console.WriteLine("=======================================================================================");
Console.WriteLine("=============================== CHATS =================================================");
Console.WriteLine("=======================================================================================");
Console.WriteLine("                                                                                       ");


var gad = await client.Channels_GetGroupsForDiscussion();
foreach (var (id, gg) in gad.chats)
{

}

var chats = await client.Messages_GetAllChats();


// Group, Chat, CHannel - what's the difference?

foreach (var (id, chat) in chats.chats)
{
    Console.WriteLine($"@{chat.MainUsername}\t{chat}\tTID:{id}");
}






async Task DoLogin(string loginInfo) // (add this method to your code)
{
    while (client.User == null)
        switch (await client.Login(loginInfo)) // returns which config is needed to continue login
        {
            case "verification_code": Console.Write("Code: "); loginInfo = Console.ReadLine(); break;
            case "name": loginInfo = "John Doe"; break;    // if sign-up is required (first/last_name)
            case "password": loginInfo = "secret!"; break; // if user has enabled 2FA
            default: loginInfo = null; break;
        }
    Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");
}