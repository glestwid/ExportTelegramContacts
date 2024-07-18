﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Contacts;
using TeleSharp.TL.Channels;
using TLSharp.Core;
using TeleSharp.TL.Messages;
using System.Diagnostics.Contracts;

namespace ExportTelegramContacts
{
	class Program
	{
		private static TelegramClient _client;
		private static TLUser _user;


		public static int ApiId
		{
			get
			{
				var idStr = System.Configuration.ConfigurationManager.AppSettings["api_id"];
				int.TryParse(idStr, out var id);

				return id;
			}
		}
		public static string ApiHash => System.Configuration.ConfigurationManager.AppSettings["api_hash"] ?? "";

		static void Main(string[] args)
		{
			Console.WriteLine("***************************");
			Console.WriteLine($"Welcome to Telegram Contacts Exporter Version {Assembly.GetExecutingAssembly().GetName().Version}");
			Console.WriteLine("***************************");
			try
			{
				var apiId = ApiId;
				var apiHash = ApiHash;

				if (string.IsNullOrWhiteSpace(apiHash) ||
				    apiHash.Contains("PLACEHOLDER") ||
				    apiId <= 0)
				{
					Console.WriteLine("The values for 'api_id' or 'api_hash' are NOT provided. Please enter these value in the '.config' file and try again.");
					Console.ReadKey(intercept: true);
					return;
				}


				Console.Write("Connecting to Telegram servers...");
				_client = new TelegramClient(ApiId, ApiHash);
				var connect = _client.ConnectAsync();
				connect.Wait();
				Console.WriteLine("Connected");
			}
			catch (Exception ex)
			{
				Console.WriteLine();
				Console.WriteLine(ex.Message);
				Console.ReadKey(intercept: true);
				return;
			}

			char? WriteMenu()
			{
				if (!_client.IsUserAuthorized())
				{
					Console.WriteLine("You are not authenticated, please authenticate first.");
				}

				Console.WriteLine();
				Console.WriteLine("***************************");
				Console.WriteLine("1: Authenticate");
                Console.WriteLine("2: Export Contacts");
                Console.WriteLine("3: Export Channels");
                Console.WriteLine("Q: Quit");
				Console.WriteLine(" ");
				Console.Write("Please enter your choice: ");
				return Console.ReadLine()?.ToLower().FirstOrDefault();
			}

			while (true)
			{
				var userInput = WriteMenu();

				switch (userInput)
				{
					case 'q':
						return;

					case '1':
						CallAuthenicate().Wait();
						break;

                    case '2':
                        CallExportContacts().Wait();
                        break;

                    case '3':
                        CallExportChannels().Wait();
                        break;
                    default:
						Console.Clear();
						Console.WriteLine("Invalid input!");

						break;
				}
			}
		}

