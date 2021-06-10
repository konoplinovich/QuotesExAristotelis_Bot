using ImageMagick;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;


namespace QuotesExAristotelis_bot
{
    public static class Configuration
    {
        public readonly static string BotToken = "1719707237:AAFwjCBU_Etx01lV9dj9tPHtI83b2ZrACRM";
    }
    public static class Program
    {
        private static TelegramBotClient Bot;
        private static string _imagePath;
        private static Collection<string> _names = new Collection<string>() { "Платон", "Сократ", "Аристотель", "Эмпедокл", "Зенон", "Диоген" };

        public static async Task Main()
        {
            Bot = new TelegramBotClient(Configuration.BotToken);

            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;

            _imagePath = Path.Combine(Path.GetTempPath(), "platonus.jpg");

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");

            Console.ReadLine();
            Bot.StopReceiving();
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            Console.WriteLine($"Message from {message.From}: {message.Text}");

            if (message == null || message.Type != MessageType.Text)
                return;

            switch (message.Text.Split(' ').First())
            {
                // send a photo
                case "/photo":
                    var m = message.Text.Substring("/photo".Length + 1);

                    await SendDocument(message, m);
                    break;

                default:
                    await Usage(message);
                    break;
            }

            static async Task SendDocument(Message message, string text)
            {
                DateTime start = DateTime.Now;

                if (text.Length <= 190)
                {
                    text = $"«{text}»";

                    using (MagickImage image = new MagickImage(new MagickColor("white"), 500, 500))
                    {
                        var readSettingsText = new MagickReadSettings
                        {
                            Font = "Font/Bitter-Light.ttf",
                            FontFamily = "Bitter",
                            TextGravity = Gravity.Center,
                            BackgroundColor = MagickColors.Transparent,
                            Height = 350, // height of text box
                            Width = 400, // width of text box
                        };

                        using (var caption = new MagickImage($"caption:{text}", readSettingsText))
                        {
                            image.Composite(caption, 50, 50, CompositeOperator.Over);
                        }

                        var readSettingsSignature = new MagickReadSettings
                        {
                            Font = "Font/Bitter-LightItalic.ttf",
                            FontFamily = "Bitter",
                            FontPointsize = 25,
                            TextGravity = Gravity.Southeast,
                            BackgroundColor = MagickColors.Transparent,
                            Height = 50, // height of text box
                            Width = 400, // width of text box
                        };

                        string name = GetName();

                        using (var caption = new MagickImage($"caption:{name}", readSettingsSignature))
                        {
                            image.Composite(caption, 50, 400, CompositeOperator.Over);
                        }

                        image.Write(_imagePath);
                    }

                    DateTime end = DateTime.Now;

                    var interval = end - start;

                    await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                    using var fileStream = new FileStream(_imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var fileName = _imagePath.Split(Path.DirectorySeparatorChar).Last();
                    await Bot.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: new InputOnlineFile(fileStream, fileName),
                        caption: interval.TotalSeconds.ToString()
                    );
                }
                else
                {
                    string name;

                    if (String.IsNullOrEmpty(message.From.Username)) name = $"{message.From.FirstName} {message.From.LastName}";
                    else name = message.From.Username;

                    await Bot.SendTextMessageAsync(chatId: message.Chat.Id, $"Эй, {name}, многовато букв ({text.Length}), а можно всего жалких 190!");
                }
            }

            static async Task Usage(Message message)
            {
                const string usage = "Usage:\n" +
                                        "/inline   - send inline keyboard\n" +
                                        "/keyboard - send custom keyboard\n" +
                                        "/photo    - send a photo\n" +
                                        "/request  - request location or contact";
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove()
                );
            }
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }

        private static string GetName()
        {
            var rand = new Random();
            int index = rand.Next(_names.Count);

            return _names[index];
        }
    }
}