        private static async Task CallExportChannels()
        {
            try
            {
                if (!_client.IsUserAuthorized())
                {
                    Console.WriteLine("You are not authenticated, please authenticate first.");
                    return;
                }

                Console.WriteLine($"Reading channels...");



                var dialogs = (TLDialogsSlice)await _client.GetUserDialogsAsync(limit: Int32.MaxValue) as TLDialogsSlice;

                var chats = ((TeleSharp.TL.Messages.TLDialogsSlice)dialogs).Chats;

                var channels = dialogs.Chats
                            .OfType<TLChannel>()
                            .ToList();

                var onlyChannels = chats.OfType<TeleSharp.TL.TLChannel>().Where(x => x.Broadcast).ToList();
                var onlyGroups = chats.OfType<TeleSharp.TL.TLChannel>().Where(x => x.Megagroup).ToList();

                Console.WriteLine($"Number of channels: {channels.Count}");

                var fileName = $"ExportedContacts\\ExportedChannels-{DateTime.Now:yyyy-MM-dd HH-mm.ss}.txt";

                Directory.CreateDirectory("ExportedContacts");

                Console.WriteLine($"Writing to: {fileName}");
                using (var file = File.Create(fileName))
                using (var stringWrite = new StreamWriter(file))
                {
                    var savedCount = 0;
                    foreach (var channel in channels)
                    {
                        //Title
                        stringWrite.WriteLine(channel.Title);

						if (string.IsNullOrEmpty(channel.Username))
						{
                            TeleSharp.TL.Messages.TLChatFull res = new TeleSharp.TL.Messages.TLChatFull();

        //                    TeleSharp.TL.Messages.TLChatFull channelInfo = await _client.SendRequestAsync<TeleSharp.TL.Messages.TLChatFull>
								//(new TLRequestGetFullChannel() { Channel = channel});
							var qq = 0;
                        }
                        //Link
                        stringWrite.WriteLine("https://t.me/" + channel.Username);

                        savedCount++;
                    }
                    Console.WriteLine($"Total number of channels saved: {savedCount}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown error, if the error conitinues removing 'session.dat' file may help.\r\n" + ex.Message);
                return;
            }
        }


        private static async Task CallExportContacts()
        {
            try
            {
                if (!_client.IsUserAuthorized())
                {
                    Console.WriteLine("You are not authenticated, please authenticate first.");
                    return;
                }

                Console.WriteLine($"Reading contacts...");

                var contacts = (await _client.GetContactsAsync()) as TLContacts;

                Console.WriteLine($"Number of contacts: {contacts.Users.Count}");

                var fileName = $"ExportedContacts\\Exported-{DateTime.Now:yyyy-MM-dd HH-mm.ss}.vcf";
                var fileNameWihPhoto = $"ExportedContacts\\Exported-WithPhoto-{DateTime.Now:yyyy-MM-dd HH-mm.ss}.vcf";

                Directory.CreateDirectory("ExportedContacts");

                Console.Write($"Export contacts only with phone? [y/n] ");
                var filterResult = Console.ReadLine() ?? "";
                var exportOnlyWithPhone = !(filterResult == "" || filterResult.ToLower() == "n");

                Console.WriteLine($"Writing to: {fileName}");
                using (var file = File.Create(fileName))
                using (var stringWrite = new StreamWriter(file))
                {
                    var savedCount = 0;
                    foreach (var user in contacts.Users.OfType<TLUser>())
                    {
                        if (exportOnlyWithPhone)
                        {
                            if (string.IsNullOrWhiteSpace(user.Phone))
                                continue;
                        }

                        //vCard Begin
                        stringWrite.WriteLine("BEGIN:VCARD");
                        stringWrite.WriteLine("VERSION:2.1");
                        //Name
                        stringWrite.WriteLine("N:" + user.LastName + ";" + user.FirstName);
                        //Full Name
                        stringWrite.WriteLine("FN:" + user.FirstName + " " +
                                             /* nameMiddle + " " +*/ user.LastName);
                        stringWrite.WriteLine("TEL;CELL:" + ConvertFromTelegramPhoneNumber(user.Phone));
                        if (!string.IsNullOrEmpty(user.Username))
                        {
                            //vCard Telegram nickname
                            stringWrite.WriteLine("TN:@" + user.Username);
                        }

                        //vCard End
                        stringWrite.WriteLine("END:VCARD");

                        savedCount++;
                    }
                    Console.WriteLine($"Total number of contacts saved: {savedCount}");
                    Console.WriteLine();
                }

                Console.Write($"Do you want to export contacts with images? [y=enter/n] ");
                var exportWithPhotoResult = Console.ReadLine() ?? "";
                var exportWithPhoto = exportWithPhotoResult == "" || exportWithPhotoResult.ToLower() == "y";

                if (exportWithPhoto)
                {
                    Console.Write($"Save small or big images? [s=small=enter/b=big] ");
                    var saveSmallResult = Console.ReadLine() ?? "";
                    var saveSmallImages = saveSmallResult == "" || saveSmallResult.ToLower() == "s";

                    Console.WriteLine($"Writing to: {fileNameWihPhoto}");
                    using (var file = File.Create(fileNameWihPhoto))
                    using (var stringWrite = new StreamWriter(file))
                    {


                        var savedCount = 0;
                        foreach (var user in contacts.Users.OfType<TLUser>())
                        {
                            if (exportOnlyWithPhone)
                            {
                                if (string.IsNullOrWhiteSpace(user.Phone))
                                    continue;
                            }

                            string userPhotoString = null;
                            try
                            {
                                var userPhoto = user.Photo as TLUserProfilePhoto;
                                if (userPhoto != null)
                                {
                                    var photo = userPhoto.PhotoBig as TLFileLocation;
                                    if (saveSmallImages)
                                        photo = userPhoto.PhotoSmall as TLFileLocation;

                                    if (photo != null)
                                    {
                                        Console.Write($"Reading prfile image for: {user.FirstName} {user.LastName}...");

                                        var smallPhotoBytes = await GetFile(_client,
                                            new TLInputFileLocation()
                                            {
                                                LocalId = photo.LocalId,
                                                Secret = photo.Secret,
                                                VolumeId = photo.VolumeId
                                            });

                                        // resize if it is the big image
                                        if (!saveSmallImages)
                                        {
                                            Console.Write("Resizing...");
                                            smallPhotoBytes = ResizeProfileImage(ref smallPhotoBytes);
                                        }

                                        userPhotoString = Convert.ToBase64String(smallPhotoBytes);

                                        Console.WriteLine("Done");
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Failed due " + e.Message);
                            }


                            //System.IO.StringWriter stringWrite = new System.IO.StringWriter();
                            //create an htmltextwriter which uses the stringwriter

                            //vCard Begin
                            stringWrite.WriteLine("BEGIN:VCARD");
                            stringWrite.WriteLine("VERSION:2.1");
                            //Name
                            stringWrite.WriteLine("N:" + user.LastName + ";" + user.FirstName);
                            //Full Name
                            stringWrite.WriteLine("FN:" + user.FirstName + " " +
                                                  /* nameMiddle + " " +*/ user.LastName);
                            stringWrite.WriteLine("TEL;CELL:" + ConvertFromTelegramPhoneNumber(user.Phone));
                            if (!string.IsNullOrEmpty(user.Username))
                            {
                                //vCard Telegram nickname
                                stringWrite.WriteLine("TN:@" + user.Username);
                            }

                            if (userPhotoString != null)
                            {
                                stringWrite.WriteLine("PHOTO;ENCODING=BASE64;TYPE=JPEG:");
                                stringWrite.WriteLine(userPhotoString);
                                stringWrite.WriteLine(string.Empty);
                            }


                            //vCard End
                            stringWrite.WriteLine("END:VCARD");

                            savedCount++;
                        }
                        Console.WriteLine($"Total number of contacts with images saved: {savedCount}");
                        Console.WriteLine();

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown error, if the error conitinues removing 'session.dat' file may help.\r\n" + ex.Message);
                return;
            }
        }
        private static async Task<byte[]> GetFile(TelegramClient client, TLInputFileLocation file)
		{
			int filePart = 512 * 1024;
			int offset = 0;
			using (var mem = new MemoryStream())
			{
				while (true)
				{
					if (!client.IsConnected)
					{
						await client.ConnectAsync(true);
					}
					var resFile = await client.GetFile(
						file,
						filePart, offset);

					mem.Write(resFile.Bytes, 0, resFile.Bytes.Length);
					offset += filePart;
					var readCount = resFile.Bytes.Length;

#if DEBUG
					Console.Write($" ... read {readCount} of {filePart} .");
#endif
					if (readCount < filePart)
						break;
				}
				return mem.ToArray();
			}
		}



		public static string ConvertFromTelegramPhoneNumber(string number)
		{
			if (string.IsNullOrEmpty(number))
				return number;
			if (number.StartsWith("0"))
				return number;
			if (number.StartsWith("+"))
				return number;
			return "+" + number;
		}


		private static async Task CallAuthenicate()
		{
			Console.Write("Please enter your mobile number (e.g: 14155552671): ");
			var phoneNumber = Console.ReadLine();

			string requestHash;
			try
			{
				requestHash = await _client.SendCodeRequestAsync(phoneNumber);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadKey(intercept: true);
				return;
			}
			Console.Write("Request is sent to your mobile or the telegram app associated with this number, please enter the code here: ");
			var authCode = Console.ReadLine();

			try
			{
				_user = await _client.MakeAuthAsync(phoneNumber, requestHash, authCode);

				Console.WriteLine($"Authenicaion was successfull for Person Name:{_user.FirstName + " " + _user.LastName}, Username={_user.Username}");

#if DEBUG
				File.Copy("session.dat", "session.backup-copy.dat", true);
#endif

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return;
			}
		}

		private static byte[] ResizeProfileImage(ref byte[] imageBytes)
		{
			int vcarImageSize = 300;
			int vcarImageQuality = 70;

			using (var imgMem = new MemoryStream(imageBytes))
			using (var img = Image.FromStream(imgMem))
			{
				using (var mediumImageStream = new MemoryStream())
				using (var mediumImage = ResizeImage(
					img,
					vcarImageSize,
					vcarImageSize))
				{
					var jpegCodec = JpegEncodingCodec;
					var jpegQuality = GetQualityParameter(vcarImageQuality);

					mediumImage.Save(mediumImageStream, jpegCodec, jpegQuality);

					// the new image should be smaller than the original one
					if (mediumImageStream.Length > imageBytes.Length)
					{
						return imageBytes;
					}
					else
					{
						return mediumImageStream.ToArray();
					}
				}
			}
		}


		private static Image ResizeImage(Image image, int maxWidth, int maxHeight)
		{
			var ratioX = (double)maxWidth / image.Width;
			var ratioY = (double)maxHeight / image.Height;
			var ratio = Math.Min(ratioX, ratioY);

			var newWidth = (int)(image.Width * ratio);
			var newHeight = (int)(image.Height * ratio);

			var newImage = new Bitmap(newWidth, newHeight);

			using (var graphics = Graphics.FromImage(newImage))
				graphics.DrawImage(image, 0, 0, newWidth, newHeight);

			return newImage;
		}


		private static EncoderParameters GetQualityParameter(int quality)
		{
			// Encoder parameter for image quality 
			var qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

			// JPEG image codec 
			var encoderParams = new EncoderParameters(1)
			{
				Param = { [0] = qualityParam }
			};

			return encoderParams;
		}

		private static ImageCodecInfo _jpegEncodingCodec;
		private static ImageCodecInfo JpegEncodingCodec => _jpegEncodingCodec ?? (_jpegEncodingCodec = GetEncoderInfo("image/jpeg"));

		/// <summary> 
		/// Returns the image codec with the given mime type 
		/// </summary> 
		private static ImageCodecInfo GetEncoderInfo(string mimeType)
		{
			// Get image codecs for all image formats 
			var codecs = ImageCodecInfo.GetImageEncoders();

			// Find the correct image codec 
			for (int i = 0; i < codecs.Length; i++)
				if (codecs[i].MimeType == mimeType)
					return codecs[i];

			return null;
		}
	}
}
